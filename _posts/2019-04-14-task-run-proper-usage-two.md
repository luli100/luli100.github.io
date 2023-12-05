---
layout: post
title: Task.Run 的正确使用方式（2）
enable: true
---

我们从一些现有的代码开始，它们以同步方式执行一些复杂计算。

```c#
class MyService
{
    public int CalculateMandelbrot()
    {
        for (int i = 0; i != 10000000; ++i)
        {
            // 一些计算逻辑
        }

        return 100;
    }
}

private void MyButton_Click(object sender, EVentArgs e)
{
    // 会堵塞 UI 线程
    myService.CalculateMandelbrot();
}
```

现在，我们想在 UI 线程中使用它，但是这个方法会堵塞我们的 UI 线程。这是一个应该使用 Task.Run 执行 CPU-bound 操作的地方。

### 别在方法实现中使用 Task.Run

为了不会堵塞 UI 线程，很多新手可能会以下面的方式使用 Task.Run

```c#
class MyService
{
    public Task&lt;int&gt; CalculateMandelbrotAsync()
    {
        return Task.Run(()=>
        {
            for (int i = 0; i != 10000000; ++i)
            {
                // 一些计算逻辑
            }
    
            return 100;
        })
    }
}

private async void MyButton_Click(object sender, EVentArgs e)
{
    // 不会堵塞 UI 线程
    await myService.CalculateMandelbrot();
}
```

咋一看，这似乎解决了问题。它确实解决了这个问题，但是它并没有以最好的方式做到这一点。

### 用 Task.Run 调用方法的正确方式

```c#
class MyService
{
    public int CalculateMandelbrotAsync()
    {
        for (int i = 0; i != 10000000; ++i)
        {
            // 一些计算逻辑
        }

        return 100;
    }
}

private async void MyButton_Click(object sender, EVentArgs e)
{
    // 不会堵塞 UI 线程
    await Task.Run(() => { myService.CalculateMandelbrot(); });
}
```

现在的服务 API 是干净的（它为 CPU-bound 方法公开了一个同步 API），它适用于所有调用者，并且 UI 层负责不堵塞 UI 线程。

结论：<strong>不要在方法实现中使用 Task.Run；而是使用 Task.Run 来调用方法。</strong>

下一篇：<a href="/task-run-proper-usage-three">Task.Run 的正确使用方式（3）</a>