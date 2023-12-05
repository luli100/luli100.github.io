---
layout: post
title: Task.Run 的正确使用方式（3）
enable: true
---

在<a href="/task-run-proper-usage-two"> Task.Run 正确使用方式（2）</a>里，研究了为什么不应该在 CPU-bound 方法实现里使用 Task.Run。相反，我们应该使用 Task.Run 调用该方法。

让我们考虑一个更复杂的场景：<strong>服务 API 包括获取数据（I/O-bound）和分析处理数据（CPU-bound）两部分。</strong>

### 不理想的同步代码

```c#
class MyService
{
    public int PredictStockMarket()
    {
        // I/O-bound 操作
        Thread.Sleep(1000);

        // CPU-bound 操作
        for (int i = 0; i != 10000000; ++i)
        {
            // 一些计算逻辑
        }

        // 更多的 I/O-bound 操作
        Thread.Sleep(1000);

        // 更多的 CPU-bound 操作
        for (int i = 0; i != 10000000; ++i)
        {
            // 一些计算逻辑
        }

        return 100;
    }
}
```

现在，我们想使用异步代码，所以可以用异步 I/O 替换堵塞 I/O。但我们如何处理 CPU-bound 部分呢？

一个常见的错误是将它们包装到 Task.Run 里

### 别在方法实现中使用 Task.Run

```c#
class MyService
{
    public async Task&lt;int&gt; PredictStockMarketAsync()
    {
        // I/O-bound 操作
        await Task.Delay(1000);
    
        // CPU-bound 操作
        await Task.Run(() =>
        {
            for (int i = 0; i != 10000000; ++i)
            {
                // 一些计算逻辑
            }
        });
    
        // 更多的 I/O-bound 操作
        await Task.Delay(1000);
    
        // 更多的 CPU-bound 操作
        await Task.Run(() =>
        {
            for (int i = 0; i != 10000000; ++i)
            {
                // 一些计算逻辑
            }
        });
    
        return 100;
    }
}
```

这是异步方法仍然存在着在方法实现里使用 Task.Run 带来的所有问题。

好吧，API 不能是异步的（因为它有 CPU-bound 部分），也不能是同步的（因为我们想使用异步 I/O）。因此，不幸的是，这里没有理想的解决方案。需要明确的是，我们谈论的是一种极其罕见的边缘情况。绝大多数服务要么是异步的，要么是 CPU-bound 的，而不是两者兼而有之。

### 带有异步签名 CPU-bound 的异步方法

目前，最好的解决方案是使用异步签名，但要清楚地记录该方法是 CPU-bound 的异步方法，这样它的性质就不会令人惊讶。

```c#
class MyService
{
    /// <summary>
    /// 该异步方法是 CPU-bound!
    /// </summary>
    public async Task&lt;int&gt; PredictStockMarketAsync()
    {
        // I/O-bound 操作
        await Task.Delay(1000);
    
        // CPU-bound 操作
        for (int i = 0; i != 10000000; ++i)
        {
            // 一些计算逻辑
        }
    
        // 更多的 I/O-bound 操作
        await Task.Delay(1000);
    
        // 更多的 CPU-bound 操作
        for (int i = 0; i != 10000000; ++i)
        {
            // 一些计算逻辑
        }
    
        return 100;
    }
}
```

这允许基于 UI 的客户端正确使用 Task.Run 来调用服务 API，而基于 ASP.NET 客户端将直接调用该方法。

```c#
private async void MyButton_Click(object sender, EventArgs e)
{
    await Task.Run(() => myService.PredictStockMarketAsync());
}

public class StockMarketController: Controller
{
    public async Task&lt;ActionResult&gt; IndexAsync()
    {
        var result = await myService.PredictStockMarketAsync();
        return View(result);
    }
}
```

在具有异步签名的方法中执行 CPU-bound 工作并不理想，但它确实允许每个可能的客户端以对他们最有意义的方式使用服务 API。每个客户端都充分利用自己的线程情况。

总而言之，即使在罕见和复杂的情况下，最好还是<strong>在方法调用时使用 Task.Run，而不是在方法实现中使用 Task.Run。</strong>

下一篇：<a href="/task-run-proper-usage-four">Task.Run 的正确使用方式（4）</a>