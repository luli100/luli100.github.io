---
layout: post
title: Task.Run 的正确使用方式（1）
enable: true
---

要想正确使用 Task.Run，我们得先搞清楚 Task.Run 存在的意义是什么？ <strong>Task.Run 的真正目的是以异步方式执行 CPU-bound 代码。</strong> 为了做到这一点，Task.Run 通过在线程池线程上执行一个方法，该方法执行完后，会返回一个 Task 对象。

这听起来似乎很简单。那为什么会存在错误使用 Task.Run 情况呢？ 我想部分原因在于 <strong>async/await 新手对异步任务(asynchronous tasks）与并行任务（parallel tasks）的理解存在问题。</strong>确实，异步任务和并行任务都是 Task 类型，但它们的主要目的完全不同，因此它们的正确用法也完全不同。

所以，问题仍然存在：应该在哪里使用 Task.Run 呢？<strong>使用 Task.Run 调用 CPU-bound 方法。</strong>这就是全部，就是这样简单。

### 用 Task.Run 创建异步包装器「错」

一个常见的错误是尝试围绕现有的同步方法创建异步“包装器”。这些方法属于“伪异步方法”，因为它们看起来是异步的，但实际上只是通过在后台线程上进行同步工作来伪装它。一般情况下，<strong>不要在方法实现中使用 Task.Run，应该用 Task.Run 调用该方法。</strong>有两个原因:

1. 如果一个方法具有异步签名，您的代码的使用者会认为它将真正异步执行。通过在后台线程上进行同步工作来伪造异步是令人惊讶的行为。
2. 如果曾经在 ASP.NET MVC 控制器上使用过异步方法，那么伪异步方法会导致开发人员走上错误的道路。在服务器端，async/await 的目标是可扩展性，而伪异步方法的可扩展性不如仅使用同步方法。

因此，您希望可重用的任何代码都不应该在其实现中使用 Task.Run。考虑一下使用该代码的开发人员（包括您自己）。

### 在 I/O-bound 异步方法里，用 Task.Run 包装 CPU-bound 部分「错」

让我们把问题整复杂一点，假如我有一个可重用的 I/O-bound 异步方法，但这个异步方法里，存在 CPU-bound 部分代码，我们是否应该用 Task.Run 来包装这部分代码呢？答案仍然是否定的。

在这种（不常见的）情况下，您最终会得到一个有点尴尬的解决方案：一种异步方法，它也执行 CPU-bound 工作。在这种情况下，您应该清楚地记录该方法不是完全异步的，以便调用者知道在必要时用 Task.Run 来调用该异步方法。<strong>（请记住，如果从 UI 线程调用该异步方法时，Task.Run 是必需的；但如果从后台线程或 ASP.NET MVC 控制器调用该异步方法时，则 Task.Run 不是必需的）。</strong>

总而言之，同步方法应该具有同步签名：
```c#
// 该方法是 CPU-bound 同步方法 (在 UI 线程调用时，使用 Task.Run 方法)
void DoWork();
```

异步方法应该有一个异步签名：

```c#
Task DoWorkAsync();
```

同步和异步工作混合的方法应该有一个异步签名，并注释指出它们的部分同步性质：

```c#
// 该方法是 CPU-bound 异步方法 (在 UI 线程调用时，使用 Task.Run 方法)
Task DoWorkAsync();
```

下一篇：<a href="/task-run-proper-usage-two">Task.Run 的正确使用方式（2）</a>