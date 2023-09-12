---
layout: post
title: 深入理解 SynchronizationContext
enable: false
---

<!DOCTYPE html>
<html>
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=0.5">
        <link href="/styles/main.css" rel="stylesheet" type="text/css">
        <title></title>
    </head>
    <body>
        <div class="inner">
            <h2>深入理解 SynchronizationContext</h2>
            <p>在开发 Windows 窗体应用程序时，关于 UI 线程，大多数开发者都知道的两条黄金法则:</p>
            <ol>
                <li>不要在 UI 线程上执行任何耗时的操作。</li>
                <li>不要在非 UI 线程上访问任何 UI 控件。</li>
            </ol>
            <p>那么问题来了，如果我正在编写一个需要在线程池线程执行一部分工作，然后在 UI 线程上再进行一部分工作的组件，我们应该如何做呢？WPF 有一个 DispatcherSynchronizationContext 类，该类派生自<a href="https://github.com/dotnet/wpf/blob/ac9d1b7a6b0ee7c44fd2875a1174b820b3940619/src/Microsoft.DotNet.Wpf/src/WindowsBase/System/Windows/Threading/DispatcherSynchronizationContext.cs"> SynchronizationContext 类</a>，重写了 Post 方法，通过 Dispatcher.BeginInvoke 将接收的委托封送到 UI 线程。</p>
            <h3>SynchronizationContext 是什么</h3>
            <p><a href="https://docs.microsoft.com/zh-cn/dotnet/api/system.threading.synchronizationcontext?view=netcore-3.1">System.Threading.SynchronizationContext</a> 的文档是这样说的：“提供在各种同步模型中传播同步上下文的基本功能”，太抽象了。</p>  
            <p>在99.9%的使用场景中，SynchronizationContext 仅仅被当作一个提供虚（virtual）Post 方法的类，该方法可以接收一个委托，然后异步执行它。虽然 SynchronizationContext 还有许多其他的虚成员，但是很少使用它们。Post 方法的基础实现就仅仅是调用一下 ThreadPool.QueueUserWorkItem，将接收的委托加入线程池队列去异步执行。</p>
            <p>另外，派生类可以选择重写（override）Post 方法，让委托在更加合适的位置和时间去执行。接下来，我将尝试编写自己 SynchronizationContext 代码，以便将任何线程的代码委托到 STA 线程执行。</p>
            <h3>如何在两个线程之间切换</h3>
            <p>第一个问题是我们如何管理两个正在运行的线程之间的通信。两个线程之间的理想通信对象是队列。队列使我们能够根据调用顺序将工作从一个线程发送到另一个线程。这是典型的生产者/消费者模型，其中一个线程扮演消费者的角色（从队列中读取），另一个线程扮演生产者的角色（将项目写入队列）。</p>
            <p>那么，我们打算在这个队列中放入什么？考虑到这个队列负责将代码从一个线程编组到另一个线程，理想的队列项是委托。尽管如此，我们需要的不仅仅是一个委托，而且不仅仅是一个简单的委托，而是一个SendOrPostCallback 委托。</p>
            <h3>StaDispatcherOperation 类</h3>
            <p>接下来，我们就来实现需要放入队列里的类：</p>
            <div class="highlighter-rouge"><div class="highlight"><pre class="highlight"><code>internal class StaDispatcherOperation
{
    private object? mState;
    private ExecutionType mExeType;
    private SendOrPostCallback mMethod;
    private ManualResetEvent mAsyncWaitHandle = new ManualResetEvent(false);
    private Exception mException;

    internal StaDispatcherOperation(SendOrPostCallback callback, object? state, ExecutionType type)
    {
        mMethod = callback;
        mState = state;
        mExeType = type;
    }

    internal Exception Exception
    {
        get { return mException; }
    }

    internal bool ExecutedWithException
    {
        get { return mException != null; }
    }

    /// &lt;summary&gt;
    /// 必须在 STA 线程上执行
    /// &lt;/summary&gt;
    internal void Execute()
    {
        if (mExeType == ExecutionType.Send)
        {
            Send();
        }
        else
        {
            Post();
        }
    }

    /// &lt;summary&gt;
    /// 调用线程将被堵塞，直到设置异步等待为有信号
    /// &lt;/summary&gt;
    internal void Send()
    {
        try
        {
            mMethod(mState);
        }
        catch (Exception e)
        {
            mException = e;
        }
        finally
        {
            mAsyncWaitHandle.Set();
        }
    }

    /// &lt;summary&gt;
    /// 未处理异常将终止调度器线程
    /// &lt;/summary&gt;
    internal void Post()
    {
        mMethod(mState);
    }

    internal WaitHandle ExecutionCompleteWaitHandle
    {
        get { return mAsyncWaitHandle; }
    }
}

internal enum ExecutionType
{
    Send,
    Post,
}</code></pre></div></div>
            <ul>
                <li>StaDispatcherOperation 包含我们希望在 STA 线程上执行的委托。</li>
                <li>Send 和 Post 是真正的辅助方法，它们都负责启动代码，并且它们都被设计为从 STA 线程调用。因为 Send 方法需要阻塞调用者，所以我使用了 ManualResentEvent。如果有异常，则会在非 STA 线程（生产者线程）上抛出。</li>
                <li>Post 方法很简单。它只是调用方法，完成时不需要通知，也不需要跟踪异常。</li>
            </ul>
            <p>总的来说，这个类负责两个主要任务：存储要执行的委托和以两种可能的模式执行 Send 和 Post 方法。Send 方法需要额外的异常跟踪。Post 方法只执行该方法而不做任何其他事情。通常，如果 Post 在 STA 线程上执行，委托报告的任何异常都会导致线程结束。</p>
            <h3>StaDispatcher 类</h3>
            <p>现在我们有了一个队列，并且我们知道将什么放入该队列中，让我们看看 STA 线程需要做什么工作。</p>
            <div class="highlighter-rouge"><div class="highlight"><pre class="highlight"><code>internal class StaDispatcher
{
    private Thread mStaThread;
    private readonly ConcurrentQueue<StaDispatcherOperation> mQueueConsumer = new ConcurrentQueue<StaDispatcherOperation>();
    private ManualResetEvent mStopEvent = new ManualResetEvent(false);

    internal StaDispatcher()
    {
        mStaThread = new Thread(Run);
        mStaThread.Name = "STA Worker Thread";
        mStaThread.SetApartmentState(ApartmentState.STA);
    }

    internal void Enqueue(StaDispatcherOperation item)
    {
        mQueueConsumer.Enqueue(item);
    }

    internal void Start()
    {
        mStaThread.Start();
    }

    internal void Join()
    {
        mStaThread.Join();
    }

    private void Run()
    {
        while (true)
        {
            bool stop = mStopEvent.WaitOne(0);
            if (stop)
            {
                return;
            }
            if (!mQueueConsumer.IsEmpty)
            {
                StaDispatcherOperation workItem;
                if (mQueueConsumer.TryDequeue(out workItem))
                {
                    if (workItem != null)
                    {
                        workItem.Execute();
                    }
                }
            }
        }
    }

    internal void Stop()
    {
        mStopEvent.Set();
        mQueueConsumer.Clear();
        mStaThread.Join();
    }
}</code></pre></div></div>
            <h3>StaDispatcherSynchronizationContext 类</h3>
            <p>现在有一个并发队列来处理 STA 线程和任何其他线程之间的通信。唯一缺少的是实际的同步上下文类本身，接下来，我们就实现它：</p>
            <div class="highlighter-rouge"><div class="highlight"><pre class="highlight"><code>internal class StaDispatcherSynchronizationContext : SynchronizationContext, IDisposable
{
    private StaDispatcher mStaDispatcher;
    public StaDispatcherSynchronizationContext() : base()
    {
        mStaDispatcher = new StaDispatcher();
        mStaDispatcher.Start();
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        StaDispatcherOperation item = new StaDispatcherOperation(d, state,ExecutionType.Send);
        mStaDispatcher.Enqueue(item);
        item.ExecutionCompleteWaitHandle.WaitOne();

        // 如果回调有异常，应该在调用线程抛出，而不是在 STA 线程抛出
        if (item.ExecutedWithException)
        {
            throw item.Exception;
        }
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        StaDispatcherOperation item = new StaDispatcherOperation(d, state, ExecutionType.Post);
        mStaDispatcher.Enqueue(item);
    }

    public void Dispose()
    {
        mStaDispatcher.Stop();
    }

    public override SynchronizationContext CreateCopy()
    {
        return this;
    }
}</code></pre></div></div>
            <p>注意 Send 操作是一个阻塞操作，这意味着我们阻塞当前操作直到 STA 线程上的操作完成。请记住，我们在 StaDispatcherOperation 类上放置了一个事件 ManualResetEvent，我们知道操作何时完成。我们还在其中捕获和缓存任何异常，因此，我们可以将它们抛出到调用线程而不是 STA 线程上。Post 操作不是一个等待调用，所以我们所要做的只是将 StaDispatcherOperation 对象排队，而不是等待委托执行完成。</p>
            <h3>测试程序</h3>
            <p>为了实际测试这个类，我创建了一个测试程序，代码如下：</p>
            <div class="highlighter-rouge"><div class="highlight"><pre class="highlight"><code>public class Params
{
    public String Output { get; set; }
    public Int32 CallCounter { get; set; }
    public Int32 OriginalThread { get; set; }
}

class Program
{
    private static Int32 mCount = 0;
    private static StaDispatcherSynchronizationContext mStaSyncContext = null;
    static void Main(string[] args)
    {

        mStaSyncContext = new StaDispatcherSynchronizationContext();
        for (Int32 i = 0; i &lt; 100; i++)
        {
            ThreadPool.QueueUserWorkItem(NonStaDispatcher);
        }
        Console.WriteLine("Press any key to dispose SyncContext");
        Console.ReadLine();
        mStaSyncContext.Dispose();
    }

    private static void NonStaDispatcher(object state)
    {
        Int32 id = Thread.CurrentThread.ManagedThreadId;

        for (Int32 i = 0; i &lt; 10; i++)
        {
            var param = new Params { OriginalThread = id, CallCounter = i };
            mStaSyncContext.Send(RunOnStaDispatcher, param);
        }
    }

    private static void RunOnStaDispatcher(object state)
    {
        mCount++;
        int id = Thread.CurrentThread.ManagedThreadId;
        var args = (Params)state;
        Console.WriteLine($"mCount: {mCount}, STA id: {id}, " +
            $"original thread: {args.OriginalThread}, " +
            $"call count: {args.CallCounter}");
        args.Output = "Processed";
    }
}</code></pre></div></div>
        <h3>结论</h3>
        <p>SynchronizationContext 本身对线程之间的代码封送执行没有任何作用。其实这个类更应该是一个抽象类，我们可以在不使用 SynchronizationContext 的情况下做同样的事情。只是微软提供了一个统一的抽象，供诸如 WPF，WinForm，WinRT 这样的框架进行实现而已。</p>
        </div>
        
        <script>
            if (/mobile/i.test(navigator.userAgent) || /android/i.test(navigator.userAgent))
            {
                document.body.classList.add('mobile');
            }
        </script>
    </body>
</html>