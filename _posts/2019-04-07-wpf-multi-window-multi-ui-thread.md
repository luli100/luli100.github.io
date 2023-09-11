---
layout: post
title: WPF 多窗口，多 UI 线程
---

某些 WPF 应用程序需要多个顶级窗口。 一个线程/ Dispatcher 组合可用于管理多个窗口是完全可接受的，但有时多个线程会执行更好的作业。 尤其当这些窗口中的某一个将有可能要独占线程时，更是如此。

Windows 资源管理器以这种方式工作。 每个新资源管理器窗口都属于原始进程，但它是在独立线程的控件下创建的

使用 WPF Frame 控件，可以显示网页。 我们可以轻松创建简单的 Internet Explorer 替换。 让我们从一个重要功能开始：打开新资源管理器窗口的能力。 当用户单击“新建窗口”按钮时，我们将在单独的线程中启动窗口的副本。 这样一来，在其中一个窗口中的长时间运行或阻塞操作将不会锁定其他窗口。

在实际情况下，Web 浏览器模型自身拥有复杂的线程模型。 由于大多数读者都熟悉它，所以我们选择它。

以下示例显示了代码。

```
<Window x:Class="SDKSamples.Window1"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="MultiBrowse"
        Height="600" 
        Width="800"
        Loaded="OnLoaded">
    <StackPanel Name="Stack" 
        Orientation="Vertical">
        <StackPanel Orientation="Horizontal">
            <Button Content="New Window"
                    Click="NewWindowHandler"/>
            <TextBox Name="newLocation"
                     Width="500"/>
            <Button Content="GO!"
                    Click="Browse" />
        </StackPanel>

        <Frame Name="placeHolder"
               Width="800"
               Height="550"/>
    </StackPanel>
</Window>
```

```
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using System.Threading;

namespace SDKSamples
{
    public partial class Window1 : Window
    {

        public Window1() : base()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            placeHolder.Source = new Uri("http://www.msn.com");
        }

        private void Browse(object sender, RoutedEventArgs e)
        {
            placeHolder.Source = new Uri(newLocation.Text);
        }

        private void NewWindowHandler(object sender, RoutedEventArgs e)
        {
            Thread newWindowThread = new Thread(new ThreadStart(ThreadStartingPoint));
            newWindowThread.SetApartmentState(ApartmentState.STA);
            newWindowThread.IsBackground = true;
            newWindowThread.Start();
        }

        private void ThreadStartingPoint()
        {
            Window1 tempWindow = new Window1();
            tempWindow.Show();
            System.Windows.Threading.Dispatcher.Run();
        }
    }
}
```

此代码中的以下线程段对我们来说是最有趣的：

```
private void NewWindowHandler(object sender, RoutedEventArgs e)
{
    Thread newWindowThread = new Thread(new ThreadStart(ThreadStartingPoint));
    newWindowThread.SetApartmentState(ApartmentState.STA);
    newWindowThread.IsBackground = true;
    newWindowThread.Start();
}
```

当单击“新建窗口”按钮时，将调用该方法。 它创建了一个新线程，并以异步方式启动。

```
private void ThreadStartingPoint()
{
    Window1 tempWindow = new Window1();
    tempWindow.Show();
    System.Windows.Threading.Dispatcher.Run();
}
```

此方法是新线程的起点。 我们在此线程的控件下创建了一个新窗口。 WPF 自动创建新的 Dispatcher 以管理新线程。 为了使窗口正常运行，我们需要做的就是启动 Dispatcher 。