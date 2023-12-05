---
layout: post
title: 如何动态隐藏/显示 WPF 应用程序的控制台窗口
enable: true
---

你曾经是否想过在 WPF 应用程序运行起来后，可以动态打开/关闭控制台窗体。也就是说，你可以在 WPF 应用程序里，留一个后门，用于调试程序。在需要时，打开控制台窗口，查看后台监控数据；不需要时，关闭控制台窗口。

下面的方法应该可以满足你的需求：

- 将 WPF 应用程序输出类型设置为<strong>控制台应用程序</strong>，在 *.csproj 文件里可以看到如下配置。

```
<OutputType>Exe</OutputType>
```

- 添加 NativeConsole 类公开 C++ 函数。

```c++
public static class NativeConsole
{
    [DllImport("kernel32.dll")]
    public static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public const int SW_HIDE = 0;
    public const int SW_SHOW = 5;

    public static void Show()
    {
        var handle = GetConsoleWindow();
        ShowWindow(handle, SW_SHOW);
    }

    public static void Hide()
    {
        var handle = GetConsoleWindow();
        ShowWindow(handle, SW_HIDE);
    }
}
```

- 为了让 WPF 应用程序启动时，默认<strong>隐藏</strong>控制台窗口，可以在 App.xaml.cs 里重写 OnStartup 方法。

```c#
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        //隐藏控制台窗口
        NativeConsole.Hide();
    }
}
```

- 在你想<strong>显示/隐藏</strong>控制台窗口的地方调用下面的方法。

<strong>显示</strong>控制台窗口方法：

```c#
NativeConsole.Show();
```

<strong>隐藏</strong>控制台窗口方法:

```c#
NativeConsole.Hide();
```