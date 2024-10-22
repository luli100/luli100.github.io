---
layout: post
title: NULL OBJECT 模式
enable: true
---

考虑如下代码：

```C#
Employee e = DB.GetEmployedd("Bob");
if (e != null && e.IsTimeToPay())
{
    e.Pay();
}
```

上面代码的意思是：从数据库中获取名为 "Bob" 的 Employee 对象。如果该对象不存在，DB 对象就返回 null；否则，就返回请求的 Employee 对象。如果 Employee 存在，并且到了他的发薪日，就调用 Pay 方法。

我们曾经都编写过类似这样的代码。大多数人也曾忘记对 null 进行检查而受挫。有没有方法可以减少出错的可能？

### try/catch 代码块

通过让 DB.GetEmployee 抛出一个异常而不是返回 null，可以减少错误。不过，try/catch 块比对 null 的检查更不优雅。

### NULL OBJECT 模式

```C#
public abstract class Employee
{
    public abstract Boolean IsTimeToPay();
    public abstract void Pay();

    public static readonly Employee NULL = new NullEmployee();

    private class NullEmployee : Employee
    {
        public override Boolean IsTimeToPay()
        {
            return false;
        }

        public override void Pay()
        {

        }
    }
}
```

使用这个模式，可以消除对 null 进行检查。最初的代码可以改为：

```C#
Employee e = DB.GetEmployedd("Bob");
if (e.IsTimeToPay())
{
    e.Pay();
}
```

让 NullEmployee 类成为一个 private 内嵌类是为了确保该类只有一个单一实例。实际上并不存在 NullEmployee 类，因为其他任何人都无创建 NullEmployee 的其他实例。这会带来另外一个好处，因为我们希望可以这样表达：

```C#
if (e == Employee.NULL)
{

}
```

如果可以创建 NullEmployee 类的多个实例，那么上面的表达方式就是不可靠的。

### NULL OBJECT 模式并不完美

**对象有复杂行为或状态** NULL OBJECT 模式适用于那些不需要实际操作的简单对象。如果对象有复杂的行为或状态变化，使用 NULL OBJECT 可能会使逻辑复杂化，因为你需要为每个方法提供“无操作”的实现，并且可能难以捕捉所有逻辑分支。

**例子：** 在一个复杂的财务计算中，如果某个对象需要根据不同状态执行不同计算，NULL OBJECT 很难适当地模拟这些行为，可能导致不一致的结果。

**逻辑简单，null 检查更直观** 在一些简单的逻辑中，直接使用 null 检查可能更容易理解和维护，而不是引入一个新的 Null Object 类。如果 null 检查频率很低，或者逻辑简单，引入 Null Object 可能被认为是过度设计。

**例子：** 在一个简单的文件处理程序中，检查文件是否为 null 只需要一行代码，使用 NULL OBJECT 反而会让代码变得复杂，而没有带来实际的好处。

### 结论

NULL OBJECT 模式，在得到避免 null 检查，简化代码逻辑，提供默认行为的好处之外，也会带来一定的复杂性，也可能造成过度设计。回过头再看开始的代码，为什么 Bob 会以数据库对象举例，也说明了一定的问题。