---
layout: post
title: 线程安全的 SafeObservableCollection&lt;T&gt;
enable: true
---

如果你使用过 WPF 框架开发应用程序，我想你一定使用过 ObservableCollection&lt;T&gt;。但 ObservableCollection&lt;T&gt; 的问题在于：它仅能在 Dispacher 线程「这里可以理解为 UI 线程」更新数据。如果你想从另外一个线程更新数据到 ObservableCollection&lt;T&gt; 中时，你需要编写类似于下面的代码：

```c#
this.Dispatcher.BeginInvoke(new Action(() =>
{
    // 操作 ObservableCollection<T> 对象逻辑
}));
```

为了不让代码到处充斥着 Dispatcher.BeginInvoke，可以封装了一个 SafeObservableCollection&lt;T&gt;：

```c#
[DebuggerDisplay("Count = {Count}")]
[ComVisible(false)]
public class SafeObservableCollection<T> : ObservableCollection<T>
{
    private readonly Dispatcher dispatcher;

    public SafeObservableCollection() : 
        this(Enumerable.Empty<T>())
    {

    }

    public SafeObservableCollection(Dispatcher dispatcher) : 
        this(Enumerable.Empty<T>(),dispatcher)
    {

    }

    public SafeObservableCollection(IEnumerable<T> collection) : 
        this(collection, Dispatcher.CurrentDispatcher)
    {

    }

    public SafeObservableCollection(IEnumerable<T> collection, Dispatcher currentDispatcher)
    {
        this.dispatcher = currentDispatcher;
        foreach (var item in collection)
        {
            this.Add(item);
        }
    }

    protected override void SetItem(Int32 index, T item)
    {
        this.ExecuteOrInvoke(() => this.SetItemBase(index, item));
    }

    protected override void MoveItem(Int32 oldIndex, Int32 newIndex)
    {
        this.ExecuteOrInvoke(() => this.MoveItemBase(oldIndex, newIndex));
    }

    protected override void ClearItems()
    {
        this.ExecuteOrInvoke(this.ClearItemsBase);
    }

    protected override void InsertItem(Int32 index, T item)
    {
        this.ExecuteOrInvoke(() => this.InsertItemBase(index, item));
    }

    protected override void RemoveItem(Int32 index)
    {
        this.ExecuteOrInvoke(() => this.RemoveItemBase(index));
    }

    private void RemoveItemBase(Int32 index)
    {
        base.RemoveItem(index);
    }

    private void InsertItemBase(Int32 index, T item)
    {
        base.InsertItem(index, item);
    }

    private void ClearItemsBase()
    {
        base.ClearItems();
    }

    private void MoveItemBase(Int32 oldIndex, int newIndex)
    {
        base.MoveItem(oldIndex, newIndex);
    }

    private void SetItemBase(Int32 index, T item)
    {
        base.SetItem(index, item);
    }

    private void ExecuteOrInvoke(Action action)
    {
        if (this.dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            this.dispatcher.BeginInvoke(action);
        }
    }
}
```

当然，在使用 SafeObservableCollection&lt;T&gt; 时，你应该在 UI 线程创建这个集合对象，不然 UI 也无法更新数据。