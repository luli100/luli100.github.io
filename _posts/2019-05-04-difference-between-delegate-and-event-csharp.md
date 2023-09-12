---
layout: post
title: C# 中委托和事件的区别
enable: false
---

严格来讲，C# 中委托和事件的区别，这个问题本身就没有表达清楚。委托是一种引用类型，用于指向具有特定参数列表和返回类型的方法。

### delegate 和 event 关键字

可使用类似于定义方法签名的语法来定义委托类型。 只需向定义方法签名的前面添加 delegate 关键字即可。

```
// 定义委托类型，类型名为 Comparison 
public delegate Int32 Comparison<in T>(T left, T right);
```

当你使用 delegate 关键字时，编译器会生成一个 Comparison 类，该类继承自 MulticastDelegate 类。值得注意的是，MulticastDelegate 类是一个特殊的类。 编译器和其他工具可以从此类派生，但我们不能定义一个类显式派生自 MulticastDelegate 类。

### 事件的本质是委托类型字段的包装器

事件的完整声明方式：

```
// 定义委托类型，类型名为 Comparison 
public delegate Int32 Comparison<in T>(T left, T right);
```

事件的简略声明方式：

### 委托和事件作为后期绑定方案