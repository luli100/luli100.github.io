# WPF 死锁：复现与验证

配套文章：[WPF 中的死锁](https://luli100.github.io/csharp-deadlock-in-wpf/)。

需要 Windows 和 .NET 10 SDK，无第三方包依赖。在本目录运行：

```powershell
dotnet run
```

程序创建真实的 WPF Dispatcher，但不显示窗口。每个场景在独立子进程中运行；独立监控线程检查操作完成状态和 Dispatcher 心跳，然后结束该子进程。父进程只回收自己启动的子进程，并汇总结果。成功返回退出码 0，失败返回非零退出码。

`UiBlocked` 表示受控等待建立后 UI 心跳停止；`OperationBlocked` 表示业务操作无法完成，但 UI 心跳仍在运行。后者也包含无人运行消息循环的队列挂起，用于和真正的循环等待区分。用来同步参与者的信号建立等待关系，监控中的短暂延时只用于采样，不用来决定锁获取顺序。

| 场景 | 验证内容 |
| --- | --- |
| `invoke-wait` | UI 同步等待后台，后台同步调用 UI |
| `result-context` | `.Result` 阻塞恢复异步方法所需的 UI 上下文 |
| `dispatcher-task-wait` | UI 同步等待自己队列里尚未执行的任务 |
| `lock-invoke` | 后台持锁等待 UI，UI 委托需要同一把锁 |
| `semaphore-recursion` | 异步嵌套获取同一个许可，UI 仍有心跳 |
| `async-task-cycle` | 两个任务互相异步等待 |
| `two-dispatchers` | 两个 STA Dispatcher 同步回调彼此 |
| `unserviced-dispatcher` | 向没有消息循环的工作线程 Dispatcher 排队 |
| `await-fix` | UI 异步等待期间继续处理心跳，之后恢复 UI 上下文 |
| `snapshot-fix` | 取得快照、释放锁后再调度 UI |
| `semaphore-fix` | 只获取一次许可，取消获取时不额外释放 |
| `begininvoke-order` | UI 同步等待结束后的代码先于已排队的更新执行 |
| `configureawait-completed` | 已完成任务上的 `ConfigureAwait(false)` 不强制换线程 |
| `configureawait-caller` | 库内部不捕获 UI，不影响 UI 调用者自己的捕获 |
| `dispatcher-inner-task` | 外层 Dispatcher 操作先完成，`Unwrap` 等待内层任务 |
| `timeout-keeps-operation` | 超时不自动取消或完成原任务 |

也可以运行一个场景：

```powershell
dotnet run -- --case semaphore-recursion
```

它同样由监控线程结束，不会把终端无限留在死锁中。不要去掉隔离和监控后直接把反例集成进日常应用。

检查文章中的全部 C# 片段是否可编译，并核对目录锚点和代码围栏：

```powershell
./VerifyArticle.ps1
```

该脚本直接读取文章的 14 段 C# 代码，只为成员片段补上窗口类、命名空间和文中沿用的依赖。编译项目写入系统临时目录，位置会在输出中显示，不修改文章或把生成代码加入仓库。代码语法同时按 C# 7.3 检查；这不代表所有使用的框架 API 都适用于任意旧版 .NET Framework。

这些检查验证具体的等待机制和示例代码，不代替真实应用的集合绑定、窗口关闭、第三方控件与 COM/Win32 集成测试。UI 心跳也只验证 Dispatcher 是否推进，不能证明整个界面的性能和正确性。
