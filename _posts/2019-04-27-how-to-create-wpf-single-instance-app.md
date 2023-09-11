---
layout: post
title: 如何创建 WPF 单实例应用程序
---

你曾经是否遇到过只准打开一个 WPF 应用程序实例这样的需求，如果有这样的需求场景，下面的方法应该可以满足你的需求：

```
public partial class App : Application
{
    private EventWaitHandle AppEventWait;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        StartSingleInstanceApp();
    }

    /// <summary>
    /// 开启单实例应用
    /// </summary>
    private void StartSingleInstanceApp()
    {
        String mutexName = "f7eeb38e-3ede-4fdb-8891-b285bff816e8";
        AppEventWait = new EventWaitHandle(false, EventResetMode.AutoReset, mutexName, out var isNewInstance);
        if (!isNewInstance)
        {
            AppEventWait.Set();
            App.Current.Shutdown();
            return;
        }
        AppListenTask();
    }

    private void AppListenTask()
    {
        Task.Run(() =>
        {
            AppEventWait.Reset();
            AppEventWait.WaitOne();
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                App.Current.MainWindow.Activate();
                App.Current.MainWindow.WindowState = WindowState.Maximized;
            }));
            AppListenTask();
        });
    }
}
```

很长一段时间，我一直用上面的方法实现 WPF 单实例应用，后来发现还有更好更优雅的方式：

```
public partial class App : Application
{
    public EventWaitHandle AppEventWait { get; set; }
    private const int SW_SHOW = 5;

    [DllImport("User32.dll")]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);

    [DllImport("User32.dll", EntryPoint = "SetForegroundWindow")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern void SwitchToThisWindow(IntPtr hWnd, bool fUnknown);

    /// <summary>
    /// 开启单实例应用
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        App.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        String mutexName = "f7eeb38e-3ede-4fdb-8891-b285bff816e8";
        AppEventWait = new EventWaitHandle(false, EventResetMode.AutoReset, mutexName, out var isNewInstance);
        if (!isNewInstance)
        {
            var processes = Process.GetProcessesByName("App 进程名称");
            if (processes != null)
            {
                foreach (Process process in processes)
                {

                    ShowWindowAsync(process.MainWindowHandle, SW_SHOW);
                    SetForegroundWindow(process.MainWindowHandle);
                    SwitchToThisWindow(process.MainWindowHandle, true);
                }
            }

            App.Current.Shutdown();
            Environment.Exit(-1);
        }
        else
        {
            base.OnStartup(e);
        }
    }
}
```