---
layout: post
title: C# 死锁--嵌套锁
enable: true
---

本文将介绍如何理解死锁，死锁的常见类型，如何解决死锁，如何调试死锁，以及避免死锁的最佳实践。

### 什么是死锁

C# 中的死锁是指两个或多个线程在执行过程中被冻结，因为它们正在等待彼此完成。例如，线程 A 正在等待由线程 B 持有的 locker2，线程 B 不能完成并释放 locker2，因为它正在等待由线程 A 持有的 locker1。例如死锁案例 -- 嵌套锁：

```c#
internal class Program
{
    static void Main(string[] args)
    {
        Object locker1 = new Object();
        Object locker2 = new Object();

        var tasks = new List<Task>();
        var task1 = Task.Run(() =>
        {
            lock (locker1)
            {
                Thread.Sleep(1000);
                lock (locker2)
                {
                    Console.WriteLine("Task 1 Finished.");
                }
            }
        });
        tasks.Add(task1);

        var task2 = Task.Run(() =>
        {
            lock (locker2)
            {
                Thread.Sleep(1000);
                lock (locker1)
                {
                    Console.WriteLine("Task 2 Finished.");
                }
            }
        });
        tasks.Add(task2);

        Task.WaitAll(tasks.ToArray());
        Console.WriteLine("All Taskes Finished.");
    }
}
```
task1 获取 locker1 休眠 1 秒，然后等待 locker2 被释放。task2 获取 locker2 后休眠 1 秒，然后等待 locker1 被释放。它们都无限期等待，并导致死锁。Task.WaitAll 等待 task1 和 task2 两个任务都完成，这也导致死锁，因为 task1 和 task2 永远无法完成。

上面这种死锁场景，只要稍稍努力，就很容易发现。然而，在更复杂的场景下，死锁是很难一下子被发现。在了解更复杂的场景前，我们先来看一下如何调试死锁。

### 使用 Threads 窗口调式死锁

在 Visual Studio 运行上面代码将导致挂起。点击 Debug -> Break All，然后点击 Debug -> Windows -> Threads，双击 Threads 里的对应的项，你可以看到如下内容,

WaitAll 卡死：

<img src="/images/deadlock-waitwall.png" width="80%">

lock(locker2) 卡死：

<img src="/images/deadlock-task1.png" width="80%">

lock(locker1) 卡死：

<img src="/images/deadlock-task2.png" width="80%">

这就是死锁在调试中的样子。正如你所看到的，主线程被卡在 Task.WaitAll 上。其它两个线程被卡在内部 lock 语句上。

### 使用 Tasks 窗口调试死锁

你使用过“Tasks 窗口”？点击 Debug->Windows->Tasks 可以调出“Tasks 窗口”。它显示当前正在运行的所有任务。包括它们的状态、开始时间、持续时间、代码中的位置等等。它最酷的一点就是能自动检测死锁。下面是调试死锁的效果：

lock(loker2) 卡死：

<img src="/images/tasks-window-task1.png" width="80%">

lock(locker1) 卡死：

<img src="/images/tasks-window-task2.png" width="80%">

使用“任务窗口”的注意事项是，它只能显示 Task。这就是为什么你在这里看不到 UI 线程，用 Thread 和 ThreadPool 类创建的线程也是看不到的。为了感受它的全部威力，你可以看这个更复杂的多线程场景：[Visual Studio .NET Debugging - Parallel Stacks and Tasks](https://www.youtube.com/watch?v=S9Ht9Npy1Cw)。

### 解决嵌套死锁问题

解决嵌套死锁问题，最简单的方式是：不要在锁中使用锁。但这并不总是有效的。例如，假设每个锁代表一个 Account。我们希望对账户上的每个操作使用锁定。当我们对这两个账户执行操作时（比如转账），我们希望同时锁定这两个账户。

#### 方案1--以相同的顺序嵌套锁

如果我们按照相同的顺序嵌套锁，就不会出现死锁问题。模拟转账账户锁定问题：
```c#
public class Account
{
    public Int32 Id { get; set; }
}

internal class Program
{
    static void Main(string[] args)
    {
        var acc1 = new Account() { Id = 1};
        var acc2 = new Account() { Id = 2 };

        var tasks = new List<Task>();
        var task1 = Transfer(acc1, acc2, 500);
        tasks.Add(task1);
        var task2 = Transfer(acc2, acc1, 1000);
        tasks.Add(task2);

        Task.WaitAll(tasks.ToArray());
        Console.WriteLine("All Taskes Finished.");
    }

    private static Task Transfer(Account acc1, Account acc2, Int32 amount)
    {
        var locker1 = acc1.Id < acc2.Id ? acc1 : acc2;
        var locker2 = acc1.Id < acc2.Id ? acc2 : acc1;
        return Task.Run(() =>
        {
            lock(locker1)
            {
                Thread.Sleep(1000);
                lock(locker2)
                {
                    Console.WriteLine($"Finished transferring amount: {amount}");
                }
            }
        });
    }
}
```

由于所有转账的外部锁都是相同的，所以不存在死锁。其中一个线程将在外部锁中等待，直到第一个线程完成，然后继续。

#### 方案2--超时

解决这个问题的另一种方法时在等待释放锁时，使用超时机制。如果在一段时间内锁没有被释放，则取消操作。它可以被再次放入队列或类似的地方，收稍后再执行。模拟转账方法更改如下：

```c#
private static Task Transfer(Account acc1, Account acc2, Int32 amount)
{
    return Task.Run(() =>
    {
        
        while (true)
        {
            try 
            {
                if (Monitor.TryEnter(acc1, TimeSpan.FromMilliseconds(100)))
                {
                    if (Monitor.TryEnter(acc2,TimeSpan.FromMilliseconds(100)))
                    {
                        Console.WriteLine($"Finished transferring amount: {amount}");
                        return;
                    }
                }
            } 
            finally
            {
                var 
                if (Monitor.IsEntered(acc1))
                {
                    Monitor.Exit(acc1);
                }
                if (Monitor.IsEntered(acc2))
                {
                    Monitor.Exit(acc2);
                }
            }
        }
    });
}
```

记住，lock 语句实际上是 Monitor.Enter 和 Monitor.Exit 的语法糖。使用 Monitor.TryEnter 可以将 Timeout 作为参数传递。这意味着，如果锁定未能在超时内获取，则返回 False。

从理论上讲，使用这种方法可能总是无法执行某个操作--当两个线程同时获取外部锁时，则无法获取内部锁。但实际上，这几乎时不可能的。线程切换机制将在每次不同的时间进行。

值得一提的是，现代应用程序中可以通过 [Event Sourcing](https://martinfowler.com/eaaDev/EventSourcing.html) 模式来完全避免死锁。

### 查找 C# 死锁

上面的死锁问题，只要稍稍努力，很容易识别。但有些场景下，一下子识别死锁还是比较困难的。事实上，在 C# 中要识别死锁，你应该查找如下地方：

- lock 语句
- 使用 AutoResetEvent, Mutex, Semaphore, EventWaitHandle 时的 WaitOne 方法
- 使用 Task 时的 WaitAll 和 WaitAny 方法
- 使用 Task 时的 .Result 和 .GetAwaiter().GetResult 以及 .Wait 方法
- 使用 Thread 时的 Join 方法
- 使用 WPF 编程时的 Dispatcher.Invoke 方法

当你看到调试器的执行点卡在上面任何一个点时，很有可能出现死锁。

### 参考资料

[C# Deadlocks in Depth - Part 1](https://michaelscodingspot.com/c-deadlocks-in-depth-part-1/)