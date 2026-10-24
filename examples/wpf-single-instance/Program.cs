using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace SingleInstanceDemo;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            var identity = InstanceIdentity.ForCurrentUser("Example.SingleInstanceDemo.v1");
            var request = ActivationRequest.Create(args);
            using var lease = new InstanceLease(identity.MutexName);
            var deadline = Stopwatch.StartNew();
            while (true)
            {
                if (lease.TryAcquire()) return RunPrimary(identity, request);

                TimeSpan remaining = TimeSpan.FromSeconds(5) - deadline.Elapsed;
                if (remaining <= TimeSpan.Zero)
                    return ReportFailure("已有实例，但尚未建立连接。请稍后重试，程序不会另开窗口。");
                var result = ActivationClient.TryForwardAsync(identity.PipeName, request,
                    remaining < TimeSpan.FromMilliseconds(500) ? remaining : TimeSpan.FromMilliseconds(500),
                    NativeMethods.AllowForeground).GetAwaiter().GetResult();
                if (result.Status == DeliveryStatus.Accepted) return 0;
                if (result.Status != DeliveryStatus.NotSent) return ReportFailure(result.Message);
                // 尚未发送请求，可以重试连接；下一轮仍由入口线程重新尝试仲裁。
            }
        }
        catch (Exception ex) { return ReportFailure(ex.Message); }
    }

    private static int RunPrimary(InstanceIdentity identity, ActivationRequest initialRequest)
    {
        var server = new ActivationServer(identity.PipeName);
        Task pump = Task.CompletedTask;
        try
        {
            var app = new App();
            app.InitializeComponent();
            var window = new MainWindow();
            window.ShowRequest(initialRequest, forwarded: false);
            pump = DispatchRequestsAsync(server, app, window);
            return app.Run(window);
        }
        finally
        {
            // 此时 Dispatcher 已退出，停止网络等待并取消尚未执行的 UI 调度。
            try { server.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
            finally { pump.GetAwaiter().GetResult(); }
            // 返回 Main 后，才在原入口线程释放 Mutex。
        }
    }

    private static async Task DispatchRequestsAsync(ActivationServer server, App app, MainWindow window)
    {
        CancellationToken token = server.Stopping;
        try
        {
            await foreach (var request in server.Requests.ReadAllAsync(token).ConfigureAwait(false))
            {
                await window.Dispatcher.InvokeAsync(() =>
                {
                    window.ShowRequest(request, forwarded: true);
                    NativeMethods.RestoreAndActivate(window);
                }, DispatcherPriority.Normal, token).Task.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
            if (!window.Dispatcher.HasShutdownStarted && !token.IsCancellationRequested)
            {
                try
                {
                    await window.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show("单实例监听已停止：" + ex.Message);
                        app.Shutdown(1);
                    }, DispatcherPriority.Normal, token).Task.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            }
        }
    }

    private static int ReportFailure(string message)
    {
        MessageBox.Show(message, "启动未完成", MessageBoxButton.OK, MessageBoxImage.Information);
        return 1;
    }
}
