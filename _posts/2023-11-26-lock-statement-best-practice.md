---
layout: post
title: 使用 lock 语句的最佳实践
enable: true
---

在多线程 C# 应用程序中同步访问共享资源时，通常使用 lock 语句 -- 部分原因是 lock 语法简单。但是，C# 开发人员在使用 lock 语句时，很容易不考虑它的功能和需求，其实 lock 语句的基本用法也是有一些陷阱的。

### 锁对象是引用类型，而不是值类型

刚开始你可能会写出如下代码：

```c#
private static Int32 locker;
public void Write()
{
    lock(locker)
    {
        //your code
    }
}
```

幸运的是，C# 编译器会提醒你，"lock 语句需要一个引用类型，int 不是一个引用类型"。如果你知道 lock 语句是 try/finally 使用 Monitor.Enter 和 Monitor.Exit 封装语法糖的话，你可能会思考下面的代码为什么编译器不会报错：

```c#
private static Int32 locker;
public void Write()
{
    
    Boolean lockAcquired = false;
    try 
    {
        Monitor.Enter(locker, ref lockAcquired);
        // your code
    }
    finally
    {
        if (lockAcquired)
        {
            Monitor.Exit(locker);
        }
    }
}
```

上面的代码块，编译时不会报错，但是在运行时，调用 Monitor.Exit 会抛出一个 SynchronizationLockException 异常。为什么会这样？

其实，是因为 lock 是一个特殊的 C# 关键字，它允许编译器为你执行额外的检查，而 Monitor.Enter 和 Monitor.Exit 是正常的 .NET 方法，该方法接受任何类型对象的变量。C# 允许将值类型变量自动装箱为引用类型对象，从而允许我们可以将值类型传递给许多方法。但是，每次需要装箱时，自动装箱都会创建一个新对象，因此对于 Monitor 方法的每次调用，该对象都是不同的。所以，当 Monitor.Exit 试图找到 locker 装箱后的对象锁时，却找不到，从而报运行时错误。

为了让事情更简单，强烈推荐你使用如下代码创建锁对象。

```c#
private static Object objLocker = new Object();
```

### 避免 lock 任何可公开访问的对象

你是否想过为什么锁对象的访问权限总是优先设为 private？你是否认为与 lock 语句一起使用的锁对象没有做其它的事情，从而选择一个已存在的对象做为锁对象？嗯，这样的方案确实很诱人。但你必须要认真思考这个已存在的对象的访问权限是什么？如果是 public 级别的访问，那么你就要注意了。例如：

```c#
public class Container
{
    public static readonly Queue<Object> datas = new Queue<Object>();
    public void Produce(Object obj)
    {
        lock (datas)
        {
            // your code
        }
    }
}
```

上面的代码，刚开始可能没啥问题，并且有一个有点是，你不必为锁创建一个单独的对象。假如有另外一个开发人员，他完全不知道 Container 的实现，并且对同步访问共享资源的不熟悉。假如他们也选择 datas 作为锁对象，即使他们同步访问的共享资源与 datas 本身无关。你知道这会带来什么潜在的问题了吧！

同理，下面的代码也存在类似的问题：

```c#
public class Container
{
    public void Produce(Object obj)
    {
        lock (this)
        {
            // your code
        }
    }
}
```

因为使用者可能创建一个 Container 对象做为锁对象，然而他们并不知道 Container 内部的某个地方也使用这个对象作为锁对象。导致的结果就是，应用程序的不相关的代码块正在使用相同的对象进行锁定。这是因为实例是可公开访问的，至少声明者可以访问。所以你应该避免把 this 做为锁对象。此外，锁对象应该仅用于锁定用途，不要用来干其它任何事情。因此，再次强烈建议你使用如下代码创建锁对象：

```c#
private static Object objLocker = new Object();
```

### 锁定开始前后检查状态

```c#
private static Object objLocker = new Object();
private static Boolean initialized = false;

public static void Init()
{
    if (!initialized)
    {
        lock(objLocker)
        {
            if (!initialized)
            {
                // init code here
                initialized = true;
            }
        }
    }
}
```

第二个 if 语句的状态检查看起来是重复的，几乎就像是一个输入错误。但是，这绝对是必要的，因为线程不知道从遇到 lock 语句到最终获取锁的时间之间发生了什么。第一个 if 语句的状态检查是一个优化，真正权威的状态检查发生在锁内部，即第二个 if 语句的状态检查才是关键。

### 避免过度锁定

```c#
private static Object objLocker = new Object();
private static ConcurrentDictionary<Int32, String> datas = new ConcurrentDictionary<Int32, String>();

public static void RemoveAllData()
{
    lock(objLocker)
    {
        datas.Clear();
    }
}
```

上面的代码中，lock 语句是多余的，因为 ConcurrentDictionary 有自己的代码来同步访问共享数据。事实上，System.Collections.Concurrent 命名空间中所有集合有一些机制来确保其同步访问共享数据。这样的集合被认为是“线程安全的”，因此你可以在多线程上下文中使用它们，而不必担心竞态条件。


### 参考资料

[Best Practices When Using the Lock Statement](https://www.pluralsight.com/guides/lock-statement-best-practices)