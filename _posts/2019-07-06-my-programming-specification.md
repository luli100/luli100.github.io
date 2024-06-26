---
layout: post
title: 本人遵循的编码规范
enable: false
---

### 命名

C#: [Naming guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines)

### 设计函数时，不要将 Boolean 类型设计成函数参数，设计成两个函数是比较好的实践。

坏的实践：

```c#
private void ShowHideWindow(Boolean value)
{
    if (value)
    {
        // 正向的逻辑
    }
    else
    {
        // 反向的逻辑
    }
}
```

上面的代码，还存在违反单一职责原则问题。

好的实践：

```c#
private void ShowWindow()
{
    // 你的逻辑
}

private void HideWindow()
{
    // 你的逻辑
}
```

这既不违反单一职责原则，也不会产生歧义。

### 避免使用魔术字符串/数字

什么是魔术字符串？它们是直接在应用程序代码中指定的字符串，对应用程序行为有直接影响。换句话说，不要在我们的应用程序中使用硬编码的字符串或值。当您的应用程序增长时，很难跟踪这些字符串。此外，这些字符串可以与某种外部引用相关联，例如文件名、文件路径、URL 等。在这种情况下，当资源的位置发生变化时，所有这些魔术字符串都必须更新，否则该应用程序中断。

坏的实践：

```c#
private void Set(String userRole)
{
    if (userRole == "Admin")
    {
        // 你的逻辑
    }
}
```

好的实践：

```c#
private const string ADMIN_ROLE = "Admin";
private void Set(String userRole)
{
    if (userRole == ADMIN_ROLE)
    {
        // 你的逻辑
    }
}
```

或者，您也可以为用户角色创建一个枚举并简单地使用它。这是一种更简洁的代码编写方式。

### 删除未使用的代码

通常有注释掉未使用代码的做法。这最终会在编译应用程序时增加代码行数。你不想这样做。您可以使用 Git 等源代码控制来确保您可以随时恢复。更喜欢使用 Git 而不是注释掉代码。

### 避免过多的参数

太多的参数总是一场噩梦。如果您倾向于对任何方法有超过 3 个参数输入，为什么不将其包装到请求对象或其他东西中然后传递呢？

坏的实践：

```c#
public void SomeMethod(string name, string city, int age, string section)
{
    // 你的逻辑
}
```

好的实践：

```c#
public void SomeMethod(Student student)
{
    // 你的逻辑
}
```

### 永远不要省略花括号

很多语言允许你在 if 语句里面只有一句话的时候省略掉花括号，这会带来不必要的麻烦。

坏的实践：

```c#
if (condition)
    Action();
```

好的实践：

```c#
if (condition)
{
    Action();
}
```

### 合理使用括号，不要依赖操作符优先级

对加减乘除操作符优先级的记忆是刻在骨子里的，不适用括号是没有问题的，但是在编程语言中，除了加减乘除操作符之外，还有移位、与等其它操作符，他们与加减乘除的操作的优先级组合的时候，那么你就很难记住了。

你觉得 2 &lt;&lt; 7 - 2 * 3 这样的表达式结果等于多少？是 250？其实它的结果是 4。因为 &lt;&lt; 操作符的优先级比 +/- 还低，所以该表达式等价于 2 &lt;&lt; (7 - 2 * 3)，而不是 (2 &lt;&lt; 7) - 2 * 3。因此，加入合理的括号来表明表达式的计算顺序，停止死记硬背操作符的优先级是更好的选择。