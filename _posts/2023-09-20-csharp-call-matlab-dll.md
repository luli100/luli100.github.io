---
layout: post
title: 生成 .NET 程序集和 构建 .NET 应用程序
enable: true
---

此示例演示如何从 MATLAB 函数生成 .NET 程序集，并将生成的程序集集成到 .NET 应用程序中。

### 在 MATLAB 中创建函数

第一步：在 MATLAB 中，分别创建名为 init, add, sub, mul, div 的 MATLAB 脚本。

<img src="/images/matlab_deploy_tool.png" width="80%" />

第二步：在 Command Window 中输入 **deploytool** 并按 **Enter** 键进入 MATLAB Compiler 选项框。

第三步：选择 **Library Compiler** 进入创建 .NET 程序集界面。

### 使用 Library Compiler 创建 .NET 程序集

第四步：在工具条的 **Type** 部分中，单击 **.NET Assembly**。

第五步：在工具条的 **EXPORTED FUNCTIONS** 部分中，导入 init.m, add.m, sub.m, mul.m, div.m 函数。

第六步：输入 **Libray Name**, **Version**, **Namespace** 后，重新命名 **Class Name**，如下所示：

<img src="/images/matlab_library_compiler.png" width="80%" />

第七步：点击右上角的 **Package**，然后等待完成创建 .NET 程序集。

### 将创建的 .NET 程序集集成到 .NET 应用程序中

第八步：打开 **Microsoft Visual Studio** 并创建名为 MainApp 的 C# 控制台应用程序。

第九步：添加 **MCalculator** 和 **MWArray** 引用。如下所示：

<img src="/images/csharp_call_matlab_dll.png" width="80%" />

注意：**MCalculator** 为上面使用 **Library Compiler** 创建的 .NET 程序集；**MWArray** 可以去 MATLAB 安装目录（C:\LegacyApp\Matlab23a\R2023a\toolbox\dotnetbuilder\bin\win64\v4.0，这是我电脑中的目录，你的或许不一样）下获取。

第十步：调用 .NET 程序集，并运行应用程序。

### 总结

init 空函数主要是用来解决 C# 首次调用 MATLAB 函数存在延迟问题，具体操作：在 .NET 应用程序启动时，主动调用一次 init 函数。