using System.Text.Json;
using System.Windows;

namespace SingleInstanceDemo;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    internal void ShowRequest(ActivationRequest request, bool forwarded)
    {
        Requests.AppendText($"{(forwarded ? "转发" : "首次启动")} · PID {Environment.ProcessId}\n" +
            $"参数：{JsonSerializer.Serialize(request.Arguments)}\n" +
            $"工作目录：{request.WorkingDirectory}\n请求：{request.RequestId}\n\n");
        Requests.ScrollToEnd();
    }
}
