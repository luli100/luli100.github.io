# 谁依赖谁：C# 示例

配套文章：[谁依赖谁：用换设备与做测试理解 DIP、IoC 和 DI](https://luli100.github.io/who-depends-on-whom/)。

安装 .NET 8 或更新的 SDK，在此目录运行：

```powershell
dotnet test
```

`Devices.cs` 包含接口、灯、风扇、开关和测试替身。`SwitchTests.cs` 验证开关的两个分支、空值校验，以及手工装配和容器装配。灯和风扇用控制台输出代替硬件操作。

测试验证控制代码，不验证真实设备。文章中的扫描流程和 C 语言片段是关系示意，未包含在这个项目中。
