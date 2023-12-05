---
layout: post
title: MVVM 模式在 WPF 中的正确使用方式
enable: true
---

我想使用过 WPF 的程序员，应该都知道 MVVM 模式。然而，争议最多的是，是否应该在 Model 类中实现 INotifyPropertyChanged 接口。我的结论是：<strong>不应该在 Model 类中实现 INotifyPropertyChanged 接口。</strong>

### MVVM 层与层的依赖关系

<img src="/images/dependencies-between-layers-in-mvvm.png" width="60%">

Model 代表真实状态内容的领域模型（面向对象），或者代表数据访问层（以数据为中心）；View 是用户在屏幕上看到的接口、布局和外观（UI）；ViewModel 是暴露公共属性和命令的 View 抽象，View 与 ViewModel 通过绑定器进行通信。

View 类不知道 Model 类的存在，而 ViewModel 和 Model 也不知道 View 是如何设计的。事实上，Model 完全不会知道 ViewModel 和 View 存在的事实。这是一个非常松散耦合的设计，同时，便于对 ViewModel 进行单元测试。

### Model 类不应该实现 INotifyPropertyChanged 接口

Model 类的设计其实与 MVVM 模式无关。你可以创建一个 ViewModel 类来将任何数据对象调整为对 WPF 友好的东西。Model 类不应该有任何内容表明它正在使用 MVVM 模式 和 WPF 框架。因为，Model 很容易来自遗留业务库。

**错误的设计**

```c#
public class User : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private String name;
    public String Name
    {
        get { return name; }
        set 
        { 
            if (name != value)
            {
                RaisePropertyChanged("Name");
            }
        }
    }

    private String email;
    public String Email
    {
        get { return email; }
        set
        {
            if (email != value)
            {
                RaisePropertyChanged("Email");
            }
        }
    }
}
```

**正确的设计**

```c#
public class User
{
    public String Name { get; set; }

    public String Email { get; set; }
}
```

### 在 ViewModel 类中实现 INotifyPropertyChanged 接口

请注意，ViewModel 是 Model 对象的包装器。它通过一组属性公开 Model 的状态，在 View 控件中使用状态。ViewModel 不复制 Model 的状态；它只是通过委托将其公开，如下所示：

```c#
public class UserViewModel : INotifyPropertyChanged
{
    private readonly User user;
    public UserViewModel(User user)
    {
        this.user = user;
    }

    public String Name
    {
        get { return this.user.Name; }
        set
        {
            if (value != this.user.Name)
            {
                this.user.Name = value;
                RaisePropertyChanged("Name");
            }
        }
    }

    public String Email
    {
        get { return this.user.Email; }
        set
        {
            if (value != this.user.Email)
            {
                this.user.Email = value;
                RaisePropertyChanged("Email");
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

你可以理解为 ViewModel 是一个或多个 Model 的属性包装器，并向 View 暴露公共属性和命令的地方。

### ObservableCollection&lt;T&gt; 怎么办

为了在 View 上显示项目集合，你需要使用 ObservableCollection&lt;T&gt;。那应该如何设计和使用 ObservableCollection&lt;T&gt; 呢？

**错误的设计**

```c#
public class MultiUserViewModel
{
    public ObservableCollection<User> Users { get; private set; } = new ObservableCollection<User>();
}
```

**正确的设计**

```c#
public class MultiUserViewModel
{
    public ObservableCollection<UserViewModel> Users { get; private set; } = new ObservableCollection<UserViewModel>();
}
```

### 总结

MVVM 模式的主要核心在 View 和 ViewModel 的交互上。不管我们是否使用 MVVM 模式，Model 都是存在的。其实，MVVM 仅仅是一个指导方针，而不是规则/规范。因此，你在使用 MVVM 模式进行应用程序开发时，在不违背大的理论基础上，可以根据自己的使用场景做一些变动。