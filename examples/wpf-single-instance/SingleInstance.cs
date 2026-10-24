using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace SingleInstanceDemo;

internal sealed record InstanceIdentity(string MutexName, string PipeName)
{
    public static InstanceIdentity ForCurrentUser(string applicationId)
    {
        using var identity = WindowsIdentity.GetCurrent();
        string sid = identity.User?.Value ?? throw new InvalidOperationException("无法取得当前用户 SID。");
        string userKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sid)))[..24];
        using var process = Process.GetCurrentProcess();
        return new($@"Local\{applicationId}.{userKey}",
            $"{applicationId}.{userKey}.{process.SessionId}");
    }
}

internal sealed class InstanceLease : IDisposable
{
    private readonly Mutex _mutex;
    private bool _ownsMutex;
    private bool _disposed;
    private int _ownerThread;

    public InstanceLease(string name) => _mutex = new Mutex(false, name);

    public bool TryAcquire()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_ownsMutex)
        {
            VerifyOwnerThread();
            return true; // 不重复 WaitOne，避免增加递归持有计数。
        }
        try { _ownsMutex = _mutex.WaitOne(0); }
        catch (AbandonedMutexException) { _ownsMutex = true; }
        if (_ownsMutex) _ownerThread = Environment.CurrentManagedThreadId;
        return _ownsMutex;
    }

    private void VerifyOwnerThread()
    {
        if (_ownerThread != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException("必须在取得 Mutex 的线程释放它。");
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_ownsMutex)
        {
            VerifyOwnerThread();
            _mutex.ReleaseMutex();
            _ownsMutex = false;
        }
        _mutex.Dispose();
        _disposed = true;
    }
}

internal sealed record ActivationRequest(int Version, Guid RequestId, string[] Arguments, string WorkingDirectory)
{
    public static ActivationRequest Create(string[] arguments) =>
        new(1, Guid.NewGuid(), arguments, Environment.CurrentDirectory);
}
internal sealed record ServerHello(int Version, int ProcessId);
internal sealed record ActivationReply(Guid RequestId, string Status);
internal enum DeliveryStatus { Accepted, NotSent, Rejected, Uncertain }
internal sealed record DeliveryResult(DeliveryStatus Status, string Message);

internal static class ActivationProtocol
{
    public const int MaxFrameBytes = 64 * 1024;

    public static byte[] Encode<T>(T value)
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(value);
        if (payload.Length is < 1 or > MaxFrameBytes)
            throw new InvalidDataException("请求超过协议允许的大小。");
        return payload;
    }

    public static async Task WritePayloadAsync(Stream stream, byte[] payload, CancellationToken token)
    {
        byte[] header = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);
        await stream.WriteAsync(header, token).ConfigureAwait(false);
        await stream.WriteAsync(payload, token).ConfigureAwait(false);
        await stream.FlushAsync(token).ConfigureAwait(false);
    }

    public static Task WriteAsync<T>(Stream stream, T value, CancellationToken token) =>
        WritePayloadAsync(stream, Encode(value), token);

    public static async Task<T> ReadAsync<T>(Stream stream, CancellationToken token)
    {
        byte[] header = new byte[4];
        await stream.ReadExactlyAsync(header, token).ConfigureAwait(false);
        int length = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (length is < 1 or > MaxFrameBytes)
            throw new InvalidDataException("无效的消息长度。");
        byte[] payload = new byte[length];
        await stream.ReadExactlyAsync(payload, token).ConfigureAwait(false);
        return JsonSerializer.Deserialize<T>(payload) ?? throw new InvalidDataException("消息不能为空。");
    }
}

internal sealed class ActivationServer : IAsyncDisposable
{
    private readonly NamedPipeServerStream _pipe;
    private readonly CancellationTokenSource _stop = new();
    private readonly Channel<ActivationRequest> _requests;
    private readonly TimeSpan _exchangeTimeout;
    private readonly Task _listener;

    public ChannelReader<ActivationRequest> Requests => _requests.Reader;
    public CancellationToken Stopping => _stop.Token;

    public ActivationServer(string pipeName, int capacity = 32, TimeSpan? exchangeTimeout = null)
    {
        _exchangeTimeout = exchangeTimeout ?? TimeSpan.FromSeconds(2);
        _requests = Channel.CreateBounded<ActivationRequest>(new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false
        });
        // 在创建 UI 前同步建立端点；名称已被占用时立即报错。
        _pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly | PipeOptions.FirstPipeInstance);
        _listener = ListenAsync();
    }

    private async Task ListenAsync()
    {
        Exception? failure = null;
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                await _pipe.WaitForConnectionAsync(_stop.Token).ConfigureAwait(false);
                using var exchange = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                exchange.CancelAfter(_exchangeTimeout);
                try
                {
                    await ActivationProtocol.WriteAsync(_pipe,
                        new ServerHello(1, Environment.ProcessId), exchange.Token).ConfigureAwait(false);
                    var request = await ActivationProtocol.ReadAsync<ActivationRequest>(_pipe, exchange.Token)
                        .ConfigureAwait(false);
                    bool valid = request.Version == 1 && request.RequestId != Guid.Empty &&
                        request.Arguments is not null && request.Arguments.All(x => x is not null) &&
                        !string.IsNullOrWhiteSpace(request.WorkingDirectory) &&
                        Path.IsPathFullyQualified(request.WorkingDirectory);
                    string status = !valid ? "invalid" : _requests.Writer.TryWrite(request) ? "accepted" : "busy";
                    await ActivationProtocol.WriteAsync(_pipe,
                        new ActivationReply(request.RequestId, status), exchange.Token).ConfigureAwait(false);
                    // 正常客户端读取确认后关闭连接。过早 Disconnect 可能丢弃尚未读走的确认。
                    int trailingBytes = await _pipe.ReadAsync(new byte[1], exchange.Token).ConfigureAwait(false);
                    if (trailingBytes != 0) Trace.WriteLine("一次连接只接受一个激活请求。");
                }
                catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or OperationCanceledException)
                {
                    Trace.WriteLine($"激活连接结束：{ex.Message}");
                }
                finally
                {
                    // 客户端关闭后 IsConnected 可能为 false，但 Broken 状态仍需 Disconnect 才能复用。
                    _pipe.Disconnect();
                }
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        catch (Exception ex) { failure = ex; throw; }
        finally { _requests.Writer.TryComplete(failure); }
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        try { await _listener.ConfigureAwait(false); }
        finally { _pipe.Dispose(); _stop.Dispose(); }
    }
}

internal static class ActivationClient
{
    public static async Task<DeliveryResult> TryForwardAsync(string pipeName, ActivationRequest request,
        TimeSpan timeout, Action<int>? allowForeground = null)
    {
        byte[] payload;
        try { payload = ActivationProtocol.Encode(request); }
        catch (InvalidDataException ex) { return new(DeliveryStatus.Rejected, ex.Message); }
        using var deadline = new CancellationTokenSource(timeout);
        using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        bool transmissionStarted = false;
        try
        {
            await pipe.ConnectAsync(deadline.Token).ConfigureAwait(false);
            var hello = await ActivationProtocol.ReadAsync<ServerHello>(pipe, deadline.Token).ConfigureAwait(false);
            if (hello.Version != 1 || hello.ProcessId <= 0)
                return new(DeliveryStatus.Rejected, "主实例协议版本不兼容。");
            allowForeground?.Invoke(hello.ProcessId); // 尽力授权，不把成功与否当成交付条件。
            transmissionStarted = true;
            await ActivationProtocol.WritePayloadAsync(pipe, payload, deadline.Token).ConfigureAwait(false);
            var reply = await ActivationProtocol.ReadAsync<ActivationReply>(pipe, deadline.Token).ConfigureAwait(false);
            if (reply.RequestId != request.RequestId)
                return new(DeliveryStatus.Uncertain, "收到不匹配的确认，无法确定交付结果。");
            return reply.Status == "accepted"
                ? new(DeliveryStatus.Accepted, "请求已进入主实例队列。")
                : new(DeliveryStatus.Rejected, $"主实例拒绝请求：{reply.Status}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return new(transmissionStarted ? DeliveryStatus.Uncertain : DeliveryStatus.Rejected,
                "通信权限被拒绝，请确认两个实例使用相同用户和提升级别。" + ex.Message);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or OperationCanceledException or JsonException)
        {
            return new(transmissionStarted ? DeliveryStatus.Uncertain : DeliveryStatus.NotSent, ex.Message);
        }
    }
}
