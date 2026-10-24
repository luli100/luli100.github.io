# WPF 单实例应用示例

配套文章：[WPF 单实例应用：Mutex 仲裁与命名管道通信](https://luli100.github.io/how-to-create-wpf-single-instance-app/)。

需要 Windows 与 .NET 10 SDK，无第三方包依赖。示例的业务窗口只展示参数，不打开文件或执行收到的命令。

## 运行窗口

在本目录执行：

```powershell
dotnet build SingleInstanceDemo.csproj
./bin/Debug/net10.0-windows/SingleInstanceDemo.exe "首次请求.txt"
```

保留窗口，再从终端启动同一个程序：

```powershell
./bin/Debug/net10.0-windows/SingleInstanceDemo.exe "含 空格的文件.txt" --example
```

窗口应显示同一个主实例 PID 和第二次请求。可以先最小化、最大化后再最小化，观察恢复行为。系统可能拒绝前台激活，示例会尝试提示任务栏，不保证每次取得焦点。

应用身份固定为 `Example.SingleInstanceDemo.v1`，Mutex 限于同一用户、同一登录会话。示例按相同提升级别通信；管道两端的 `CurrentUserOnly` 在 Windows 上也检查提升级别。换用产品自己的稳定身份时，应明确不同版本是否共享同一个主实例。

## 文件

- `Program.cs`：STA 入口、启动仲裁、转发分支、UI 消费和退出收尾。
- `SingleInstance.cs`：用户／会话身份、Mutex 所有权、长度前缀协议、管道服务器和客户端。
- `NativeMethods.cs`：尽力授予前台权限、恢复窗口、激活失败后的任务栏提示。
- `App.xaml`：作为 `Page` 编译，不生成默认应用入口，也不使用 `StartupUri`。
- `MainWindow.xaml`：展示首次启动与后续请求的窗口。
- `Tests`：直接复用 `SingleInstance.cs` 的控制台验证程序。

## 验证

运行：

```powershell
dotnet run --project Tests/SingleInstanceTests.csproj
./VerifyArticle.ps1
```

16 项检查覆盖：并发进程仲裁、其他句柄存活时接管、异常退出接管、重复取得资格、稳定身份、参数与目录往返、并发请求、启动缓冲、发送前重试、确认丢失、队列满、畸形消息、空闲客户端、两个停止阶段和过大请求。

并发仲裁使用 6 个独立子进程和启动／释放信号，保证它们的竞争时间重叠。异常退出场景只在测试子进程中使用 `Environment.Exit`，不影响用户的其他进程。测试失败时回收自己启动的子进程，正常验证不显示窗口或调用前台激活 API。

`VerifyArticle.ps1` 检查文章关键代码与项目源文件一致、目录锚点和围栏完整，然后编译 WPF 项目。它不等同于完整 Jekyll 构建。

窗口恢复、任务栏提示、多用户、跨会话和混合提升权限由人工在相应环境验证；自动检查只覆盖列出的仲裁与通信行为。

## 交付与退出约定

`accepted` 只表示请求进入内存队列。客户端发送后未收到匹配的确认，返回 `Uncertain`，不会自动重发或创建第二个业务窗口。请求编号用来关联确认，没有跨进程重启的持久去重功能。

服务器使用 64 KiB 帧上限、32 条等待请求的队列、2 秒连接交换期限；入口尝试连接和重新仲裁的总期限约为 5 秒。返回 `NotSent` 的失败可以在期限内重试，明确拒绝和结果不确定都结束本次启动。

窗口关闭时，示例取消监听和未执行的 UI 调度，等待收尾完成后再由入口线程释放 Mutex。窗口关闭前已经入队但未处理的请求可能丢失，需要排空或持久化的产品应实现自己的关闭协议。
