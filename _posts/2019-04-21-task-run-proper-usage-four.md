---
layout: post
title: Task.Run 的正确使用方式（4）
enable: true
---

在 WPF 事件处理器中使用 Task.Run 的方式，是否适用于 ASP.NET MVC 控制器呢，答案是否定的。

### ASP.NET MVC 控制器更喜欢 CPU-bound 的同步方法

```c#
class MyService
{
    public int CalculateMandelbrot()
    {
        // CPU-bound 操作
        for (int i = 0; i != 10000000; ++i)
        {
            // 一些计算逻辑
        }
        return 100;
    }
}
    
public class MandelbrotController: Controller
{
    public ActionResult Index()
    {
        var result = myService.CalculateMandelbrot();
        return View(result);
    }
}
```

到现在为止还挺好。当请求进来时，控制器使用服务（同步）计算视图数据。在该计算期间内，都使用单个 ASP.NET 请求线程。

### ASP.NET MVC 控制器不喜欢 Task.Run 异步包装器

```c#
class MyService
{
    public int CalculateMandelbrot()
    {
        // CPU-bound 操作
        for (int i = 0; i != 10000000; ++i)
        {
            // 一些计算逻辑
        }
        return 100;
    }
}

public class MandelbrotController: Controller
{
    public async Task&lt;ActionResult&gt; IndexAsync()
    {
        var result = await Task.Run(() => { myService.CalculateMandelbrotAsync(); }); 
        return View(result);
    }
}
```

当我们进行测试时，它可以工作！不幸的是，在ASP.NET MVC 控制器中使用 Task.Run 异步包装器会引入了性能问题。

使用原始（同步）代码，从头到尾只使用一个线程来处理请求。这是一个高度优化的 ASP.NET 方案。使用 Task.Run 异步包装器，而不是单个请求线程，会发生以下情况：

- 请求开始在 ASP.NET 线程上进行处理。
- Task.Run 在 ASP.NET 线程池上获取一个线程来完成计算。
- 原始请求线程返回到 ASP.NET 线程池。
- 计算完成后，负责完成计算的那个线程返回到 ASP.NET 线程池。

这可以正常工作，但它根本没有效率，因为存在线程切换和线程回收过程。

只要你在 ASP.NET MVC 中使用带 await 的 Task.Run， 至少会引入 3 个效率问题：

- 不必要的线程切换。同样，当该线程完成请求时，它必须进入请求上下文（这不是实际的线程切换，但确实有开销）。
- 创建了不必要的垃圾。异步编程是一种权衡：以更高的内存使用为代价获得更高的响应能力。在这种情况下，你最终会为完全不必要的异步操作创建更多垃圾。
- ASP.NET 不能提前终止请求，即，如果客户端断开连接或请求超时。在同步情况下，ASP.NET 知道请求线程并且可以终止它。在异步情况下，ASP.NET 不知道辅助计算线程是“用于”该请求的。可以通过使用取消令牌来解决此问题，但这超出了本博文的范围。

如果您多次调用 Task.Run，则性能问题会更加复杂。在繁忙的服务器上，这种实现会扼杀可伸缩性。

这就是为什么 ASP.NET 的原则之一是避免使用线程池线程（当然除了 ASP.NET 给你的请求线程）。更重要的是，这意味着 ASP.NET 应用程序应该避免使用 Task.Run。

现在我，们知道该实现存在什么问题。显而易见的事实是：<strong>如果服务 API 是 CPU-bound 方法，则 ASP.NET 更喜欢同步方法；如果服务 API 是 I/O-bound 方法，也许 ASP.NET 更喜欢异步方法。</strong>对于其他场景也是如此：控制台应用程序、桌面应用程序中的后台线程等。事实上，我们真正需要异步计算的唯一地方就是我们从 UI 线程调用它的时候。