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



