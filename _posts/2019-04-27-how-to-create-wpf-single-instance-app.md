---
layout: post
title: WPF 单实例应用：Mutex 仲裁与命名管道通信
enable: true
toc:
  - title: 单实例需要解决三个问题
    anchor: responsibilities
  - title: 明确实例的作用域
    anchor: scope
  - title: 用 Mutex 仲裁主实例
    anchor: ownership
  - title: 在创建窗口之前决定去向
    anchor: startup
  - title: 用命名管道转发请求
    anchor: activation
  - title: 主实例恢复与激活窗口
    anchor: window
  - title: 退出、异常与交付边界
    anchor: lifecycle
  - title: 验证与方案选择
    anchor: verification
  - title: 示例与参考资料
    anchor: references
---

WPF 单实例应用通常需要这样的行为：第一次启动创建主窗口，之后再启动时，把新请求交给已经运行的实例。例如，再次双击程序应恢复原窗口；双击一个关联文件，则应让原实例打开这个文件。

要可靠地完成这件事，需要分别处理主实例仲裁、进程间通信和窗口激活。下面采用命名 `Mutex` 与命名管道实现，并说明启动、退出和交付失败时的边界。

[完整示例项目](https://github.com/luli100/luli100.github.io/tree/main/examples/wpf-single-instance)包含 WPF 窗口、入口、通信代码和验证程序，无第三方包依赖，使用 Windows 与 .NET 10。正文展示关键实现；现代 .NET 的异步 I/O、通道和序列化 API 不能原样复制到旧版 .NET Framework。

## 单实例需要解决三个问题 {#responsibilities}

| 职责 | 解决什么 | 本文的实现 |
| --- | --- | --- |
| 主实例仲裁 | 并发启动时，谁可以创建业务窗口 | 持有命名 Mutex 的进程成为主实例 |
| 请求转发 | 新进程如何交出参数，并确认对方已接收 | 本机命名管道与请求／确认协议 |
| 窗口展示 | 由谁恢复、显示和尝试激活窗口 | 主实例在自己的 UI 线程操作窗口 |

单实例不意味着系统中永远只能出现一个同名进程。后续启动的进程需要短暂运行，完成请求转发后再退出；需要限制的是主实例资格和业务窗口。

也不能用 `Process.GetProcessesByName` 代替仲裁：它只得到一个进程快照，不能原子地决定谁先启动；同名程序可能位于不同目录，同一程序也可能改名。即使找到进程，`MainWindowHandle` 在窗口尚未创建或隐藏等情况下仍可能为零，见 [Process.MainWindowHandle 文档](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.mainwindowhandle)。

命名事件可以传递简单的“请唤醒”信号，但不携带参数、请求编号和接收结果。自动重置事件也不是计数队列：多次 `Set` 可能合并；在 `WaitOne` 之前随意 `Reset`，还可能清掉已到达的通知。本文使用有明确消息边界的通信协议，参见 [AutoResetEvent 语义](https://learn.microsoft.com/en-us/dotnet/api/system.threading.autoresetevent)。

## 明确实例的作用域 {#scope}

先确定“只能运行一个”指什么。普通桌面程序往往选择同一用户、同一登录会话只有一个主实例，这也是本文的范围。

- Mutex 使用 `Local\` 命名空间，限制在当前会话。
- 名称包含用户 SID 的稳定摘要，区分同一会话中使用不同用户身份运行的程序。
- 命名管道的名称另外包含会话 ID，因为管道名称本身不使用 Mutex 的 `Local\` 命名空间规则。
- 服务器和客户端都使用 `PipeOptions.CurrentUserOnly`，限制同用户通信。

示例根据固定应用标识生成名称：

```csharp
internal sealed record InstanceIdentity(string MutexName, string PipeName)
{
    public static InstanceIdentity ForCurrentUser(string applicationId)
    {
        using var identity = WindowsIdentity.GetCurrent();
        string sid = identity.User?.Value ?? throw new InvalidOperationException("无法取得当前用户 SID。");
        string userKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sid)))[..24];
        using var process = Process.GetCurrentProcess();
        return new($@"Local\{applicationId}.{userKey}",
            $"{applicationId}.{userKey}.{process.SessionId}");
    }
}
```

应用标识必须稳定，并与其他产品区分，不能在每次启动时随机生成。示例中的 `v1` 是固定身份的一部分；真实产品应明确不同版本是否共用一个主实例，避免仅仅升级版本号就意外改变单实例范围。

`Local\` 是会话范围，`Global\` 是跨会话范围；它们本身不等于“当前用户”和“所有用户”。用户身份摘要用于区分名称，不代替访问控制。命名空间规则见 [Kernel object namespaces](https://learn.microsoft.com/en-us/windows/win32/termserv/kernel-object-namespaces)。

本文按相同权限级别运行两个实例。在 Windows 上，`CurrentUserOnly` 还检查提升级别；管理员与普通权限进程之间不能假定可以直接转发。需要跨权限协作的产品，应专门设计访问控制和受控代理，不能在连接被拒绝时绕过仲裁另开窗口。见 [PipeOptions 文档](https://learn.microsoft.com/en-us/dotnet/api/system.io.pipes.pipeoptions)。

## 用 Mutex 仲裁主实例 {#ownership}

关键是取得 Mutex 的所有权，并在整个主实例生命周期中保持持有。

```csharp
internal sealed class InstanceLease : IDisposable
{
    private readonly Mutex _mutex;
    private bool _ownsMutex;
    private bool _disposed;
    private int _ownerThread;

    public InstanceLease(string name) => _mutex = new Mutex(false, name);

    public bool TryAcquire()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_ownsMutex)
        {
            VerifyOwnerThread();
            return true; // 不重复 WaitOne，避免增加递归持有计数。
        }
        try { _ownsMutex = _mutex.WaitOne(0); }
        catch (AbandonedMutexException) { _ownsMutex = true; }
        if (_ownsMutex) _ownerThread = Environment.CurrentManagedThreadId;
        return _ownsMutex;
    }

    private void VerifyOwnerThread()
    {
        if (_ownerThread != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException("必须在取得 Mutex 的线程释放它。");
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_ownsMutex)
        {
            VerifyOwnerThread();
            _mutex.ReleaseMutex();
            _ownsMutex = false;
        }
        _mutex.Dispose();
        _disposed = true;
    }
}
```

这里有几处容易误解：

1. `new Mutex(false, name)` 只创建或打开对象，没有取得所有权。
2. `WaitOne(0)` 原子地尝试取得所有权；失败立即返回，不会把启动线程无限卡住。
3. Mutex 已存在，也可能处于无人持有的状态。因此，是否拥有所有权与“是不是刚创建了对象”不是同一个问题。
4. `AbandonedMutexException` 表示原拥有线程退出时没有释放，而当前等待者已经取得了该 Mutex。这里可以接管主实例资格；文件、数据库等业务数据是否一致，仍要另行恢复。
5. Mutex 所有权属于线程。取得与释放发生在同一个入口线程，不能取得后跨 `await` 换线程，再任意 `ReleaseMutex`。
6. 已持有时不要反复 `WaitOne`；Mutex 支持递归获取，每次成功获取都需要对应释放。

`Dispose` 也不能代替拥有者应执行的 `ReleaseMutex`。这些语义见 [Mutex 文档](https://learn.microsoft.com/en-us/dotnet/api/system.threading.mutex)和 [AbandonedMutexException 文档](https://learn.microsoft.com/en-us/dotnet/api/system.threading.abandonedmutexexception)。

## 在创建窗口之前决定去向 {#startup}

示例使用自定义入口，在创建 `Application` 和业务窗口之前完成仲裁。WPF 入口保持 `[STAThread]`，也不把整个 `Main` 改成异步方法。

### 配置入口

在现有 WPF 项目中加入 `Program.cs`，指定 `StartupObject`，把 `App.xaml` 的生成方式从 `ApplicationDefinition` 改为 `Page`，由自定义入口初始化它。示例项目的关键配置是：

```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <TargetFramework>net10.0-windows</TargetFramework>
  <UseWPF>true</UseWPF>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
  <StartupObject>SingleInstanceDemo.Program</StartupObject>
</PropertyGroup>
<ItemGroup>
  <ApplicationDefinition Remove="App.xaml" />
  <Page Include="App.xaml" />
</ItemGroup>
```

`App.xaml` 不再设置 `StartupUri`，主窗口由入口显式创建：

```xml
<Application x:Class="SingleInstanceDemo.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             ShutdownMode="OnMainWindowClose">
    <Application.Resources />
</Application>
```

### 主实例创建窗口，后续实例转发请求

下面是 `Program` 的入口方法。`RunPrimary` 和 `ReportFailure` 的完整实现位于示例项目中；前者运行主实例，后者显示启动失败信息并返回非零退出码。

```csharp
[STAThread]
public static int Main(string[] args)
{
    try
    {
        var identity = InstanceIdentity.ForCurrentUser("Example.SingleInstanceDemo.v1");
        var request = ActivationRequest.Create(args);
        using var lease = new InstanceLease(identity.MutexName);
        var deadline = Stopwatch.StartNew();
        while (true)
        {
            if (lease.TryAcquire()) return RunPrimary(identity, request);

            TimeSpan remaining = TimeSpan.FromSeconds(5) - deadline.Elapsed;
            if (remaining <= TimeSpan.Zero)
                return ReportFailure("已有实例，但尚未建立连接。请稍后重试，程序不会另开窗口。");
            var result = ActivationClient.TryForwardAsync(identity.PipeName, request,
                remaining < TimeSpan.FromMilliseconds(500) ? remaining : TimeSpan.FromMilliseconds(500),
                NativeMethods.AllowForeground).GetAwaiter().GetResult();
            if (result.Status == DeliveryStatus.Accepted) return 0;
            if (result.Status != DeliveryStatus.NotSent) return ReportFailure(result.Message);
            // 尚未发送请求，可以重试连接；下一轮仍由入口线程重新尝试仲裁。
        }
    }
    catch (Exception ex) { return ReportFailure(ex.Message); }
}
```

只有 `TryAcquire()` 成功才能进入主实例分支。主实例退出或崩溃，而新进程尚未发送请求时，新进程可以重新尝试取得资格。

请求还没有发送时，连接失败可以重试；开始发送以后，如果没有收到确认，就无法简单断言对方没有处理。因此不能对所有异常都自动重发，也不能因为转发失败就创建第二个业务窗口。

这里有同步等待，但它发生在 WPF `Application` 创建和消息循环启动之前；客户端的通信代码不依赖 UI 上下文，内部异步等待使用 `ConfigureAwait(false)`。这不构成“可以在按钮事件里同步等待任意异步方法”的理由。主窗口运行后的 UI 工作仍异步调度，相关等待环见[《WPF 中的死锁》](/csharp-deadlock-in-wpf/)。

## 用命名管道转发请求 {#activation}

### 请求保留参数的结构

不要先用空格拼接命令行，再要求主实例猜测如何拆分。示例直接传递参数数组，并携带发送者的工作目录：

```csharp
internal sealed record ActivationRequest(int Version, Guid RequestId, string[] Arguments, string WorkingDirectory)
{
    public static ActivationRequest Create(string[] arguments) =>
        new(1, Guid.NewGuid(), arguments, Environment.CurrentDirectory);
}
```

相对文件名需要根据发送者的工作目录解释，不能默认相对于主实例的当前目录。实际产品还需要按自己的命令行语法识别文件参数、验证路径和处理打开失败。示例窗口只展示请求，不执行文件操作或命令。

### 先监听，再初始化 UI

主实例取得 Mutex 后，先建立管道和一个有界请求队列，再初始化 WPF 窗口。完整的主实例入口如下：

```csharp
private static int RunPrimary(InstanceIdentity identity, ActivationRequest initialRequest)
{
    var server = new ActivationServer(identity.PipeName);
    Task pump = Task.CompletedTask;
    try
    {
        var app = new App();
        app.InitializeComponent();
        var window = new MainWindow();
        window.ShowRequest(initialRequest, forwarded: false);
        pump = DispatchRequestsAsync(server, app, window);
        return app.Run(window);
    }
    finally
    {
        // 此时 Dispatcher 已退出，停止网络等待并取消尚未执行的 UI 调度。
        try { server.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
        finally { pump.GetAwaiter().GetResult(); }
        // 返回 Main 后，才在原入口线程释放 Mutex。
    }
}
```

这是为了覆盖“主实例已经取得资格，但主窗口尚未准备好”的启动阶段：请求先进入队列，窗口初始化后再由消费者处理。管道构造失败时，主实例明确报错，不继续展示一个失去通信能力的业务窗口。

示例的服务端使用以下选项：

```csharp
PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly | PipeOptions.FirstPipeInstance
```

`FirstPipeInstance` 要求名称没有被其他管道服务端占用；`Asynchronous` 使等待连接和读写可以异步进行并观察取消。管道服务器在生命周期内复用同一个端点，连接结束后断开该连接，再接受下一个客户端。

`WaitForConnectionAsync` 的取消要求管道以异步模式创建，参见 [等待连接的文档](https://learn.microsoft.com/en-us/dotnet/api/system.io.pipes.namedpipeserverstream.waitforconnectionasync)。

### 消息边界、队列和超时

管道使用字节模式，读一次不代表刚好读完一条消息。示例协议使用 4 字节小端长度前缀，加 UTF-8 JSON 消息体，并用 `ReadExactlyAsync` 读取指定长度。

服务端在分配消息缓冲区之前检查长度，单条消息最多 64 KiB；还验证协议版本、请求编号、参数和工作目录。队列最多保留 32 条等待请求，满时明确回复 `busy`，不会一边丢弃请求一边返回成功。

服务器为一次连接设置 2 秒交换期限，客户端整个启动协商最多尝试约 5 秒。这些数字是示例参数，需要按产品启动耗时调整。慢客户端不能永久占住唯一的连接处理位置。读写和监听的完整代码位于 [SingleInstance.cs](https://github.com/luli100/luli100.github.io/blob/main/examples/wpf-single-instance/SingleInstance.cs)。

### “已接收”与“已完成”分开

一条正常连接的顺序是：

```text
主实例 → Hello：协议版本、进程 ID
新进程 → 尽力授权主实例取得前台权限
新进程 → Request：请求编号、参数、工作目录
主实例 → 将有效请求放入有界队列
主实例 → Reply：相同请求编号、accepted 或拒绝原因
新进程 → 读到确认后关闭连接并退出
主实例 → UI 消费者按顺序处理已入队的请求
```

主实例可能在确认写出之前就已经开始处理请求，UI 处理不依赖客户端继续存活。`accepted` 只表示有效请求进入了内存队列，不表示窗口已经激活，也不表示文件已经打开。

客户端区分四种结果：

| 结果 | 已知事实 | 启动端的行为 |
| --- | --- | --- |
| `Accepted` | 收到匹配请求编号的接收确认 | 正常退出，返回 0 |
| `NotSent` | 请求尚未开始发送 | 在总期限内重新连接或重新尝试仲裁 |
| `Rejected` | 本地请求不合法，或主实例明确拒绝 | 显示原因并退出 |
| `Uncertain` | 发送已开始，但结果不能确认 | 报告结果不确定，不自动重发或新开窗口 |

请求编号用于关联确认，不等于自动拥有去重能力。若业务需要可靠重试、去重或崩溃后继续处理，还需要明确的幂等规则、持久化和结果查询。单靠一个随机编号与内存队列，不能承诺“恰好执行一次”。

## 主实例恢复与激活窗口 {#window}

### UI 操作由窗口所属的 Dispatcher 执行

管道监听和 UI 消费者分开。监听器只负责协议和入队，不同步等待 UI；消费者取得请求后，再异步调度到窗口线程：

```csharp
await foreach (var request in server.Requests.ReadAllAsync(token).ConfigureAwait(false))
{
    await window.Dispatcher.InvokeAsync(() =>
    {
        window.ShowRequest(request, forwarded: true);
        NativeMethods.RestoreAndActivate(window);
    }, DispatcherPriority.Normal, token).Task.ConfigureAwait(false);
}
```

这段来自示例的 `DispatchRequestsAsync` 方法；完整方法还处理取消、监听故障和应用退出。UI 消费者按到达队列的顺序调用处理逻辑；如果实际业务又启动并发的异步打开操作，完成顺序仍需要另外定义。

### 恢复原窗口，不强制最大化

```csharp
if (!window.IsVisible) window.Show();
IntPtr handle = new WindowInteropHelper(window).EnsureHandle();
if (window.WindowState == WindowState.Minimized)
    ShowWindow(handle, 9); // SW_RESTORE
```

这些操作在 UI 线程执行。`SW_RESTORE` 用于恢复最小化窗口，保留系统记录的恢复状态；已经正常显示或最大化的窗口不强制改变状态。完整方法随后调用 `window.Activate()`，失败时用 `FlashWindowEx` 提示任务栏。

不要无条件设置 `WindowState.Maximized`，否则每次重复启动都会改变用户的窗口布局。也不需要靠枚举其他进程来猜窗口句柄，因为操作发生在主实例自己拥有的窗口上。恢复标志见 [ShowWindow 文档](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindow)。

### 前台激活是尽力行为

新进程从管道握手取得主实例 PID，然后调用 `AllowSetForegroundWindow` 尝试授权。调用者自己必须满足前台权限条件，授权也可能随后失效。因此它不是绕过 Windows 焦点规则的保证。

主实例恢复窗口后尝试激活；系统拒绝时提示任务栏即可，不反复抢焦点，也不调用官方标为不适合一般用途的 `SwitchToThisWindow`。相关契约见 [AllowSetForegroundWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-allowsetforegroundwindow)、[SetForegroundWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow)和 [FlashWindowEx](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-flashwindowex)。

## 退出、异常与交付边界 {#lifecycle}

主实例退出时，顺序同样重要：

1. 窗口和 Dispatcher 结束运行。
2. 请求停止管道监听，取消等待连接和正在进行的 I/O。
3. 取消消费者尚未执行的 UI 调度，观察监听与消费者任务的结果。
4. 在原入口线程释放并销毁 Mutex。

示例把这些操作放在 `RunPrimary` 和入口的 `finally`／`using` 中。服务端在停止期间不再依赖 UI 执行清理；客户端和监听器的等待也都有取消路径，因此可以在消息循环退出后收尾。

不能在 UI 线程上等待一个仍必须回调 UI 才能退出的监听任务；也不能先释放 Mutex，再慢慢关闭旧管道，否则新主实例可能已经开始接管，而旧主实例还在提供服务。

重复启动并成功转发属于正常情况，直接返回 0。通信失败则返回非零退出码并显示原因，不用 `Environment.Exit` 截断 WPF 的正常清理流程。

这里采用的是轻量桌面激活协议：主实例崩溃时，内存中的请求会丢失；窗口关闭前刚接收的请求，也可能尚未处理。需要严格“处理完才允许关闭”的程序，应在关闭前停止接收新请求，再异步排空队列和业务操作，最后结束 Dispatcher。若要求崩溃恢复，则还需要持久化请求，不能仅延长超时。

主实例异常退出后，Mutex 可以被后续进程重新取得；业务状态恢复仍是独立责任。进程退出不是撤销已完成文件写入、网络请求或其他外部操作的事务机制。

## 验证与方案选择 {#verification}

### 先测并发与失败路径

示例的验证程序直接复用通信和仲裁代码，检查以下行为：

- 6 个子进程同时争用时，重叠期间只有一个拥有者。
- Mutex 仍有其他句柄存活时，原拥有者释放后可以接管。
- 原拥有者未清理直接退出后可以接管；重复查询所有权不会漏掉递归计数。
- 中文、空格、空参数和工作目录能完整转发；并发请求不会相互覆盖。
- UI 消费者尚未启动时，请求可以先入队。
- 未发送时可重试；确认丢失时返回结果不确定。
- 队列满、无效消息长度和过大的请求得到明确处理。
- 慢客户端不会永久占用监听器；等待连接或读取时可以停止。

运行检查：

```powershell
dotnet run --project examples/wpf-single-instance/Tests/SingleInstanceTests.csproj
```

再手动验证界面行为：正常窗口、最小化、最大化后最小化、隐藏到托盘的产品逻辑，以及主窗口关闭期间再次启动。前台激活可能被系统拒绝，不能把“每次都抢到焦点”当作测试成功条件。

验证项目不自动操作用户当前的前台窗口；多用户、远程桌面会话和混合提升权限也需要在对应环境进行集成验证。

### 什么时候换一种方案

| 需求或环境 | 选择方向 |
| --- | --- |
| 只禁止重复运行，不需要唤醒或转发参数 | 保留作用域明确的 Mutex 仲裁与退出流程即可 |
| 传统 WPF，需要传递命令行参数并控制依赖 | 本文的 Mutex 加命名管道方案 |
| 已采用 Windows App SDK | 评估 `AppInstance` 注册与激活重定向 |
| 需要跨用户、跨权限或严格可靠交付 | 明确代理、权限和持久化协议，扩大设计范围 |

Windows App SDK 的 `AppInstance` 已提供实例注册和激活重定向能力，官方也给出 WPF 接入说明。采用它仍要处理初始化时机、STA 等待、UI 调度和业务请求的生命周期，不应直接把 WinUI 入口代码原样复制进 WPF。参见[应用生命周期实例管理](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/applifecycle/applifecycle-instancing)。

## 示例与参考资料 {#references}

[配套项目与运行说明](https://github.com/luli100/luli100.github.io/tree/main/examples/wpf-single-instance)包含全部源文件。示例窗口会展示首次启动和后续转发的请求，便于观察 PID、参数和工作目录；接入真实业务时，替换 `ShowRequest`，保留仲裁、协议与 UI 线程边界。

主要资料：

- [Mutex](https://learn.microsoft.com/en-us/dotnet/api/system.threading.mutex)
- [AbandonedMutexException](https://learn.microsoft.com/en-us/dotnet/api/system.threading.abandonedmutexexception)
- [命名对象的作用域](https://learn.microsoft.com/en-us/windows/win32/termserv/kernel-object-namespaces)
- [命名管道](https://learn.microsoft.com/en-us/windows/win32/ipc/named-pipes)
- [PipeOptions](https://learn.microsoft.com/en-us/dotnet/api/system.io.pipes.pipeoptions)
- [ShowWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindow)
- [AllowSetForegroundWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-allowsetforegroundwindow)
- [Windows App SDK 实例管理](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/applifecycle/applifecycle-instancing)
