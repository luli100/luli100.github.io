---
layout: post
title: FAQ
use_math: true
---

### MathJax 上下标同时出现

$w_{1}^{2}$ 的完整写法如下：

```
$w_{1}^{2}$
```
在这个例子中，_{1} 表示下标 1，^{2} 表示上标 2。注意，下标和上标的顺序不能颠倒，必须先写下标，再写上标。

### 如何清空 MATLAB 命令行窗口

```
>> clc
```

### 在 WPF XAML中，EventTrigger 如何 Binding 命令

1、安装 Microsoft.Xaml.Behaviors.Wpf 包
2、在需要的窗体或控件处引入包, 格式： xmlns:i="http://schemas.microsoft.com/xaml/behaviors" 
3、编写事件触发器命令调用，例如：
   ```
    <i:Interaction.Triggers>
        <i:EventTrigger EventName="Loaded">
            <i:InvokeCommandAction Command="{Binding VM.LoadedCommand}" 
                                   CommandParameter="{Binding RelativeSource={RelativeSource AncestorType=Window}}"/>
        </i:EventTrigger>
    </i:Interaction.Triggers>
   ```

### 如何计算雷达车的 Orientation

已知雷达车的 yawrate(单位：rad/s)，cycleTime(单位：s)，如何计算雷达车当前的 Orientation。

```
orientation += yawrate * cycleTime;
orientation -= 2 * Math.PI * Math.Floor((orientation + Math.PI) / (2* Math.PI));
```

### Visual Studio 中解决方案和项目的关系

在 Visual Studio 中，解决方案不是“答案”。解决方案仅仅是 Visual Studio 用来组织一个或多个相关项目的容器。打开某个解决方案时，Visual Studio 会自动加载该解决方案包含的所有项目。

### 灯火阑珊处指什么地方

灯火稀疏的地方
