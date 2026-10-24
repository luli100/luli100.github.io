---
layout: post
title: WPF 中的死锁
enable: true
toc:
  - title: 先分清卡顿、死锁和饥饿
    anchor: classify
  - title: WPF 的线程与调度模型
    anchor: threading-model
  - title: 同步等待 UI 工作
    anchor: sync-wait
  - title: 同步等待异步方法
    anchor: sync-over-async
  - title: 锁与 Dispatcher 的组合
    anchor: locks
  - title: 纯异步代码中的死锁
    anchor: async-cycles
  - title: 回调与集合绑定
    anchor: binding
  - title: 初始化、取消与关闭
    anchor: lifecycle
  - title: 多 UI 线程与原生互操作
    anchor: multiple-ui
  - title: 嵌套消息循环与重入
    anchor: reentrancy
  - title: 修复手段的适用边界
    anchor: remedies
  - title: 调试与现场诊断
    anchor: diagnostics
  - title: 工程约定与验证
    anchor: verification
  - title: 配套示例与参考资料
    anchor: references
---

WPF 死锁不只发生在 `.Result` 和 `Dispatcher.Invoke` 之间。锁、异步信号量、集合绑定、窗口关闭和跨线程回调，都可能让一项工作等待自己才能提供的资源。

本文从等待关系出发，整理 WPF 应用中主要的死锁机制、容易混淆的挂起现象，以及诊断和修复方法。阅读时可以一直追问：**谁在等谁，完成这项工作需要哪个线程或资源，它又被谁占用？**

正文中的窗口成员代码放在 WPF `Window` 的代码隐藏类中，反例与修复版分别使用；修复版沿用前文明确给出的辅助方法和字段。需要的命名空间包括 `System`、`System.Diagnostics`、`System.Threading`、`System.Threading.Tasks` 和 `System.Windows`；使用 Dispatcher 类型的片段还需要 `System.Windows.Threading`。`Thread.Sleep` 只用于模拟同步工作，不代表真实项目应这样等待。

核心机制同样适用于 .NET Framework WPF；配套验证项目使用 Windows 与 .NET 10。较新的 API 会在相应位置注明，不能把新 API 直接复制到旧框架项目。

## 先分清卡顿、死锁和饥饿 {#classify}

“窗口未响应”是现象，不是诊断结果。

| 现象 | 实质 | 判断线索 |
| --- | --- | --- |
| 同步阻塞或耗时计算 | UI 正在执行工作，或等待一项可以独立完成的工作 | 工作结束后可能恢复，不一定存在等待环 |
| 死锁 | 一组工作互相等待，各方无法按现有逻辑继续 | 能画出闭合的依赖关系，包括锁、任务和执行线程 |
| 调度饥饿 | 工作可以执行，却长期拿不到线程或执行机会 | 大量排队、优先级失衡、线程池线程被占满 |
| 无人完成的等待 | 等待一个未触发的事件、没有完成的任务，或无人处理的队列 | 不一定有等待环，可能是生产者遗漏或已经退出 |
| 重入造成的状态错误 | 操作尚未完成，同一线程又进入相关代码 | 不一定死锁，但状态与预期顺序被破坏 |

死锁也不要求整个程序停止。后台任务可以互相死锁，而窗口仍然能拖动；纯异步任务也可能永远无法完成，却没有一个线程停在阻塞调用上。

经典资源死锁通常需要四个条件同时成立：资源不能无限共享、占有资源后继续等待其他资源、资源不能被任意强行收回、存在循环等待。这里的资源既可以是锁，也可以是“必须由某个 UI 线程执行一次代码”的机会。异步等待改变了等待方式，不会自动消除这些条件，参见 [Are deadlocks still possible with await?](https://devblogs.microsoft.com/dotnet/are-deadlocks-still-possible-with-await/)。

排查时可以把参与者和资源画成一张等待图。本文使用简写：

```text
UI 线程 → 后台任务完成 → UI 回调执行 → UI 线程
任务 A → 信号量 B → 任务 B → 信号量 A → 任务 A
```

修复的目标是拆掉某条必要依赖。单纯改成另一种等待 API，未必改变这张图。

## WPF 的线程与调度模型 {#threading-model}

### UI 线程、Dispatcher 和线程亲和性

WPF 通常有处理应用 UI 的线程和内部渲染线程。渲染线程不代替 UI 线程执行按钮事件、属性设置或普通 Dispatcher 委托。UI 线程忙于一个长方法时，不能同时继续执行队列里的普通工作。

窗口、控件和 `CollectionView` 等对象具有线程亲和性。对象的 `Dispatcher` 指向拥有它的线程；跨线程直接访问通常会抛出异常。`CheckAccess()` 可以检查访问权限，`VerifyAccess()` 会在不满足要求时抛出异常。给 UI 对象加一把 `lock` 不会获得跨线程访问权限。可冻结并已冻结的 `Freezable` 有专门的共享规则，不能由此推导出所有 WPF 对象都可以跨线程使用。

这些背景见 [WPF 线程模型](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model)和 [Freezable 对象概述](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/freezable-objects-overview)。

### 几种调度方式的区别

| 写法 | 调用方的等待方式 | 完成边界与注意事项 |
| --- | --- | --- |
| `dispatcher.Invoke(action)` | 同步等待 | 委托执行完才返回；在所属线程上的某些调用可以直接执行 |
| `dispatcher.BeginInvoke(action)` | 提交后返回 | 返回 `DispatcherOperation`，返回时不代表委托已完成 |
| `dispatcher.InvokeAsync(action)` | 提交后返回 | 可 `await` 操作或其 `Task`，适合观察完成和异常 |
| `SynchronizationContext.Post(...)` | 提交后返回 | 没有直接表示完成结果的返回任务 |
| `SynchronizationContext.Send(...)` | 同步发送 | 必须分析目标上下文；WPF 中可能形成跨线程等待 |

“异步提交”不表示委托在线程池执行。提交给窗口 Dispatcher 的委托仍在该窗口所属的 UI 线程执行，耗时委托一样会卡住界面。具体重载和异常行为应以 [InvokeAsync 文档](https://learn.microsoft.com/en-us/dotnet/api/system.windows.threading.dispatcher.invokeasync)为准。

### 同步上下文和任务调度器

正常的 WPF UI 事件中，`SynchronizationContext.Current` 通常是 `DispatcherSynchronizationContext`。默认 `await Task` 遇到尚未完成的任务时，会考虑捕获当前的自定义同步上下文；没有合适的同步上下文时，还会考虑非默认 `TaskScheduler`。

捕获发生在相应的等待点，不是给整个方法永久绑定一个线程。任务如果已经完成，`await` 可以直接继续执行，不必排队。普通线程池工作通常没有 UI 同步上下文，但自定义线程和调度器可以有自己的规则。`ExecutionContext`、`AsyncLocal` 等上下文流动也不等同于 UI 调度。详细机制见 [ConfigureAwait FAQ](https://devblogs.microsoft.com/dotnet/configureawait-faq/)。

`async` 不会自动创建线程。耗时 CPU 工作通常通过 `Task.Run` 移出 UI；已有异步 API 的网络、文件等 I/O 通常直接 `await`。在异步方法第一次挂起之前执行的同步代码，仍占用调用线程。参见 [异步编程场景](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios)。

`Task.Factory.StartNew` 也不能直接当作 `Task.Run` 的同义词。默认工厂使用当前任务调度器；如果当前正处于 UI 调度器上的任务中，新任务可能仍被安排到 UI。向 `StartNew` 传入异步委托还可能得到嵌套任务，需要正确展开。选择 `Task.Run` 或显式调度器时，应明确执行位置与完成边界，参见 [Task.Run 与 StartNew 的区别](https://devblogs.microsoft.com/dotnet/task-run-vs-task-factory-startnew/)。

## 同步等待 UI 工作 {#sync-wait}

### Task.Wait 与 Dispatcher.Invoke

下面的按钮事件会死锁：

```csharp
private void OnButtonClick(object sender, RoutedEventArgs e)
{
    Task.Run(() =>
    {
        Thread.Sleep(1000);
        Dispatcher.Invoke(() => Title = "Finished");
    }).Wait();
}
```

UI 在线程池任务完成前不返回；线程池任务又必须等 UI 执行标题更新才能完成：

```text
UI：Task.Wait → 等后台任务
后台：Dispatcher.Invoke → 等 UI 执行委托
```

这里根本不需要一把显式的锁。把 `Invoke` 换成一个同样需要 UI 参与的同步 API，或者把 `.Wait()` 换成 `.Result`，仍可能形成同样的环。

推荐让 UI 异步等待，把最后的 UI 更新留在事件处理器中：

```csharp
private async void OnButtonClick(object sender, RoutedEventArgs e)
{
    try
    {
        await Task.Run(() => Thread.Sleep(1000));
        Title = "Finished";
    }
    catch (Exception ex)
    {
        Debug.WriteLine(ex);
        Title = "操作失败";
    }
}
```

当任务尚未完成时，事件处理器在 `await` 处挂起并返回。UI 可以继续调度，任务完成后，事件处理器通过 UI 上下文恢复执行。这里只有一次最终更新，无需在后台委托里再调用 Dispatcher。

### InvokeAsync 之后再同步等待，也会重建等待环

```csharp
private void OnButtonClick(object sender, RoutedEventArgs e)
{
    var operation = Dispatcher.InvokeAsync(() => Title = "Finished");
    operation.Task.Wait(); // 等待一个尚未执行、又需要当前 UI 线程的操作。
}
```

只有“提交”是异步的，调用者随后仍在阻塞 UI。正确的异步写法是 `await operation` 或 `await operation.Task`。

`DispatcherOperation.Wait()`、普通 `Task.Wait()` 与 WPF 的 `DispatcherOperationWait()` 扩展不能混为一谈。WPF 对 Dispatcher 操作提供了特殊同步等待机制，可能涉及嵌套消息处理；`DispatcherOperationWait()` 只适用于关联了 Dispatcher 操作的任务，不是任意任务的通用修复器。普通 UI 业务仍应优先使用 `await`。参见 [DispatcherOperationWait 文档](https://learn.microsoft.com/en-us/dotnet/api/system.windows.threading.taskextensions.dispatcheroperationwait)。

## 同步等待异步方法 {#sync-over-async}

### 没有显式 Invoke，后续代码仍可能需要 UI

```csharp
private void OnButtonClick(object sender, RoutedEventArgs e)
{
    int value = GetValueAsync().Result;
    Title = value.ToString();
}

private async Task<int> GetValueAsync()
{
    await Task.Delay(1000);
    return 100;
}
```

假设延迟在 `await` 时尚未完成：

1. `GetValueAsync` 在 UI 线程开始执行，到 `await` 时捕获 UI 上下文，返回未完成的任务。
2. UI 读取 `.Result`，同步等待该方法完成。
3. 延迟结束，但 `GetValueAsync` 还需要回到 UI 执行 `return 100`。
4. UI 正在等它返回，因此后续代码无法运行。

`Task.Delay` 的任务完成，不等于 `GetValueAsync` 返回的任务已经完成。后者代表整个方法的执行。

同样，如果任务只能由 `DispatcherTimer.Tick`、某个 UI 事件或排队的 UI 回调完成，在 UI 上同步等它，也会阻止这个完成路径运行。不要因为异步方法内部没有显式的 UI 控件访问，就认定它不依赖 UI。

`.GetAwaiter().GetResult()` 可以改变异常的传播形式，但同样会同步阻塞；它不是解决 UI 死锁的替代 API。任务已经完成，或者其整个完成路径都不依赖 UI 时，同步取结果未必死锁，但等待期间仍可能冻结窗口。

### 让异步一直到达可等待的入口

```csharp
private async void OnButtonClick(object sender, RoutedEventArgs e)
{
    try
    {
        int value = await GetValueAsync();
        Title = value.ToString();
    }
    catch (Exception ex)
    {
        Debug.WriteLine(ex);
        Title = "操作失败";
    }
}
```

普通业务方法返回 `Task` 或 `Task<T>`，由调用者继续 `await`，一直传递到事件或其他异步入口。按钮事件采用 `async void` 是事件签名的要求，不应把普通业务方法也改成 `async void`。

同一问题还会隐藏在 `ICommand.Execute`、构造函数、属性 getter、转换器、验证回调和启动流程中。MVVM 只改变代码组织，不会自动改变等待关系。可以使用异步命令、显式的 `InitializeAsync`，或先显示加载状态，再异步填充属性；不要让绑定读取属性时同步等待远程数据。

## 锁与 Dispatcher 的组合 {#locks}

### 持锁跨线程调用

即使 UI 事件已经使用 `await`，仍然可能死锁：

```csharp
private readonly object _gate = new object();
private int _value;

private async Task UpdateWrongAsync()
{
    await Task.Run(() =>
    {
        lock (_gate)
        {
            _value++;
            Dispatcher.Invoke(() =>
            {
                lock (_gate)
                {
                    Title = _value.ToString();
                }
            });
        }
    });
}
```

后台线程持有 `_gate` 并等待 UI 委托完成；UI 委托又需要取得 `_gate`。UI 事件外层是否使用 `await`，已经不能解除这个环。

`lock` 基于 `Monitor`，允许**同一线程**重入；这里委托是在另一个线程执行，因此不是同线程重入。也不要把锁换成 `SemaphoreSlim` 就认为问题消失了。

实际业务里，第二次取锁可能藏在属性 getter、日志目标、事件订阅者或绑定刷新中，不一定像例子一样相邻可见。

### 在锁内取得快照，释放后再调度

```csharp
private async Task UpdateAsync()
{
    int snapshot = await Task.Run(() =>
    {
        lock (_gate)
        {
            _value++;
            return _value;
        }
    });

    await Dispatcher.InvokeAsync(() => Title = snapshot.ToString());
}
```

此处沿用上一段的 `_gate` 和 `_value` 字段。现在后台不会持锁等待 UI，UI 也不必为了展示这个结果再次取锁。如果调用者就在正常的 UI 上下文中，也可以在 `await Task.Run(...)` 之后直接更新标题。

快照只保证取得数据时的一致性。如果多个请求并发，较旧的结果可能最后到达；需要按业务选择版本号、取消旧请求或串行提交，不能把“没有死锁”当成“结果一定最新”。

### 多把锁与读写锁

传统的锁顺序反转同样适用于 WPF：后台先取 A 再取 B，而 UI 先取 B 再取 A，就可能死锁。统一所有路径的锁顺序、缩小临界区，并避免持锁调用不受控制的外部代码。

[`ReaderWriterLockSlim`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.readerwriterlockslim) 还需要检查读锁、升级读锁和写锁的使用协议；不能把任意“读锁内再申请写锁”当成正常升级。`Monitor`、`Mutex`、读写锁、信号量的重入和线程所有权规则不同，不应机械互换。C# 不允许在 `lock` 块内直接 `await`；手写 `Monitor.Enter` 绕开语法限制，也不会让它变成适合跨异步挂起使用的锁。

## 纯异步代码中的死锁 {#async-cycles}

### 同一个 SemaphoreSlim 被嵌套获取

`WaitAsync` 不占住线程，但资源仍然只有一份：

```csharp
private readonly SemaphoreSlim _serial = new SemaphoreSlim(1, 1);

private async Task OuterAsync()
{
    await _serial.WaitAsync();
    try
    {
        await InnerAsync();
    }
    finally
    {
        _serial.Release();
    }
}

private async Task InnerAsync()
{
    await _serial.WaitAsync();
    try
    {
        await Task.Delay(100);
    }
    finally
    {
        _serial.Release();
    }
}
```

`OuterAsync` 已经拿走唯一的许可，却等待 `InnerAsync` 完成；`InnerAsync` 必须等外层释放许可，而释放又在外层等待之后。窗口可能仍然响应，但这个业务操作永远无法结束。

`SemaphoreSlim` 不识别“这是同一个业务调用”，不会因为来自同一调用链就允许重入。参见 [SemaphoreSlim 文档](https://learn.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim)。

可以只在公开入口获取一次许可，把内部实现拆成要求调用方已取得许可的核心方法：

```csharp
private async Task RunSerializedAsync(CancellationToken token)
{
    await _serial.WaitAsync(token);
    try
    {
        await ExecuteCoreAsync(token);
    }
    finally
    {
        _serial.Release();
    }
}

private static async Task ExecuteCoreAsync(CancellationToken token)
{
    await Task.Delay(100, token);
}
```

此处沿用前文的 `_serial` 字段。`try/finally` 放在成功取得许可之后：如果获取许可时就被取消，不能额外 `Release`。获取成功后，异常和取消都必须经过释放路径。核心方法不应再偷偷获取同一个许可。

### 两个异步操作互相等待

两个任务互相 `await`，或者分别持有不同的异步锁再等待对方的锁，同样可以形成环。`Task.WhenAll` 只是组合等待，不会替内部任务解开依赖。

另一种常见写法是：拿着某个许可等待 `TaskCompletionSource`，而唯一能完成这个 source 的方法也必须先取得同一许可：

```text
操作 A 持有许可 → 等完成信号
完成信号的生产者 → 等许可 → 操作 A
```

修复依赖图，而不是给这些 `await` 全部加 `ConfigureAwait(false)`。统一资源获取顺序；在等待对方前释放自己不必继续占有的资源；或者调整协议，让独立的生产者负责完成信号。

### 有界队列的容量也可能成为资源

有界 `Channel<T>` 在等待模式下，队列满时 `WriteAsync` 要等消费者读走数据。按这个契约，如果生产者持有某个许可并等待写入，而消费者必须先获得这个许可才能读取，就形成“生产者等容量、消费者等许可”的环。UI 一边同步等待生产者，一边承担消费工作，也会产生类似问题。

因此背压需要与资源协议一起设计：不要持有消费者需要的资源去等待队列腾出空间，关闭时还要明确谁负责完成写入端、唤醒读者以及传播失败。容量与等待语义见 [Channels 文档](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels)。

### TaskCompletionSource 的隐式续接

`TaskCompletionSource<T>.SetResult` 还可能在完成任务的线程上内联执行消费者的后续代码。这会把看似独立的生产者和消费者连接起来：消费者如果阻塞，就可能反过来卡住设置结果的线程。

用于封装事件或生产者协议时，通常可以显式选择异步运行续接：

```csharp
var completion = new TaskCompletionSource<int>(
    TaskCreationOptions.RunContinuationsAsynchronously);
```

同时尽量在释放内部锁之后通知完成，并为成功、失败、取消和对象关闭设计明确的终态。这个选项不能修复消费者和生产者本来就互相等待的逻辑环，也不保证消费者一定在某个特定线程运行。该选项从 .NET Framework 4.6 起可用。背景见微软的 [TaskCompletionSource 分析](https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/)。

## 回调与集合绑定 {#binding}

### 看不到 Invoke 的跨线程依赖

同步事件、`PropertyChanged`、`CollectionChanged`、进度上报、日志处理器和第三方控件，都可能在内部调用 UI，或者重新获取调用方持有的锁。调用一个普通方法，不代表它一定不会阻塞或回调。

例如：后台持锁修改数据并触发通知；订阅者同步切到 UI；UI 在更新绑定时读取数据，又尝试取同一把锁。表面上是“刷新数据卡住”，实质仍是持锁等待 UI。

优先在锁内完成内部状态修改并取得通知所需的数据，锁外调用外部回调。对于要求“修改和通知原子发生”的协议，则应保留该原子性，重新设计订阅者，避免它同步等待 UI，不能为了缩短锁范围就破坏协议。

`Progress<T>` 通常在创建时捕获同步上下文；因此应在 UI 上创建 UI 进度接收器。它也不能保证“调用 `Report` 返回时，界面已经更新”。自定义 `IProgress<T>` 实现还可能同步执行回调，需要检查具体实现。

### ObservableCollection 与 EnableCollectionSynchronization

一种直接的方案是后台计算独立的数据批次，再让 UI 线程统一修改绑定集合。这样集合的修改和 UI 展示都归属同一个线程；仍应控制批量大小，避免一次刷新长期占用 UI。

如果确实需要跨线程访问同一个绑定集合，可以使用 `BindingOperations.EnableCollectionSynchronization`。它让 WPF 的 `CollectionView` 参与指定的同步机制，**不会自动使集合和所有订阅者都线程安全**。

使用时必须做到：

1. 在拥有对应视图的 UI 线程注册同步机制，并在跨线程使用与绑定都开始之前完成注册。
2. 应用自己的读取、枚举和修改也使用同一套同步机制。
3. 集合修改与对应的 `CollectionChanged` 通知保持原子性。
4. 将 WPF 的取锁纳入等待关系分析，不能持集合锁同步等待 UI。
5. 同一集合用于多个 UI 线程时，各线程分别注册；停止使用时在合适的 UI 线程解除注册。

尤其不要在集合同步回调中，拿着集合锁调用 `Dispatcher.Invoke`，把“取锁”和“切换 UI”互相嵌套。WPF 会维护视图影子副本并异步应用集合变化，后台修改完成不代表界面已经显示。完整契约见 [EnableCollectionSynchronization 文档](https://learn.microsoft.com/en-us/dotnet/api/system.windows.data.bindingoperations.enablecollectionsynchronization)。

## 初始化、取消与关闭 {#lifecycle}

### 构造函数与静态初始化

窗口构造函数、单例初始化和静态字段初始化中的同步等待，都可能挡住后续工作需要的 UI。把 `.Result` 移进单例属性，并没有消除这个依赖。

静态构造函数还涉及类型初始化的同步：初始化线程启动工作并同步等待，工作又访问尚未初始化完成的类型，就可能产生新的等待环。不要在静态初始化中等待线程、任务或跨线程调用。参见 [静态构造函数文档](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/static-constructors)。

可把耗时初始化移到显式的异步入口，并处理重复加载、初始化失败和窗口提前关闭。`Loaded` 不应被无条件当成“进程中只触发一次”的初始化保证。

### 关闭时等待后台退出

常见的关闭死锁是：UI 在 `Closing` 中 `.Wait()` 或 `Join()`；后台在退出前还要向 UI 提交最后一次更新；两者互等。

```text
Closing → 等后台退出
后台退出流程 → 等最后一次 UI 更新 → Closing 所在 UI
```

应在 Dispatcher 仍然可工作的阶段先请求停止，然后异步等待后台结束，最后真正关闭窗口。下面演示由本窗口管理关闭的两阶段流程；`Closing` 事件需要绑定到 `OnClosing`：

```csharp
private readonly CancellationTokenSource _stop = new CancellationTokenSource();
private Task _worker = Task.CompletedTask;
private bool _closing;
private bool _allowClose;

private async void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
{
    if (_allowClose)
        return;

    e.Cancel = true; // 必须在第一次 await 之前阻止本次关闭。
    if (_closing)
        return;

    _closing = true;
    try
    {
        _stop.Cancel(); // 约定取消回调保持短小，不同步等待 UI。
        await _worker;
    }
    catch (OperationCanceledException) when (_stop.IsCancellationRequested)
    {
        // 本窗口请求的正常取消。
    }
    catch (Exception ex)
    {
        Debug.WriteLine(ex);
        _closing = false;
        return; // 本例选择保留窗口，交由用户或上层决定如何恢复。
    }

    _stop.Dispose();
    _allowClose = true;
    Close();
}
```

这里假设 `_worker` 保存本窗口拥有的后台任务，任务按协议观察 `_stop.Token`，并且不会在关闭期间再启动新工作。真正项目还需要停止接收新请求、解除订阅，以及明确超时后的产品行为。应用级 `Shutdown`、系统会话结束和其他订阅者取消关闭，不等同于这个单窗口流程，需要统一的应用生命周期管理。

不要等到 Dispatcher 已经停止后，再要求它执行清理委托；也不要用“进程最终会退出”代替资源所有权和退出协议。

### 取消与超时不是强制终止

`CancellationTokenSource.Cancel()` 会同步执行已注册的取消回调。回调如果取锁、同步调度 UI 或等待任务，取消这个动作本身就可能阻塞或死锁。取消回调应简短地通知状态，不应承担复杂的同步清理。参见 [Cancel 文档](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource.cancel)。

较新运行时的 [`CancelAsync`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource.cancelasync) 可以改变回调执行方式，但如果调用者持有回调必需的资源并等待其结束，逻辑环仍然存在。取消是协作协议，不会自动释放别人持有的锁。

`.Wait(timeout)`、`Task.WhenAny`，以及 .NET 6 起的 [`Task.WaitAsync`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.waitasync)，可以限制某个等待者等待多久，但通常不会自动停止底层工作。超时返回后，原任务仍可能继续写数据、持有资源或失败，必须设计后续观察和收尾。不要在未确认底层工作终止时，盲目重试非幂等操作。

## 多 UI 线程与原生互操作 {#multiple-ui}

### 两个 Dispatcher 互相同步调用

WPF 允许多个 UI 线程，每个线程拥有自己的 Dispatcher 和 UI 对象。线程 A 的窗口不归线程 B 的 Dispatcher 管理。

```text
UI A 执行 A.Dispatcher 的事件
  → B.Dispatcher.Invoke
      → B 上的委托又调用 A.Dispatcher.Invoke
          → A 正在等 B
```

避免两个 UI 线程同步来回调用。跨窗口交换可复制的数据，以异步请求和明确的结果任务通信；异步通信本身也必须避免任务互相等待。线程应按需设置 STA 并运行消息循环，关闭时先结束通信，再关闭各自的 Dispatcher。

### CurrentDispatcher 不等于主界面的 Dispatcher

在线程池里读取 `Dispatcher.CurrentDispatcher`，得到的是当前工作线程的 Dispatcher；如果此前没有，还会创建一个。它不会替你找到主窗口，也不会自动启动消息循环。

向这样一个无人运行消息循环的 Dispatcher 投递操作，操作可能一直无法执行。这通常属于队列没有消费者，不一定是有环的死锁。应显式传入目标窗口的 Dispatcher；只有确实要访问应用主 UI 时，才使用应用对应的 Dispatcher。参见 [CurrentDispatcher 文档](https://learn.microsoft.com/en-us/dotnet/api/system.windows.threading.dispatcher.currentdispatcher)。

### COM、Win32、WinForms 和第三方控件

`SendMessage`、COM STA 调用、WinForms `Control.Invoke` 和某些 SDK 的同步 API，都可能引入隐式的跨线程调用。后台没有出现 `Dispatcher.Invoke`，不等于它不依赖 UI 或某个 STA 线程。

这类问题需要同时查看托管与原生栈，并确认对象的 apartment、调用线程和框架的消息泵规则。`Task.Run` 使用线程池，不能自动把一个要求 STA 或要求创建线程的组件变成可在线程池调用的组件。相关约束可参考微软的[线程规则](https://microsoft.github.io/vs-threading/docs/threading_rules.html)。

## 嵌套消息循环与重入 {#reentrancy}

`ShowDialog`、`MessageBox.Show` 以及 `Dispatcher.PushFrame` 可能在当前调用尚未返回时继续处理消息。因此，“一个方法还没返回”不总等于“这个 UI 线程绝对不能处理任何工作”。某些 CLR、COM 等待也会处理特定消息，不能把它们当成完整、等价的 Dispatcher 循环。

这解释了两个容易误判的现象：

- 某种同步等待在测试里似乎不死锁，不代表换个调用位置、优先级或原生组件后仍然成立。
- 弹出对话框期间，其他回调可能重新进入尚未完成的操作。`lock` 对同一线程可重入，未必能保护“操作完成之前不会再执行”的业务假设。

自行循环调用 `DoEvents` 或 `PushFrame`，可能让眼前的等待继续推进，同时允许点击、关闭、绑定和其他回调在中间状态执行。它不提供原本异步流程的生命周期、错误处理和重入控制。

`Dispatcher.DisableProcessing()` 也不是消除死锁的工具；抑制处理期间如果等待依赖 Dispatcher 的工作，反而可能阻止它推进。嵌套消息处理与重入的细节见 [WPF 线程模型的技术细节](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model#technical-details-and-stumbling-points)。

## 修复手段的适用边界 {#remedies}

### ConfigureAwait(false) 只改变这一次等待

不依赖 UI 的库代码可以写：

```csharp
private async Task<int> GetLibraryValueAsync()
{
    await Task.Delay(100).ConfigureAwait(false);
    return 100;
}
```

它不要求后续代码返回捕获的同步上下文或调度器，因而可以移除某些对 UI 的依赖。但它不创建一个“默认同步上下文”，也不保证切到后台线程：任务如果已经完成，后续代码仍可以在原线程执行。

尤其注意：

- 只配置调用者的一次 `await`，不会改写被调用方法内部已经发生的上下文捕获。
- 只配置方法内的第一次 `await`，不保证其他等待永远不会捕获 UI。
- 不能用 `SomeAsync().ConfigureAwait(false).GetAwaiter().GetResult()` 来保证同步调用安全；这不会取消 `SomeAsync` 内部的捕获。
- 即使库内部不捕获 UI，UI 调用者自己的 `await` 仍然可以捕获 UI，然后安全地展示结果。
- 这不解决资源锁顺序、信号量递归或两个任务互相依赖。

因此，UI 代码通常保留自己的上下文，库代码按自身契约决定是否捕获；不要把 `ConfigureAwait(false)` 当成全项目统一的死锁开关。参见 [ConfigureAwait FAQ](https://devblogs.microsoft.com/dotnet/configureawait-faq/)。

### BeginInvoke 与执行顺序

把第一个反例的 `Invoke` 改成 `BeginInvoke`，后台不再等待 UI 委托完成，可以拆掉那个等待环。但 UI 上的 `.Wait()` 还在，界面仍然阻塞；并且任务完成时，只能保证更新已提交，不能保证标题已更新。

直接去掉 `.Wait()` 再丢弃 `Task`，则改变了事件处理器的完成边界，失败也可能无人观察。使用 `ContinueWith` 时，需要在 UI 上取得并显式传入 `TaskScheduler.FromCurrentSynchronizationContext()` 才能要求 UI 调度；还必须分别处理前置任务和续接任务的失败、取消。对于顺序工作，`await` 更容易保留清楚的完成和异常语义。

### Task.Run 与同步桥接

```csharp
int value = Task.Run(() => GetLibraryValueAsync()).GetAwaiter().GetResult();
```

这个重载会展开内部的异步任务；在方法完全不依赖 UI 时，它可以避开 UI 上下文捕获导致的特定等待环。但调用者如果是 UI，仍会阻塞界面；内部如果同步调用 UI，第一种死锁仍会回来。大量用线程池包住同步等待，还会引出线程池饥饿。

已有同步 API 的兼容需求应集中在明确边界处理。若确实需要复杂的同步与异步互操作，可以研究 `Microsoft.VisualStudio.Threading` 的 `JoinableTaskFactory`，但必须遵循它的任务加入和主线程切换协议，不能仅替换一次 `.Wait()` 就假定所有依赖都被跟踪。参见微软的 [JTF 线程规则](https://microsoft.github.io/vs-threading/docs/threading_rules.html)。

### Dispatcher 的异步委托可能还有内层任务

`InvokeAsync` 没有一个自动理解整个异步业务流程的通用语义。把 `Func<Task>` 传给泛型重载时，结果是 `DispatcherOperation<Task>`：外层表示委托调用完成，内层表示异步方法完成。

```csharp
private async Task RunOnUiAsync()
{
    var operation = Dispatcher.InvokeAsync(new Func<Task>(async () =>
    {
        await Task.Delay(100);
        Title = "Finished";
    }));

    await operation.Task.Unwrap(); // 同时等待内层异步工作并观察异常。
}
```

只等待外层可能过早报告完成。如果把异步 lambda 强制转换为 `Action`，会变成 `async void`，又失去了可等待的内层任务。API 的委托类型和完成边界必须一起检查，参见 [InvokeAsync 的泛型重载](https://learn.microsoft.com/en-us/dotnet/api/system.windows.threading.dispatcher.invokeasync)。

### 一张修复边界表

| 手段 | 能解决的依赖 | 仍需处理 |
| --- | --- | --- |
| UI 使用 `await` | UI 被同步等待占住 | 锁、异步资源和任务之间仍可能有环 |
| 释放锁后再调度 | 持锁等待 UI | 快照版本、提交顺序与生命周期 |
| `ConfigureAwait(false)` | 某次等待对原上下文的依赖 | 内部其他等待、线程权限、业务资源环 |
| `BeginInvoke` | 调用方同步等待 UI 委托 | 更新完成顺序、排队量和失败观察 |
| `Task.Run` | 把合适的同步计算移出 UI | UI 依赖、组件线程要求、线程池压力 |
| `WaitAsync`／超时／取消 | 给等待者退出机会 | 底层操作可能仍运行，资源不会自动释放 |
| 统一锁顺序／调整协议 | 资源获取环 | 所有入口和回调必须遵守同一协议 |

## 调试与现场诊断 {#diagnostics}

### 在 Visual Studio 中建立等待图

先暂停所有线程，再找拥有卡住窗口的线程，不要只根据线程编号猜 UI。查看该线程的调用栈，确认它在执行计算、取锁、等待任务，还是调用原生组件。

使用 Threads、Call Stack 和 Parallel Stacks 的 Threads／Tasks 视图对照分析。异步后续操作可能尚未分配到线程，因此不能只找“哪个后台线程被阻塞”；Tasks 视图也不能自动展示所有任意协议的死锁。参见 [Visual Studio 多线程调试](https://learn.microsoft.com/en-us/visualstudio/debugger/get-started-debugging-multithreaded-apps)。

| 栈或状态线索 | 下一步追查 |
| --- | --- |
| UI 停在 `Task.Wait`、`Result`、`GetResult` | 任务由谁完成，完成前是否需要 UI |
| 后台停在 `Dispatcher.Invoke` | 目标 Dispatcher 属于谁，UI 又在等什么 |
| UI 停在 `Monitor.Enter`／读写锁 | 谁持有资源，持有者有没有调用 UI |
| UI 响应，但操作一直 Loading | 信号量许可、未完成的 TCS、互相等待的任务 |
| Dispatcher 操作长期 Pending | 正确的 Dispatcher 吗，是否运行、关闭或被优先级压住 |
| 线程池排队增长，很多线程同步等待 | 是否线程池饥饿，而不只是 UI 等待环 |

日志中记录操作 ID、阶段、托管线程 ID、目标 Dispatcher、资源名和开始／结束时间。超时只能证明规定时间内没有完成；日志和等待图才有助于判断原因。避免在持业务锁时同步写入一个可能回调 UI 的日志目标。

### 没有现场调试器时

先保存进程转储和必要日志，再决定是否重启。对于现代 .NET，可以使用已安装的 `dotnet-dump`；把下面的 `1234` 换成目标进程 ID：

```powershell
dotnet-dump collect --process-id 1234 --output wpf-hang.dmp
dotnet-dump analyze wpf-hang.dmp
```

在分析会话中，常见命令包括：

```text
clrthreads
clrstack -all
syncblk
dumpasync
```

`syncblk` 主要帮助查看 Monitor 等同步块，不会列出所有信号量、异步任务和原生锁；`dumpasync` 的可用性和信息量取决于运行时与 SOS 版本。`dotnet-dump` 不显示完整原生调用栈，涉及 COM、Win32 或 .NET Framework 现场时，应选择匹配位数和运行时的 Visual Studio／WinDbg 与 SOS。参见 [dotnet-dump 文档](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump)。

可以间隔保存多个栈或转储，比较进度和资源状态。一次栈快照停在等待处，不足以证明永远不会完成；高 CPU 也可能来自循环或重试，低 CPU 也可能只是正常 I/O。

### 调度饥饿要单独处理

持续向 Dispatcher 投递大量高优先级工作，或者每条遥测数据都单独刷新控件，可能挤压输入、绘制和低优先级工作。提高优先级并不能普遍解决这个问题，应合并更新、限制频率、分批执行并对生产者施加背压。

线程池被大量 `.Wait()`、阻塞 I/O 或长期任务占据时，异步后续工作也可能迟迟得不到执行。运行时增加线程后可能恢复，这与一个固定的资源环不同。应减少同步等待并限制并发，不能仅靠提高最小线程数掩盖问题。指标和取证方法见 [线程池饥饿诊断](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/debug-threadpool-starvation)。

## 工程约定与验证 {#verification}

可以把预防要求落实到几项可审查的约定：

1. 普通异步方法返回可等待的任务；事件边界观察失败，禁止无意丢弃任务。
2. 明确每个 UI 对象、集合和后台服务的所有线程与生命周期；向后台组件显式提供必要的 UI 调度接口。
3. 不持有业务锁或异步许可，去等待会回调自身、UI 或其他持锁组件的工作。
4. 记录资源的全局获取顺序；允许重入与禁止重入的入口要有清楚的协议。
5. 异步期间界面仍能响应，因此另一次点击、导航和关闭可以发生；按业务选择禁止重复、串行化、取消旧请求或按版本提交。
6. 取消、超时、失败与关闭都有可到达的完成路径，且资源释放与实际取得资源相匹配。
7. UI 更新有频率和队列上限，后台生产速度不能无限压过 UI 消费速度。

验证不应只测“某次点击没有卡住”。应分别覆盖异步任务立即完成与延迟完成、连续点击、失败、取消、窗口提前关闭和后台最后一次通知。使用屏障或完成信号构造确定的依赖关系，不要仅靠两个 `Sleep` 碰运气。

普通控制台测试没有 WPF 的同步上下文，可能无法复现 UI 上下文死锁。需要在 STA 线程运行真实 Dispatcher，并设置独立于被测 Dispatcher 和线程池的监控；故意构造死锁的例子应放进隔离子进程，测试结束后回收该进程。

## 配套示例与参考资料 {#references}

[配套验证项目](https://github.com/luli100/luli100.github.io/tree/main/examples/wpf-deadlocks)包含可运行的复现与修复示例。项目用 UI 心跳区分“UI 被阻塞”和“仅异步操作无法完成”，同时检查执行顺序与完成状态。它验证代表性的等待机制；集合绑定、应用生命周期和原生互操作仍需结合实际控件与组件做集成验证。

在示例目录运行 `dotnet run` 可执行 16 个隔离场景；运行 `./VerifyArticle.ps1` 可编译正文的 14 段 C# 代码，并检查目录与代码围栏。正文与示例保留了反例，复制时需按各段说明选择修复版。

主要资料：

- [WPF 线程模型](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model)
- [ConfigureAwait FAQ](https://devblogs.microsoft.com/dotnet/configureawait-faq/)
- [异步编程场景](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios)
- [异步方法返回类型](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-return-types)
- [异步代码也会死锁](https://devblogs.microsoft.com/dotnet/are-deadlocks-still-possible-with-await/)
- [EnableCollectionSynchronization](https://learn.microsoft.com/en-us/dotnet/api/system.windows.data.bindingoperations.enablecollectionsynchronization)
- [TaskCompletionSource 的续接风险](https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/)
- [Visual Studio 多线程调试](https://learn.microsoft.com/en-us/visualstudio/debugger/get-started-debugging-multithreaded-apps)
- 初稿参考：[C# Deadlocks in Depth – Part 2](https://michaelscodingspot.com/c-deadlocks-in-depth-part-2/)
