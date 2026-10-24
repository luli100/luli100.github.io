using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using SingleInstanceDemo;

internal static class Program
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(4);

    private static int Main(string[] args)
    {
        if (args.Length > 0) return Child(args);
        (string Name, Action Run)[] tests =
        [
            ("one-owner-under-concurrent-startup", ConcurrentStartup),
            ("release-with-other-handles-alive", ReleaseWithOtherHandles),
            ("abandoned-owner-recovery", AbandonedOwner),
            ("repeat-acquire-does-not-recurse", RepeatAcquire),
            ("identity-is-stable-and-app-specific", Identity),
            ("unicode-and-empty-arguments-roundtrip", () => Run(RoundtripAsync)),
            ("simultaneous-activation-requests", () => Run(BurstAsync)),
            ("buffer-before-ui-consumer-ready", () => Run(BufferBeforeUiAsync)),
            ("retry-before-request-sent", () => Run(RetryBeforeSentAsync)),
            ("lost-ack-is-uncertain", () => Run(LostAckAsync)),
            ("full-queue-rejects-without-dropping", () => Run(FullQueueAsync)),
            ("invalid-frame-does-not-stop-server", () => Run(InvalidFrameAsync)),
            ("idle-peer-has-deadline", () => Run(IdlePeerAsync)),
            ("stop-while-waiting-for-client", () => Run(StopWaitingAsync)),
            ("stop-while-reading-client", () => Run(StopReadingAsync)),
            ("oversized-request-is-not-sent", () => Run(OversizedRequestAsync))
        ];
        int failures = 0;
        foreach (var test in tests)
        {
            try { test.Run(); Console.WriteLine("PASS " + test.Name); }
            catch (Exception ex) { failures++; Console.Error.WriteLine($"FAIL {test.Name}: {ex}"); }
        }
        Console.WriteLine($"{tests.Length - failures}/{tests.Length} checks passed.");
        return failures == 0 ? 0 : 1;
    }

    private static void Run(Func<Task> test) => test().WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
    private static void Require(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException(reason);
    }
    private static string Name() => "Codex.SingleInstanceTest." + Guid.NewGuid().ToString("N");
    private static Task<DeliveryResult> Forward(string name, ActivationRequest request) =>
        ActivationClient.TryForwardAsync(name, request, Timeout);
    private static Task<ActivationRequest> Read(ActivationServer server) =>
        server.Requests.ReadAsync().AsTask().WaitAsync(Timeout);

    private static Process Spawn(params string[] arguments)
    {
        var info = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        info.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        foreach (string arg in arguments) info.ArgumentList.Add(arg);
        return Process.Start(info)!;
    }
    private static string? Line(Process child) => child.StandardOutput.ReadLineAsync().WaitAsync(Timeout).GetAwaiter().GetResult();
    private static void Finish(Process child)
    {
        if (!child.WaitForExit(5000))
        {
            child.Kill(entireProcessTree: true);
            child.WaitForExit();
            throw new TimeoutException("Child exceeded timeout.");
        }
        Require(child.ExitCode == 0, child.StandardError.ReadToEnd());
    }
    private static void Cleanup(Process child)
    {
        if (!child.HasExited) { child.Kill(entireProcessTree: true); child.WaitForExit(); }
        child.Dispose();
    }

    private static int Child(string[] args)
    {
        using var lease = new InstanceLease(args[1]);
        if (args[0] == "probe")
        {
            Console.WriteLine(lease.TryAcquire() ? "PRIMARY" : "SECONDARY");
            return 0;
        }
        if (args[0] == "abandon")
        {
            if (!lease.TryAcquire()) return 1;
            Console.WriteLine("OWNED");
            Console.Out.Flush();
            Environment.Exit(0); // 仅用于测试持有者异常退出，不执行 using 清理。
        }
        using var start = EventWaitHandle.OpenExisting(args[2]);
        using var release = EventWaitHandle.OpenExisting(args[3]);
        Console.WriteLine("READY");
        Console.Out.Flush();
        if (!start.WaitOne(4000)) return 1;
        bool primary = lease.TryAcquire();
        Console.WriteLine(primary ? "PRIMARY" : "SECONDARY");
        Console.Out.Flush();
        return !primary || release.WaitOne(4000) ? 0 : 1;
    }

    private static void ConcurrentStartup()
    {
        string mutex = @"Local\" + Name();
        string startName = @"Local\" + Name();
        string releaseName = @"Local\" + Name();
        using var start = new EventWaitHandle(false, EventResetMode.ManualReset, startName);
        using var release = new EventWaitHandle(false, EventResetMode.ManualReset, releaseName);
        var children = Enumerable.Range(0, 6).Select(_ => Spawn("contend", mutex, startName, releaseName)).ToArray();
        try
        {
            foreach (var child in children) Require(Line(child) == "READY", "Child not ready.");
            start.Set();
            var roles = children.Select(Line).ToArray();
            Require(roles.Count(x => x == "PRIMARY") == 1 && roles.Count(x => x == "SECONDARY") == 5,
                "Expected exactly one owner during overlap.");
            release.Set();
            foreach (var child in children) Finish(child);
        }
        finally { release.Set(); foreach (var child in children) Cleanup(child); }
    }

    private static void ReleaseWithOtherHandles()
    {
        string name = @"Local\" + Name();
        using var otherHandle = new Mutex(false, name);
        using (var owner = new InstanceLease(name))
        {
            Require(owner.TryAcquire(), "Owner should acquire.");
            using var candidate = Spawn("probe", name);
            Require(Line(candidate) == "SECONDARY", "Another process acquired held mutex.");
            Finish(candidate);
        }
        using var next = Spawn("probe", name);
        Require(Line(next) == "PRIMARY", "Existing unowned handle prevented takeover.");
        Finish(next);
    }

    private static void AbandonedOwner()
    {
        string name = @"Local\" + Name();
        using var keepAlive = new Mutex(false, name);
        using var previous = Spawn("abandon", name);
        Require(Line(previous) == "OWNED", "Child did not own mutex.");
        Finish(previous);
        using var recovered = new InstanceLease(name);
        Require(recovered.TryAcquire(), "Should own abandoned mutex after exception.");
        using var other = Spawn("probe", name);
        Require(Line(other) == "SECONDARY", "Recovered ownership was not held.");
        Finish(other);
    }

    private static void RepeatAcquire()
    {
        string name = @"Local\" + Name();
        using var keepAlive = new Mutex(false, name);
        using (var lease = new InstanceLease(name))
            Require(lease.TryAcquire() && lease.TryAcquire(), "Repeated query failed.");
        using var next = Spawn("probe", name);
        Require(Line(next) == "PRIMARY", "Repeated acquire left a recursive ownership count.");
        Finish(next);
    }

    private static void Identity()
    {
        var first = InstanceIdentity.ForCurrentUser("Example.Test");
        Require(first == InstanceIdentity.ForCurrentUser("Example.Test"), "Identity must be stable.");
        Require(first != InstanceIdentity.ForCurrentUser("Example.Other"), "App identities must differ.");
        Require(first.MutexName.StartsWith(@"Local\"), "Expected session-local mutex.");
    }

    private static async Task RoundtripAsync()
    {
        string name = Name();
        await using var server = new ActivationServer(name);
        var request = ActivationRequest.Create(["含 空格.txt", "", "--flag", "路径\\文件.txt"]);
        Require((await Forward(name, request)).Status == DeliveryStatus.Accepted, "Request rejected.");
        var received = await Read(server);
        Require(received.RequestId == request.RequestId && received.Arguments.SequenceEqual(request.Arguments) &&
            received.WorkingDirectory == request.WorkingDirectory, "Request changed in transit.");
    }

    private static async Task BurstAsync()
    {
        string name = Name();
        await using var server = new ActivationServer(name);
        var requests = Enumerable.Range(0, 8).Select(i => ActivationRequest.Create([i.ToString()])).ToArray();
        var results = await Task.WhenAll(requests.Select(x => Forward(name, x)));
        Require(results.All(x => x.Status == DeliveryStatus.Accepted), "Burst contained unsuccessful deliveries.");
        var ids = new HashSet<Guid>();
        foreach (var request in requests) ids.Add((await Read(server)).RequestId);
        Require(ids.SetEquals(requests.Select(x => x.RequestId)) && !server.Requests.TryRead(out _), "Lost or duplicate request.");
    }

    private static async Task BufferBeforeUiAsync()
    {
        string name = Name();
        await using var server = new ActivationServer(name);
        var request = ActivationRequest.Create(["before-ui"]);
        Require((await Forward(name, request)).Status == DeliveryStatus.Accepted, "Should accept without a UI consumer.");
        Require((await Read(server)).RequestId == request.RequestId, "Startup-buffered request lost.");
    }

    private static async Task RetryBeforeSentAsync()
    {
        string name = Name();
        var request = ActivationRequest.Create(["retry"]);
        var before = await ActivationClient.TryForwardAsync(name, request, TimeSpan.FromMilliseconds(80));
        Require(before.Status == DeliveryStatus.NotSent, "Expected a safe pre-send failure.");
        await using var server = new ActivationServer(name);
        Require((await Forward(name, request)).Status == DeliveryStatus.Accepted, "Retry failed.");
        Require((await Read(server)).RequestId == request.RequestId && !server.Requests.TryRead(out _), "Retry duplicated request.");
    }

    private static async Task LostAckAsync()
    {
        string name = Name();
        using var fake = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        using var timeout = new CancellationTokenSource(Timeout);
        async Task<ActivationRequest> ReceiveAndDrop()
        {
            await fake.WaitForConnectionAsync(timeout.Token);
            await ActivationProtocol.WriteAsync(fake, new ServerHello(1, Environment.ProcessId), timeout.Token);
            var read = await ActivationProtocol.ReadAsync<ActivationRequest>(fake, timeout.Token);
            fake.Disconnect();
            return read;
        }
        var receive = ReceiveAndDrop();
        var request = ActivationRequest.Create(["one-send"]);
        var result = await Forward(name, request);
        Require((await receive).RequestId == request.RequestId && result.Status == DeliveryStatus.Uncertain,
            "Lost acknowledgement must not be reported as safe to resend.");
    }

    private static async Task FullQueueAsync()
    {
        string name = Name();
        await using var server = new ActivationServer(name, capacity: 1);
        var first = ActivationRequest.Create(["first"]);
        Require((await Forward(name, first)).Status == DeliveryStatus.Accepted, "First request failed.");
        Require((await Forward(name, ActivationRequest.Create(["busy"]))).Status == DeliveryStatus.Rejected,
            "Full queue must reject instead of acknowledging a dropped request.");
        Require((await Read(server)).RequestId == first.RequestId, "Queued request was replaced.");
        Require((await Forward(name, ActivationRequest.Create(["next"]))).Status == DeliveryStatus.Accepted,
            "Server did not recover after queue was drained.");
    }

    private static async Task<NamedPipeClientStream> ConnectPeer(string name)
    {
        var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        using var timeout = new CancellationTokenSource(Timeout);
        await client.ConnectAsync(timeout.Token);
        await ActivationProtocol.ReadAsync<ServerHello>(client, timeout.Token);
        return client;
    }

    private static async Task InvalidFrameAsync()
    {
        string name = Name();
        await using var server = new ActivationServer(name);
        using (var bad = await ConnectPeer(name))
        {
            byte[] length = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(length, ActivationProtocol.MaxFrameBytes + 1);
            await bad.WriteAsync(length);
            using var timeout = new CancellationTokenSource(Timeout);
            try { Require(await bad.ReadAsync(new byte[1], timeout.Token) == 0, "Expected peer closure."); }
            catch (IOException) { }
        }
        Require((await Forward(name, ActivationRequest.Create([]))).Status == DeliveryStatus.Accepted,
            "Malformed client stopped the server.");
    }

    private static async Task IdlePeerAsync()
    {
        string name = Name();
        await using var server = new ActivationServer(name, exchangeTimeout: TimeSpan.FromMilliseconds(100));
        using (var idle = await ConnectPeer(name))
        {
            using var timeout = new CancellationTokenSource(Timeout);
            try { Require(await idle.ReadAsync(new byte[1], timeout.Token) == 0, "Expected idle peer closure."); }
            catch (IOException) { }
        }
        Require((await Forward(name, ActivationRequest.Create([]))).Status == DeliveryStatus.Accepted,
            "Idle client permanently blocked activation listener.");
    }

    private static async Task StopWaitingAsync()
    {
        var server = new ActivationServer(Name());
        await server.DisposeAsync().AsTask().WaitAsync(Timeout);
    }

    private static async Task StopReadingAsync()
    {
        string name = Name();
        var server = new ActivationServer(name);
        using var peer = await ConnectPeer(name);
        await server.DisposeAsync().AsTask().WaitAsync(Timeout);
    }

    private static async Task OversizedRequestAsync()
    {
        string name = Name();
        await using var server = new ActivationServer(name);
        var result = await Forward(name, ActivationRequest.Create([new string('x', ActivationProtocol.MaxFrameBytes)]));
        Require(result.Status == DeliveryStatus.Rejected && !server.Requests.TryRead(out _), "Oversized request was sent.");
        Require((await Forward(name, ActivationRequest.Create([]))).Status == DeliveryStatus.Accepted,
            "Server should remain available.");
    }
}
