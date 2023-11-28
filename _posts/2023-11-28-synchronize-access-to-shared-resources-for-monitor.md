---
layout: post
title: 使用 Monitor 同步访问共享资源
enable: true
---

为了理解同步访问共享资源，我们来看一个例子：2 个线程向 Dictionary 添加数据。

```C#
internal class Program
{
    private static Int32 PRODUCT_NUMBER = 10000;
    private static Dictionary<Int32, String> dics = new Dictionary<Int32, String>();

    static void Main(string[] args)
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 2; i++)
        {
            tasks.Add(Task.Run(() => { Produce(); }));
        }

        Task.WaitAll(tasks.ToArray());

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

上面的代码会报类似于下面的错误：

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



