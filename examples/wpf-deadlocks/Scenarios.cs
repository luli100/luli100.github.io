using System.Windows.Threading;

namespace WpfDeadlocks;

internal static class Scenarios
{
    public static readonly Scenario[] All =
    [
        new("invoke-wait", Expected.UiBlocked, InvokeWait),
        new("result-context", Expected.UiBlocked, ResultContext),
        new("dispatcher-task-wait", Expected.UiBlocked, DispatcherTaskWait),
        new("lock-invoke", Expected.UiBlocked, LockInvoke),
        new("semaphore-recursion", Expected.OperationBlocked, SemaphoreRecursion),
        new("async-task-cycle", Expected.OperationBlocked, AsyncTaskCycle),
        new("two-dispatchers", Expected.UiBlocked, TwoDispatchers),
        new("unserviced-dispatcher", Expected.OperationBlocked, UnservicedDispatcher),
        new("await-fix", Expected.Complete, AwaitFix),
        new("snapshot-fix", Expected.Complete, SnapshotFix),
        new("semaphore-fix", Expected.Complete, SemaphoreFix),
        new("begininvoke-order", Expected.Complete, BeginInvokeOrder),
        new("configureawait-completed", Expected.Complete, ConfigureCompleted),
        new("configureawait-caller", Expected.Complete, ConfigureCaller),
        new("dispatcher-inner-task", Expected.Complete, DispatcherInnerTask),
        new("timeout-keeps-operation", Expected.Complete, TimeoutKeepsOperation)
    ];

    private static Task InvokeWait(Probe p)
    {
        Task.Run(() =>
        {
            p.ReachedWait.Set();
            p.Dispatcher.Invoke(() => p.Window.Title = "Finished");
        }).Wait();
        return Task.CompletedTask;
    }

    private static Task ResultContext(Probe p)
    {
        var source = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<int> GetValueAsync() => await source.Task;
        Task<int> task = GetValueAsync(); // 在 UI 上先捕获上下文，再由生产者完成等待对象。
        _ = Task.Run(() => source.SetResult(100));
        p.ReachedWait.Set();
        int value = task.Result;
        p.Window.Title = value.ToString();
        return Task.CompletedTask;
    }

    private static Task DispatcherTaskWait(Probe p)
    {
        var operation = p.Dispatcher.InvokeAsync(() => p.Window.Title = "Finished");
        p.ReachedWait.Set();
        operation.Task.Wait();
        return Task.CompletedTask;
    }

    private static async Task LockInvoke(Probe p)
    {
        var gate = new object();
        await Task.Run(() =>
        {
            lock (gate)
            {
                p.Dispatcher.Invoke(() =>
                {
                    p.ReachedWait.Set();
                    lock (gate) { p.Window.Title = "Finished"; }
                });
            }
        });
    }

    private static async Task SemaphoreRecursion(Probe p)
    {
        using var serial = new SemaphoreSlim(1, 1);
        async Task InnerAsync()
        {
            p.ReachedWait.Set();
            await serial.WaitAsync();
            try { await Task.Yield(); }
            finally { serial.Release(); }
        }
        await serial.WaitAsync();
        try { await InnerAsync(); }
        finally { serial.Release(); }
    }

    private static async Task AsyncTaskCycle(Probe p)
    {
        var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task first = null!;
        Task second = null!;
        async Task WaitForOther(Func<Task> other)
        {
            await ready.Task;
            await other();
        }
        first = WaitForOther(() => second);
        second = WaitForOther(() => first);
        ready.SetResult(true);
        p.ReachedWait.Set();
        await Task.WhenAll(first, second);
    }

    private static Task TwoDispatchers(Probe p)
    {
        Dispatcher? other = null;
        using var ready = new ManualResetEventSlim();
        var uiB = new Thread(() =>
        {
            other = Dispatcher.CurrentDispatcher;
            ready.Set();
            Dispatcher.Run();
        }) { IsBackground = true, Name = "UI B" };
        uiB.SetApartmentState(ApartmentState.STA);
        uiB.Start();
        if (!ready.Wait(2000)) throw new TimeoutException("UI B failed to initialize");
        other!.Invoke(() =>
        {
            p.ReachedWait.Set();
            p.Dispatcher.Invoke(() => p.Window.Title = "Unreachable");
        });
        return Task.CompletedTask;
    }

    private static async Task UnservicedDispatcher(Probe p)
    {
        await Task.Run(() =>
        {
            var wrong = Dispatcher.CurrentDispatcher;
            p.Require(wrong != p.Dispatcher, "expected a different dispatcher");
            var operation = wrong.InvokeAsync(() => { });
            p.ReachedWait.Set();
            return operation.Task; // 工作线程没有运行 Dispatcher.Run。
        });
    }

    private static async Task AwaitFix(Probe p)
    {
        int before = p.Pulses;
        await Task.Run(() => Thread.Sleep(150));
        p.Require(p.Dispatcher.CheckAccess(), "UI context was not restored");
        p.Require(p.Pulses > before, "UI did not make progress during await");
        p.Window.Title = "Finished";
    }

    private static async Task SnapshotFix(Probe p)
    {
        var gate = new object();
        int value = 0;
        int snapshot = await Task.Run(() =>
        {
            lock (gate) { return ++value; }
        });
        await p.Dispatcher.InvokeAsync(() => p.Window.Title = snapshot.ToString());
        p.Require(p.Window.Title == "1", "snapshot was not displayed");
    }

    private static async Task SemaphoreFix(Probe p)
    {
        using var serial = new SemaphoreSlim(1, 1);
        int executions = 0;
        async Task CoreAsync(CancellationToken token)
        {
            await Task.Delay(30, token);
            executions++;
        }
        async Task RunAsync(CancellationToken token)
        {
            await serial.WaitAsync(token);
            try { await CoreAsync(token); }
            finally { serial.Release(); }
        }
        await RunAsync(CancellationToken.None);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        try
        {
            await RunAsync(canceled.Token);
            throw new InvalidOperationException("expected cancellation");
        }
        catch (OperationCanceledException) when (canceled.IsCancellationRequested) { }
        p.Require(executions == 1 && serial.CurrentCount == 1,
            "canceled acquisition must not release a permit it never acquired");
    }

    private static async Task BeginInvokeOrder(Probe p)
    {
        var order = new List<string>();
        DispatcherOperation? update = null;
        int before = p.Pulses;
        Task.Run(() =>
        {
            Thread.Sleep(100);
            update = p.Dispatcher.BeginInvoke(new Action(() => order.Add("ui-update")));
        }).Wait();
        p.Require(p.Pulses == before, "UI heartbeat unexpectedly ran during Wait");
        order.Add("after-wait");
        await update!.Task;
        p.Require(order.SequenceEqual(new[] { "after-wait", "ui-update" }),
            "task completion must precede queued UI update in this example");
    }

    private static async Task ConfigureCompleted(Probe p)
    {
        int thread = Environment.CurrentManagedThreadId;
        await Task.FromResult(100).ConfigureAwait(false);
        p.Require(thread == Environment.CurrentManagedThreadId && p.Dispatcher.CheckAccess(),
            "an already completed await should continue inline in this test");
    }

    private static async Task ConfigureCaller(Probe p)
    {
        var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<int> LibraryAsync()
        {
            int value = await completion.Task.ConfigureAwait(false);
            p.Require(!p.Dispatcher.CheckAccess(), "library unexpectedly requires UI");
            return value;
        }
        Task<int> task = LibraryAsync();
        _ = Task.Run(() => completion.SetResult(100));
        int result = await task;
        p.Require(result == 100 && p.Dispatcher.CheckAccess(), "caller did not retain its UI context");
        p.Window.Title = result.ToString();
    }

    private static async Task DispatcherInnerTask(Probe p)
    {
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = p.Dispatcher.InvokeAsync(new Func<Task>(async () =>
        {
            await release.Task;
            p.Window.Title = "Finished";
        }));
        Task inner = await operation.Task;
        p.Require(!inner.IsCompleted && p.Window.Title != "Finished", "outer task unexpectedly waited for inner");
        release.SetResult(true);
        await operation.Task.Unwrap();
        p.Require(inner.IsCompletedSuccessfully && p.Window.Title == "Finished", "inner task did not finish");
    }

    private static async Task TimeoutKeepsOperation(Probe p)
    {
        var original = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            await original.Task.WaitAsync(TimeSpan.FromMilliseconds(30));
            throw new InvalidOperationException("expected timeout");
        }
        catch (TimeoutException) { }
        p.Require(!original.Task.IsCompleted, "timeout should not complete or cancel original task");
        original.SetResult(100);
        p.Require(await original.Task == 100, "original operation should remain completable");
    }
}
