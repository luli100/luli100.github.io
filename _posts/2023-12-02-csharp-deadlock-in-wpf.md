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

### 方案 #2--Dispatcher.BeginInvoke

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

### 方案 #3--ContinueWith

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

此方案要好得多。代码是异步，但是编写起来像简单的同步调用。值得注意的是，在较低的 C# 版本是不具备 async/await 功能的。

### SynchronizationContext 死锁

咋一看，async/await 模式似乎很好。但是，就像编程中几乎所有东西一样，它需要被彻底理解才能正确地使用。当你在同步方法中使用异步方法时，一个潜在的危险就会显现。在这些情况下，你可以使用 .Result 属性调用异步方法，等待异步方法完成，然后获取结果。例如，

```c#
private void OnButtonClick(object sender, RoutedEventArgs e)
{
    var x = Do().Result;
}

private async Task<Int32> Do()
{
    await Task.Delay(1000);
    return 100;
}
```

看上去者没有什么问题。执行时，这实际上会导致死锁。你知道为什么？为了理解它，我们必须深入地研究一下 async/await 的内部机制。

重要的问题是：Do() 方法 await 后的其余部分将在哪个线程上执行？async/await 被设计为允许你异步等待一个方法，然后在相同的上下文中执行其余代码。换句话说，如果从 UI 线程调用，等待之后的代码也应该在 UI 线程中执行。

async/await 使用当前线程的 SynchronizationContext 来知道在哪里执行该方法的其余部分。当从 UI 线程中调用时，其 SynchronizationContext 将添加到 Dispatcher 队列中。其他线程没有 SynchronizationContext，在这种情况下，有一个默认的 SynchronizationContext。这意味着该方法的其余部分将继续在同一个线程或 ThreadPool 线程上执行。

此处的解释可能过分简化了概念。SynchronizationContext、TaskScheduler 和 async/await 背后的技术由于许多边缘情况而变得复杂。为了更全面的解释，这里有一些很好的文章：[Asynchronous programming with async and await](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/) 和 [Await, SynchronizationContext, and Console Apps](https://devblogs.microsoft.com/pfxteam/await-synchronizationcontext-and-console-apps/)。

让我们回到先前的死锁代码。OnButtonClick 事件处理器在 UI 线程上执行。然后，我们同步调用 Do 方法，这使得 UI 线程繁忙（堵塞）。await 完成后，它使用当前的 SynchronizationContext(从 UI 线程捕的同步上下文)，该上下文使用 Dispatcher 队列尝试在 UI 线程上运行它。但是，UI 线程卡住了，因为我们使用同步方式调用了 Do 方法，结果导致死锁。

### 解决 SynchronizationContext 死锁问题

你可以有三种方法解决 SynchronizationContext 死锁问题：

#### #1 - ConfigureAwait(false)

你可以使用 .ConfigureAwait() 方法配置是否捕获 SynchronizationContext。例如：await Task.Delay(1000).ConfigureAwait(false) - 意味着使用默认的 SynchronizationContext，它通常会在 ThreadPool 线程上继续，即使从 UI 线程调用也是如此。

```c#
private void OnButtonClick(object sender, RoutedEventArgs e)
{
    var x = Do().Result;
    this.Title = x.ToString();
}

private async Task<Int32> Do()
{
    await Task.Delay(1000).ConfigureAwait(false);
    return 100;
}
```

当然，这样操作的后，你就不能在 await 后面直接更新 UI，下面的代码会报异常：System.AggregateException: 'One or more errors occurred. (The calling thread cannot access this object because a different thread owns it.)'。

```c#
private void OnButtonClick(object sender, RoutedEventArgs e)
{
    var x = Do().Result;
    this.Title = x.ToString();
}

private async Task<Int32> Do()
{
    await Task.Delay(1000).ConfigureAwait(false);
    this.Title = "Finished"; // error
    return 100;
}
```

#### #2 - async 调用

将 OnButtonClick 事件处理器改为 async 调用：

```c#
private async void OnButtonClick(object sender, RoutedEventArgs e)
{
    var x = await Do();
    this.Title = x.ToString();
}

private async Task<Int32> Do()
{
    await Task.Delay(1000);
    return 100;
}
```

为什么这种方法有效？因为在调用 await Do() 时，UI 线程将被释放，等待方法完成后，this.Title = x.ToString(); 部分才会被添加到 UI 队列里。

#### #3 - 从非 UI 线程中调用

有时候，将整个调用堆栈改为异步方法调用是不可能的，因此可以从非 UI 线程调用 Do 方法：

```c#
private void OnButtonClick(object sender, RoutedEventArgs e)
{
    Int32 x;
    Task.Run(() =>
    {
        x = Do().Result;
    }).Wait();

    this.Title = "Finished";
}

private async Task<Int32> Do()
{
    await Task.Delay(1000);
    return 100;
}
```

虽然 Wait 方法仍然会堵塞 UI 线程，但 Do 方法是在线程池线程调用的，它没有 SynchronizationContext，该方法的其余部分也在线程池线程上执行，所以不会导致死锁。

### 参考资料

[C# Deadlocks in Depth – Part 2](https://michaelscodingspot.com/c-deadlocks-in-depth-part-2/)

