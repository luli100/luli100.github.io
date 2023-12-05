---
layout: post
title: WPF 中的死锁
enable: true
---

桌面应用程序的一个关键点：**不要堵塞 UI 线程**。与 UI 进行任何交互，比如单击按钮，都从 UI 线程开始执行事件。当你在 UI 线程上做事情的时候，你的窗口会完全冻结。因此，我们通常会在另一个线程执行某些操作，就不会冻结。然而，在另一个线程上的操作完成后，我们可能需要更改 UI。任何 UI 的更改都应该在 UI 线程上进行。这意味着我们需要回到 UI 线程。这个问题已经造成了数不清的死锁，例如：

```c#
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        Task.Run(() =>
        {
            // long operation on another thread.
            Thread.Sleep(1000);
            this.Dispatcher.Invoke(() =>
            {
                this.Title = "Download Finished";
            });
        }).Wait();
        Debug.WriteLine("Do more operation...");
    }
}
```

Dispatcher.Invoke() 方法在 UI 线程上同步执行。.Wait() 等待任务完成，它使 UI 线程保持忙碌（堵塞）。但是，该任务正在等待释放 UI 线程以便完成 Dispatcher.Invoke() 同步调用。因此，.Wait() 与 Dispatcher.Invoke() 陷入僵局。

### 方案 #1--将任务后面的内容移到任务里面

[task].Wait() 用于在任务之后执行其他操作。但是，为什么不直接在委托中编写额外的代码呢？你这可能是你想到的第一个方案：

```c#
private void OnButtonClick(object sender, RoutedEventArgs e)
{
    Task.Run(() =>
    {
        // long operation on another thread.
        Thread.Sleep(1000);
        this.Dispatcher.Invoke(() =>
        {
            this.Title = "Download Finished";
        });

        Debug.WriteLine("Do more operation...");
    });
}
```

### 方案 #3--Dispatcher.BeginInvoke

Dispatcher.Invoke 会在 UI 线程上执行同步的委托调用并等待完成。而 Dispatcher.BeginInvoke 会在 UI 线程执行异步的委托调用，不会等待它完成。使用 Dispatcher.BeginInvoke 方法替换 Dispatcher.Invoke 方法解决死锁问题的方案：

```c#
private void OnButtonClick(object sender, RoutedEventArgs e)
{
    Task.Run(() =>
    {
        // long operation on another thread.
        Thread.Sleep(1000);
        this.Dispatcher.BeginInvoke(() =>
        {
            this.Title = "Download Finished";
        });
    }).Wait();
    Debug.WriteLine("Do more operation...");
}
```

### 方案 #2--ContinueWith

另一种方法是使用 ContinueWith 方法：

```c#
private void OnButtonClick(object sender, RoutedEventArgs e)
{
    Task.Run(() =>
    {
        // long operation on another thread.
        Thread.Sleep(1000);
        this.Dispatcher.Invoke(() =>
        {
            this.Title = "Download Finished";
        });
    }).ContinueWith((t)=>
    {
        Debug.WriteLine("Do more operation...");
    }, TaskScheduler.FromCurrentSynchronizationContext());
}
```

你将创建一个更好的工作单元逻辑分离。你可以控制它是否在 UI 线程上执行。TaskScheduler.FromCurrentSynchronizationContext() 方式使其在 UI 线程上执行。

### 方案 #4--Async/Await

Async/Await 是一个异步编程泛型的强大工具。其思想是你可以用同步方式编写异步代码。下面是一个 Async/Await 的解决方案：

```c#
private async void OnButtonClick(object sender, RoutedEventArgs e)
{
    await Task.Run(() =>
    {
        // long operation on another thread.
        Thread.Sleep(1000);
    });

    this.Title = "Download Finished";
    Debug.WriteLine("Do more operation...");
}
```

此方案要好得多。代码是异步，但是编写起来像简单的同步调用。值得注意的是，在较低的 C# 版本是不具备 Async/Await 功能的。

### 使用 Task 窗口进行调试

