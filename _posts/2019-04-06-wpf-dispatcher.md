---
layout: post
title: WPF Dispatcher 是什么，如何使用它(译)
---

本文翻译自 dotnetpattern 上的一篇文章，<a href="http://dotnetpattern.com/wpf-dispatcher">原文地址</a>。

Dispatcher 是什么？ 为什么需要它？ 在这之前，需要理解线程单元『Apartment』是什么？

### 线程单元

进程里的所有对象都分组到线程单元里。有两种类型的线程单元:

1. STA(Single-Threaded Apartments)，单线程单元
2. MTA(Multi-Threaded Apartments)，多线程单元
            
#### 单线程单元(STA)

STA 仅仅包含一个线程。STA 中的所有对象都只能从这个线程接收方法调用。对象不需要同步，因为所有方法调用都是从单个线程同步进行的。

STA 需要一个消息队列来处理来自其他线程的调用。当其他线程调用 STA 线程中的对象时，方法调用将在消息队列中排队，并且 STA 对象将从该消息队列接收调用。

#### 多线程单元(MTA)

MTA 包含一个或多个线程。MTA 中的所有对象都可以从任何线程接收调用。所有对象都负责维护其数据的同步。

### WPF Dispatcher

WPF 应用程序必须在 STA 线程中启动。STA 有一个消息队列来同步其他单元里的方法调用。其他线程无法直接更新 STA 中的对象。必须将方法调用放入消息队列才能更新 STA 中的对象。

**Dispatcher 拥有 STA 线程的消息队列**

当你运行 WPF 应用程序时，它会自动创建一个新的 Dispatcher 对象并调用其 Run 方法。Run 方法用于初始化消息队列。

当 WPF 应用程序启动时，它创建两个线程:

1. 渲染线程
2. UI 线程

UI 线程负责所有用户输入，处理事件，绘制屏幕并运行应用程序代码。渲染线程在后台运行，用于渲染 WPF 界面。

WPF Dispatcher 与 UI 线程相关联。UI 线程在 Dispatcher 对象内调用排队方法。每当更改界面或执行任何事件，或在后台代码中调用方法时，所有这些都发生在 UI 线程中，UI 线程将这些方法排入 Dispatcher 队列里。Dispatcher 将以同步顺序执行这些消息队列里的方法。

#### 所有 WPF 对象是如何引用单个 Dispatcher？

每个 WPF 控件，无论是Window，Button 还是 TextBox，都继承自 DispatcherObject。下面是 Button 类继承结构图。

<img src="/images/button-class-hierarchy.png"/>

当 WPF 创建一个 Button 实例时，会调用 DispatcherObject 的受保护的构造函数。DispatcherObject 获得一个 Dispatcher 类型的属性，它将当前线程的引用保存到DispatcherObject 的 Dispatcher 属性。

### 为什么需要 Dispatcher

WPF 在幕后用 Dispatcher 对象工作，在 UI 线程工作时，我们不需要使用 Dispatcher。

当我们创建一个新的线程来分担工作并想要从该新线程更新UI时，必须需要使用 Dispatcher。只有 Dispatcher 可以从非 UI 线程更新UI中的对象。

#### Invoke

Invoke 方法采用 Action 或委托并同步执行该方法。这意味着在 Dispatcher 完成方法的执行之前它不会返回。

这里是 Invoke 的一个例子：

```C#
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Task.Factory.StartNew(() =>
        {
            InvokeMethodExample();
        });
    }

    private void InvokeMethodExample()
    {
        Thread.Sleep(2000);
        Dispatcher.Invoke(() =>
        {
            btn1.Content = "By Invoke";
        });
    }
}
```
上面的代码将使用 Task.Factory 创建一个新线程并立即启动该线程。在 InvokeMethodExample 中，如果我们尝试直接调用以更新 btn1 对象的 Content 属性。它将抛出 System.InvalidOperationException。我们使用了 Dispatcher 的 Invoke 方法。在 Invoke 方法中，传递 Action 并更新 Button 对象的 Content 属性。它不会抛出任何错误并成功更新 Content 属性。

#### BeginInvoke

BeginInvoke 方法接受一个 Delegate，但它以异步方式执行该方法。这意味着它在调用方法之前理解返回。

```C#
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Task.Factory.StartNew(() =>
        {
            BeginInvokeExample();
        });
    }

    private void BeginInvokeExample()
    {
        DispatcherOperation op = Dispatcher.BeginInvoke(() =>
        {
            btn1.Content = "By BeginInvoke";
        });
    }
}
```

BeginInvoke 返回一个 DispatcherOperation 对象。此对象可用于了解操作的状态是否已完成。它还提供两个事件 Aborted 和 Completed。