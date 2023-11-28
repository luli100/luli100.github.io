---
layout: post
title: Monitor 和 Lock 同步访问共享资源
enable: true
---

为了理解同步访问共享资源，我们来看一个例子：2 个线程向 Dictionary 添加数据。

```C#
internal class Program
{
    private static Int32 PRODUCT_NUMBER = 100;
    private static Dictionary<Int32, String> dics = new Dictionary<Int32, String>();
    static void Main(string[] args)
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 2; i++)
        {
            tasks.Add(Task.Run(() => { Produce(); }));
        }

        Task.WaitAll(tasks.ToArray());
        Console.WriteLine("main method finished.");
        Console.ReadKey();
    }

    private static void Produce()
    {
        Random random = new Random();
        for(int i = 0;i < PRODUCT_NUMBER; i++)
        {
            if (!dics.ContainsKey(i))
            {
                dics.Add(i, i.ToString());
                Thread.Sleep(random.Next(50));
            }
        }
    }
}
```

上面的代码出现了竞态条件，会出现类似于下面的错误：

```C#
System.ArgumentException
  HResult=0x80070057
  Message=An item with the same key has already been added. Key: 5
  Source=System.Private.CoreLib
  StackTrace:
   at System.ThrowHelper.ThrowAddingDuplicateWithKeyArgumentException[T](T key)
   at System.Collections.Generic.Dictionary`2.TryInsert(TKey key, TValue value, InsertionBehavior behavior)
   at System.Collections.Generic.Dictionary`2.Add(TKey key, TValue value)
   at MonitorSample.Program.Produce() in D:\continental\adas_am_toolchain\src\conti.adas.am.toolchain\MonitorSample\Program.cs:line 30
   at MonitorSample.Program.<>c.<Main>b__2_0() in D:\continental\adas_am_toolchain\src\conti.adas.am.toolchain\MonitorSample\Program.cs:line 15
   at System.Threading.Tasks.Task.InnerInvoke()
   at System.Threading.Tasks.Task.<>c.<.cctor>b__272_0(Object obj)
   at System.Threading.ExecutionContext.RunFromThreadPoolDispatchLoop(Thread threadPoolThread, ExecutionContext executionContext, ContextCallback callback, Object state)
```

为了防止竞态条件需要对来自多个线程的数据进行同步访问。

### 使用 Monitor 同步访问共享数据

Monitor 类可以通过调用 Monitor.Enter, Monitor.TryEnter, 以及 Monitor.Exit 方法来获取和释放对象锁，从而对代码区域进行同步访问。

```C#
internal class Program
{
    private static Int32 PRODUCT_NUMBER = 100;
    private static Dictionary<Int32, String> dics = new Dictionary<Int32, String>();
    private static Object objLocker = new Object();
    static void Main(string[] args)
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 2; i++)
        {
            tasks.Add(Task.Run(() => { Produce(); }));
        }

        Task.WaitAll(tasks.ToArray());
        Console.WriteLine("main method finished.");
        Console.ReadKey();
    }

    private static void Produce()
    {
        Random random = new Random();
        for(int i = 0;i < PRODUCT_NUMBER; i++)
        {
            if (!dics.ContainsKey(i))
            {
                Monitor.Enter(objLocker);
                if (!dics.ContainsKey(i))
                {
                    dics.Add(i, i.ToString());
                }
                Monitor.Exit(objLocker);
                Thread.Sleep(random.Next(50));
            }
        }
    }
}
```

### 使用 lock 关键字同步访问共享资源

使用 lock 关键字也可以达到同步访问共享资源的目的，它的语法也相当简单：

```C#
internal class Program
{
    private static Int32 PRODUCT_NUMBER = 100;
    private static Dictionary<Int32, String> dics = new Dictionary<Int32, String>();
    private static Object objLocker = new Object();
    static void Main(string[] args)
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 2; i++)
        {
            tasks.Add(Task.Run(() => { Produce(); }));
        }

        Task.WaitAll(tasks.ToArray());
        Console.WriteLine("main method finished.");
        Console.ReadKey();
    }

    private static void Produce()
    {
        Random random = new Random();
        for(int i = 0;i < PRODUCT_NUMBER; i++)
        {
            if (!dics.ContainsKey(i))
            {
                lock (objLocker)
                {
                    if (!dics.ContainsKey(i))
                    {
                        dics.Add(i, i.ToString());
                    }
                }

                Thread.Sleep(random.Next(50));
            }
        }
    }
}
```

事实上，lock 语句的存在只是为了方便，就像 C# 的 using 语句一样。

```C#
lock(objLocker)
{
    // your code
}
```

会被 C# 编译器翻译成等效于下面的代码块

```C#
Boolean lockAcquired = false;
try 
{
    Monitor.Enter(objLocker, ref lockAcquired);
    // your code
}
finally
{
    if (lockAcquired)
    {
        Monitor.Exit(objLocker);
    }
}
```

### 结论

如果仅仅是为了对代码块的同步访问，直接使用 lock 关键字即可，无需自己再去封装 Monitor.Enter 和 Monitor.Exit 方法。

由于 lock 语句是 Monitor 类中 Enter/Exit 方法的语法糖，因此 lock 还可以与 Monitor 类中的 Monitor.Wait 和 Monitor.Pulse 方法一起使用，以实现更高级的线程协调或通信。


