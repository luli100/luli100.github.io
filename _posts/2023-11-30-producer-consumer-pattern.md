---
layout: post
title: 使用 Monitor 简单实现生产者-消费者模式
enable: true
---

如果仔细看了 Monitor 类的 API，除了用于同步访问共享数据的 Monitor.Enter，Monitor.Exit 方法外，还有 Monitor.Wait 和 Monitor.Pulse 方法，它们可以用于线程间通信。

### 生产者-消费者的简单实现

Monitor.Wait 和 Monitor.Pulse 方法最常见的用法是实现生产者-消费者模式，其中一个线程将工作项放在队列中，另一个线程将工作项取来处理。
消费者线程通常会将工作项从队列中移除，直到队列为空，然后等待。当生产者向队列中添加一个工作项后，它会发送一个信号告诉消费者有新的工作项了。下面是一个例子：

```C#
internal class Program
{
    private static Int32 PRODUCT_NUMBER = 10;
    private static ProductContainer container = new ProductContainer();
    static void Main(string[] args)
    {
        var tasks = new List<Task>();
        tasks.Add(Task.Run(() => { ProducerJob(); }));
        tasks.Add(Task.Run(() => { ConsumerJob(); }));

        Task.WaitAll(tasks.ToArray());

        Console.WriteLine("main method finished.");
        Console.ReadKey();
    }

    private static void ProducerJob()
    {
        Random random = new Random();
        for (int i = 0; i < PRODUCT_NUMBER; i++)
        {
            Console.WriteLine($"Producing {i}");
            container.Produce(i);
            Thread.Sleep(random.Next(1000));
        }
    }

    private static void ConsumerJob()
    {
        Random random = new Random();
        while (true)
        {
            Object o = container.Consume();
            Console.WriteLine($"\t\t\tConsuming {o}");
            Thread.Sleep(random.Next(1000));
        }
    }
}

public class ProductContainer
{
    private readonly Object queueLocker = new Object();
    private readonly Queue<Object> products = new Queue<Object>();

    public void Produce(Object obj)
    {
        Monitor.Enter(queueLocker);
        products.Enqueue(obj);
        Monitor.Pulse(queueLocker);
        Monitor.Exit(queueLocker);
    }

    public Object Consume()
    {
        Monitor.Enter(queueLocker);
        while (products.Count == 0)
        {
            Monitor.Wait(queueLocker);
        }
        var o = products.Dequeue();
        Monitor.Exit(queueLocker);
        return o;
    }
}
```

由于 lock 关键字是 Monitor.Enter 和 Monitor.Exit 方法的语法糖，因此上述代码可以简化为：

```C#
internal class Program
{
    private static Int32 PRODUCT_NUMBER = 10;
    private static ProductContainer container = new ProductContainer();
    static void Main(string[] args)
    {
        var tasks = new List<Task>();
        tasks.Add(Task.Run(() => { ProducerJob(); }));
        tasks.Add(Task.Run(() => { ConsumerJob(); }));

        Task.WaitAll(tasks.ToArray());

        Console.WriteLine("main method finished.");
        Console.ReadKey();
    }

    private static void ProducerJob()
    {
        Random random = new Random();
        for (int i = 0; i < PRODUCT_NUMBER; i++)
        {
            Console.WriteLine($"Producing {i}");
            container.Produce(i);
            Thread.Sleep(random.Next(1000));
        }
    }

    private static void ConsumerJob()
    {
        Random random = new Random();
        while (true)
        {
            Object o = container.Consume();
            Console.WriteLine($"\t\t\tConsuming {o}");
            Thread.Sleep(random.Next(1000));
        }
    }
}

public class ProductContainer
{
    private readonly Object queueLocker = new Object();
    private readonly Queue<Object> products = new Queue<Object>();

    public void Produce(Object obj)
    {
        lock (queueLocker)
        {
            products.Enqueue(obj);
            Monitor.Pulse(queueLocker);
        }
    }

    public Object Consume()
    {
        lock (queueLocker)
        {
            while (products.Count == 0)
            {
                Monitor.Wait(queueLocker);
            }
            return products.Dequeue();
        }
    }
}
```

在上述代码中，只有一个生产者和一个消费者，你完全可以添加多个生产者或多个消费者，并且可以很好的运行。

### Monitor.Wait, Monitor.Pulse 和 Monitor.PulseAll

当调用 Monitor.Wait 时，将释放 Monitor 锁，但在消费者线程真实运行之前需要重新获取 Monitor 锁。这意味着会再次堵塞消费者线程，直到生产者线程调用 Monitor.Pulse 或 Monitor.PulseAll 并且释放 Monitor 锁后，消费者线程才有可能真实运行。如果多个线程被唤醒，它们都会尝试获取 Monitor 锁，当然，一次只有有一个线程成功获取。

当调用 Monitor.Wait 后，消费者线程会被堵塞，直到生产者线程调用 Monitor.Pulse 或 Monitor.PulseAll。Monitor.Pulse 与 Monitor.PulseAll 的区别在于唤醒多少个线程：Monitor.Pulse 只唤醒一个等待的线程；Monitor.PulseAll 会唤醒所有等待的线程。但是，这并不意味着它们都会立即开始运行，只有获取到 Monitor 对象锁的那个线程才会真正运行。

**什么时候会用到 Monitor.PulseAll?** 如果只有一个线程在等待，或者任何线程都可以使用任何生成的对象，那么只需使用 Monitor.Pulse。如果有几个线程在等待，并且你知道只有一个线程能够真实运行，那么 Monitor.Pulse 会比 PulseAll 效率更高，而且你唤醒哪个线程并不重要，那么唤醒一堆线程就没有意义了。但是，有时候不同的线程在不同的条件下等待，并且所有线程都在同一个 Monitor 上等待。在这种情况下，您需要使用 Monitor.PulseAll 唤醒所有线程，唤醒所有线程的目的，是想让所有线程都有可能获得对象锁的机会。