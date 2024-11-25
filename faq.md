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

1. 安装 Microsoft.Xaml.Behaviors.Wpf 包
2. 在需要的窗体或控件处引入包, 格式： xmlns:i="http://schemas.microsoft.com/xaml/behaviors" 
3. 编写事件触发器命令调用，例如：
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

### 在 Visual Studio 下，如何格式化 XAML 代码

去 Tools -> Options -> Text Editor -> XAML -> Formating -> Spacing 进行如下设置：

<img src="/images/how-to-format-xaml-code.png" width="80%">

然后尝试按：Ctrl + K + D。

### 球坐标(γ,θ,φ) 转直角坐标(x,y,z)

<img src="/images/azimuth_elevation.png" width="80%">
<img src="/images/spherical_coordinate_system.png" width="80%">

[球面坐标到三维笛卡尔坐标的映射](https://www.mathworks.com/help/matlab/ref/sph2cart_zh_CN.html)

### Meaning of In Which

[Meaning of In Which](https://www.geeksforgeeks.org/difference-between-which-and-in-which-in-english-grammar/)