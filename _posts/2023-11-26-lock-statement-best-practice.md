---
layout: post
title: 使用 lock 语句的最佳实践
enable: true
---

在多线程 C# 应用程序中同步访问共享资源时，通常使用 lock 语句 -- 部分原因是 lock 语法简单。但是，C# 开发人员在使用 lock 语句时，很容易不考虑它的功能和需求，其实 lock 语句的基本用法也是有一些陷阱的。

### 锁对象是引用类型，而不是值类型

### 避免 lock 任何可公开访问的对象

你选择与 lock 语句一起使用的对象没有太多要做的事情，所以锁定已存在的对象可能会很诱人。

### 避免使用 lock(this){}

### 强烈推荐的锁对象

```C#
private static Object objLocker = new Object();
```

### 锁定开始前后检查状态

```C#
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

```C#
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

上面的代码中，lock 语句是多余的，因为 ConcurrentDictionary 有自己的代码来同步访问共享数据。事实上，System.Collections.Concurrent 命名空间中所有集合有一些机制来确保其同步访问共享数据。这样的集合被认为是“线程安全的”，因此你可以在多线程上下文中使用它们，而不必担心静态条件。