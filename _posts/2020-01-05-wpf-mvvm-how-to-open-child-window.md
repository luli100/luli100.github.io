---
layout: post
title: 在 WPF 中使用 MVVM 模式时，如何打开子窗口
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
            <h2>在 WPF 中使用 MVVM 模式时，如何打开子窗口</h2>
            <p>通常来说，MVVM 模式中的 ViewModel 是不需知道 View 是如何设计的。也就是说，从 ViewModel 中访问『创建』View 是错误的设计。</p>
            <h3>直接在 View 后台代码打开子窗口</h3>
            <div class="highlighter-rouge"><div class="highlight"><pre class="highlight"><code>public partial class MainWindow : Window
{
    private readonly MainViewModel MainVM;
    public MainWindow()
    {
        InitializeComponent();
        this.MainVM = this.DataContext as MainViewModel;
        this.btnOpen.Click += BtnOpen_Click;
    }

    private void BtnOpen_Click(object sender, RoutedEventArgs e)
    {
        ChildWindow window = new ChildWindow() { Owner = this};
            var result = window.ShowDialog();
        if (result == true)
        {
            // 调用 MainViewModel 里的函数，进行相应的逻辑处理
        }
    }
}</code></pre></div></div>
                <p>这种方式能够满足 MVVM 模式的要求，但在 View 后台代码中访问 ViewModel 不够优雅。</p>
                <h3>以 MVVMLight Messenger 消息通知法打开子窗体</h3>
                <div class="highlighter-rouge"><div class="highlight"><pre class="highlight"><code>public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Messenger.Default.Register<DialogMessage>(this,MessageHandler);
    }

    private void MessageHandler(DialogMessage message)
    {
        ChildWindow window = new ChildWindow() { Owner = this };
        var result = window.ShowDialog();
    }
}</code></pre></div></div>
            <p>这种方法有 2 个问题：1、很难调试和导航到打开子窗口处理函数『MessageHandler』；2、获取打开窗口的返回值比较困难。</p>
            <h3>以委托的方式打开子窗口</h3>
            <div class="highlighter-rouge"><div class="highlight"><pre class="highlight"><code>public partial class MainWindow : Window
{
    private readonly MainViewModel MainVM;
    public MainWindow()
    {
        InitializeComponent();
        this.MainVM = this.DataContext as MainViewModel;
        this.MainVM.SetOpenWindow(Open);
    }

    private Boolean Open()
    {
        ChildWindow window = new ChildWindow() { Owner = this };
        var result = window.ShowDialog();
        return result == true;
    }
}</code></pre></div></div>
            <p>这种方法似乎解决了前 2 中方法的问题，但似乎还可以实现得更优雅，更通用。</p>
            <h3>以导航服务的方式打开子窗口</h3>
            <div class="highlighter-rouge"><div class="highlight"><pre class="highlight"><code>public class NavigationService
{
    private Dictionary<String, Type> windows { get; } = new Dictionary<String, Type>();

    private readonly IServiceProvider serviceProvider;

    public void Configure(String key, Type windowType)
    {
        if (!windows.ContainsKey(key))
        {
            windows.Add(key, windowType);
        }
    }

    public NavigationService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public void Show(String windowKey)
    {
        var window = GetAndActivateWindow(windowKey);
        window.Show();
    }

    public Boolean? ShowDialog(String windowKey, Window parentWindow)
    {
        var window = GetAndActivateWindow(windowKey);
        window.Owner = parentWindow;
        return window.ShowDialog();
    }

    private Window GetAndActivateWindow(String windowKey)
    {
        var window = serviceProvider.GetRequiredService(windows[windowKey]) as Window;
        return window;
    }
}</code></pre></div></div>

        <div class="highlighter-rouge"><div class="highlight"><pre class="highlight"><code>public static class WindowsKeys
{
    public const string MainWindow = nameof(MainWindow);
    public const string ChildWindow = nameof(ChildWindow);
}</code></pre></div></div>
            <p>这种方法类似于所有窗口都在一个上帝类中打开，只需要传入对应窗口的 Key。并且，在 WindowsKeys 类中全局管理程序中可以打开哪些窗口。</p>
        </div>
        
        <script>
            if (/mobile/i.test(navigator.userAgent) || /android/i.test(navigator.userAgent))
            {
                document.body.classList.add('mobile');
            }
        </script>
    </body>
</html>