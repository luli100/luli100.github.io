---
layout: post
title: 如何创建 WPF 单实例应用程序
enable: true
---

你曾经是否遇到过只准打开一个 WPF 应用程序实例这样的需求，如果有这样的需求场景，下面的方法应该可以满足你的需求：

```C#
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

```C#
public partial class App : Application
{
    private const String MUTEX_NAME = "f7eeb38e-3ede-4fdb-8891-b285bff816e8";
    private EventWaitHandle? _eventEaitHandle;
    private const Int32 SW_SHOW = 5;

    [DllImport("User32.dll")]
    private static extern Boolean ShowWindowAsync(IntPtr hWnd, Int32 cmdShow);

    [DllImport("User32.dll")]
    private static extern Boolean SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern void SwitchToThisWindow(IntPtr hWnd, Boolean fUnknown);

    /// <summary>
    /// 开启单实例应用
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        App.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        this._eventEaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, MUTEX_NAME, out var newInstance);
        if (!newInstance)
        {
            var currProcess = Process.GetCurrentProcess();
            var processes = Process.GetProcessesByName(currProcess.ProcessName);
            if (processes is not null)
            {
                var lastProcess = processes.FirstOrDefault((t) => { return t.Id != currProcess.Id; });
                if (lastProcess is not null)
                {
                    ShowWindowAsync(lastProcess.MainWindowHandle, SW_SHOW);
                    SetForegroundWindow(lastProcess.MainWindowHandle);
                    SwitchToThisWindow(lastProcess.MainWindowHandle, true);
                    App.Current.Shutdown();
                    Environment.Exit(-1);
            
                    return;
                }
            }
        }
    }
}
```