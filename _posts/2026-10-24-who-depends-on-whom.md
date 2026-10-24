---
layout: post
title: 谁依赖谁：用换设备与做测试理解 DIP、IoC 和 DI
enable: true
description: 从开关控制灯的逐步改造出发，分清依赖倒置、控制反转与依赖注入，再把同样的思路用到设备适配和 C 语言算法替换中。
---

一段控制设备的代码已经能正常工作。现在，要把灯换成风扇，应该改哪里？如果手边没有真实设备，能不能先验证控制逻辑？

这两个问题，能帮我们看清代码中的依赖关系。DIP、IoC、DI 这几个经常一起出现的概念，也可以沿着同一次修改来理解。

本文先把一个开关逐步改到可以换设备、做测试，再把这个思路放到扫描系统和 C 语言的算法选择中。示例刻意简化了设备操作，重点看：使用设备的代码依赖什么，具体设备由谁准备，又怎样交给使用它的代码。

## 开关把灯写死了

先看最直接的实现：

```csharp
public sealed class Light
{
    public void TurnOn() => Console.WriteLine("灯亮了");
    public void TurnOff() => Console.WriteLine("灯灭了");
}

public sealed class Switch
{
    private readonly Light _light = new Light();

    public void Press(bool shouldOn)
    {
        if (shouldOn)
            _light.TurnOn();
        else
            _light.TurnOff();
    }
}
```

`Press(true)` 打开灯，`Press(false)` 关闭灯。这里用控制台输出代替真实硬件操作。

如果开关始终只控制这一种灯，这样写很直观。但一旦要换成风扇，就得回来修改 `Switch`。原因藏在这个字段里：

```csharp
private readonly Light _light = new Light();
```

它同时做了两个决定：字段类型指定为 `Light`，意味着开关认识具体的灯；`new Light()` 写在开关内部，意味着开关还负责创建设备。

测试也受到影响。我们想验证的是“传入打开指令时，开关有没有调用打开操作”，却无法给它提供一个记录调用的测试设备。创建 `Switch` 时，它就已经自己创建了 `Light`。

于是，修改有了明确目标：让开关只依赖自己需要的设备能力，同时允许外面提供设备。

## 有了接口，还差一步

开关需要设备提供什么能力？在这个例子里，只有打开和关闭。先把这两项能力写成接口：

```csharp
public interface ISwitchable
{
    void TurnOn();
    void TurnOff();
}

public sealed class Light : ISwitchable
{
    public void TurnOn() => Console.WriteLine("灯亮了");
    public void TurnOff() => Console.WriteLine("灯灭了");
}

public sealed class Fan : ISwitchable
{
    public void TurnOn() => Console.WriteLine("风扇转了");
    public void TurnOff() => Console.WriteLine("风扇停了");
}
```

`ISwitchable` 表达的是开关对设备的要求。灯和风扇内部怎样工作，可以各自实现。

接下来，把开关中的字段类型换成接口：

```csharp
private readonly ISwitchable _device = new Light();
```

调用时确实可以使用 `_device.TurnOn()` 和 `_device.TurnOff()` 了。不过，换成风扇时，我们仍然得修改这一行，把 `new Light()` 换成 `new Fan()`。

**通过接口调用，只解决了调用这一侧的问题。创建时对具体类型的依赖，还留在 `Switch` 里。**

判断依赖关系时，要看完整的类。字段、构造函数、方法体里，只要仍然引用具体设备类型，这段代码就仍然认识具体设备。

## 把设备的选择和创建交给外面

继续改造，让 `Switch` 从构造函数接收设备：

```csharp
public sealed class Switch
{
    private readonly ISwitchable _device;

    public Switch(ISwitchable device)
    {
        _device = device
            ?? throw new ArgumentNullException(nameof(device));
    }

    public void Press(bool shouldOn)
    {
        if (shouldOn)
            _device.TurnOn();
        else
            _device.TurnOff();
    }
}
```

此时，`Switch` 只声明“我需要一个能打开、能关闭的设备”。到底用灯还是风扇，由外面的装配代码决定：

```csharp
// Program.cs：选择实现，并把对象组装起来。
var lightSwitch = new Switch(new Light());
var fanSwitch = new Switch(new Fan());

lightSwitch.Press(true);
fanSwitch.Press(true);
```

两个对象使用同一个 `Switch` 类，控制不同的设备。以后增加一种满足接口约定的设备，开关的控制逻辑可以继续使用。

`new Light()` 没有消失，它移动到了装配入口。系统总得有地方知道具体实现，并把对象连接起来。集中承担这项职责的入口，通常称为**组合根（Composition Root）**。小程序里，它可以就是 `Program.cs`。

这次修改把职责分开了：`Switch` 负责使用设备，装配入口负责选择设备、创建对象，并把它交给开关。

## 同一次修改，三个观察角度

现在回头看 DIP、IoC 和 DI。它们描述的是这次修改中的不同方面。

| 概念 | 关注的问题 | 在这个例子中的体现 |
| --- | --- | --- |
| DIP，依赖倒置原则 | 高层逻辑依赖什么？抽象是否带着实现细节？ | `Switch` 依赖 `ISwitchable`，设备实现这个接口 |
| IoC，控制反转 | 哪部分控制权交给了外部？ | 设备的选择、创建和装配由外部入口负责 |
| DI，依赖注入 | 使用者怎样得到依赖？ | 装配代码通过构造函数传入设备 |

### DIP：倒置的是源代码依赖的方向

在这个局部例子中，开关的控制逻辑是高层，设备的具体实现是低层。DIP 的要求可以概括为：高层模块与低层模块都依赖抽象，抽象不依赖实现细节，具体实现依赖抽象。

原来，`Switch` 直接依赖 `Light`。改造后，`Switch` 依赖自己需要的能力约定，`Light` 和 `Fan` 来实现这个约定。

![改造前 Switch 直接依赖 Light；改造后 Switch 依赖 ISwitchable，Light 和 Fan 实现 ISwitchable。图中的箭头表示源代码依赖。](/images/posts/who-depends-on-whom/dependencies.svg)

图中的箭头表示源代码依赖。程序运行时，开关仍然会调用灯或风扇的方法。**源代码依赖的方向和运行时调用的方向，需要分开看。**

接口放在哪个项目也会影响依赖。一个常见安排是：高层流程定义它需要的接口，设备适配项目引用这个接口并实现它，装配入口引用两边。如果高层项目为了使用接口，仍然必须引用整个厂家实现项目，就还需要检查项目之间的关系。

这里的关键是让抽象表达调用方的需要。接口里一旦出现 `VendorAHandle` 之类的厂家类型，使用接口的代码就仍然受厂家细节影响。

### IoC：说清交出去的控制权

在开关的例子里，原本由 `Switch` 自己决定用什么设备、如何创建它。改造后，这些决定由外面的装配者做出。这是对象装配方面的控制反转。

IoC 也出现在其他场景。比如，设备 SDK 收到一帧数据后调用我们的回调，处理逻辑由我们编写，调用时机由 SDK 决定。单元测试框架发现并执行测试方法，也由框架安排执行流程。

因此，看到“IoC”时，可以接着问一句：这里交出去的是对象的选择与创建，还是代码的调用时机、执行流程？把这一点说具体，概念就容易落到代码上。

### DI：外面准备好，再传进来

`new Switch(new Light())` 就已经使用了依赖注入。设备由外面提供，开关通过构造函数接收它，这就是构造函数注入。

DI 还可以通过属性或方法参数完成：

| 方式 | 依赖的使用范围 | 需要注意什么 |
| --- | --- | --- |
| 构造函数注入 | 对象正常工作一直需要的依赖 | 创建时就准备好，校验必要条件 |
| 属性注入 | 创建后设置，常用于可选依赖 | 没有赋值时，需要明确的默认行为或错误提示 |
| 方法注入 | 某次调用需要的依赖 | 明确它是否只在这次调用中使用 |

开关始终需要设备，放在构造函数里最清楚。调用者只要成功创建了开关，就知道设备已经提供了。

另外，外面传入对象时，还要约定谁负责释放它。设备连接可能持有串口、网络连接或 SDK 句柄；注入动作本身不会自动确定资源的所有权。

## 没有真设备，怎样验证开关

设备能从外面传进来后，测试代码就可以提供自己的实现。

这个测试设备只记录状态和调用次数：

```csharp
public sealed class FakeDevice : ISwitchable
{
    public bool IsOn { get; private set; }
    public int TurnOnCalls { get; private set; }
    public int TurnOffCalls { get; private set; }

    public void TurnOn()
    {
        IsOn = true;
        TurnOnCalls++;
    }

    public void TurnOff()
    {
        IsOn = false;
        TurnOffCalls++;
    }
}
```

下面用 xUnit 同时验证打开和关闭两个分支：

```csharp
[Theory]
[InlineData(true)]
[InlineData(false)]
public void Press_SendsExpectedCommand(bool shouldOn)
{
    var fake = new FakeDevice();

    // 从相反状态开始，避免关闭测试因默认就是 false 而误通过。
    if (shouldOn)
        fake.TurnOff();
    else
        fake.TurnOn();

    int onBefore = fake.TurnOnCalls;
    int offBefore = fake.TurnOffCalls;
    var sw = new Switch(fake);

    sw.Press(shouldOn);

    Assert.Equal(shouldOn, fake.IsOn);
    Assert.Equal(onBefore + (shouldOn ? 1 : 0), fake.TurnOnCalls);
    Assert.Equal(offBefore + (shouldOn ? 0 : 1), fake.TurnOffCalls);
}
```

状态断言检查结果，调用次数断言还可以发现多调了一次、两个分支都调了等错误。测试替身的行为越简单，越容易看清测试究竟验证了什么。

这个测试验证了开关是否发送正确的控制命令。真实设备能否打开、SDK 是否正确工作、连接中断后怎样处理，仍然需要设备适配层测试和真机集成测试。

换设备和做测试，正好使用了同一个入口：生产环境传入真实实现，测试环境传入可控制、可观察的替身。

文末提供了[完整测试项目](https://github.com/luli100/luli100.github.io/tree/main/examples/who-depends-on-whom)，可以直接运行，验证这里的代码。

## 放到扫描系统里：流程依赖设备能力

开关例子很小，同样的关系也适用于设备控制系统。

假设扫描主流程需要让转台依次转到指定角度，再进行数据采集。流程真正需要的是“转到指定角度”的能力，可以先约定：

```csharp
public interface ITurntable
{
    // 约定：正常完成表示已经到位，失败则报告异常。
    Task MoveToAsync(double angle);
}

public sealed class ScanFlow
{
    private readonly ITurntable _turntable;

    public ScanFlow(ITurntable turntable)
    {
        _turntable = turntable
            ?? throw new ArgumentNullException(nameof(turntable));
    }

    public async Task RunAsync(IEnumerable<double> angles)
    {
        foreach (double angle in angles)
        {
            await _turntable.MoveToAsync(angle);
            // 到位后，采集数据、记录结果。
        }
    }
}
```

厂家 A 和厂家 B 的设备适配器分别实现 `ITurntable`。适配器内部对接 SDK，负责把厂家协议转换成流程需要的行为。装配入口再选择具体实现：

```csharp
// 示意：VendorATurntable 是封装厂家 SDK 的适配器。
ITurntable turntable = new VendorATurntable();
var scan = new ScanFlow(turntable);
```

换厂家时，修改适配器和装配入口；只要新实现满足原来的行为约定，扫描顺序就可以继续复用。测试时，也可以用一个记录角度的假转台，检查流程有没有按计划依次发出移动请求。

这里有一个比方法名更重要的细节：`MoveToAsync` 到底什么时候算完成？如果一家 SDK 在“指令已发送”时返回，另一家在“实际到位”时返回，两个同名方法还不能直接互换。适配器需要把它们统一到接口约定的语义。

实际项目还要明确角度单位、超时、取消、失败后的状态。这些约定决定了流程能否安全地使用不同实现。相同的方法签名，只是起点。

## C 语言里，也可以从外部提供算法

DIP、IoC 和 DI 的思路也能用于 C 代码。

例如，目标跟踪主循环需要选择 NN 或 GNN 关联算法。可以用一个函数指针类型，约定主循环需要的关联操作：

```c
/* association.h：公共契约。
   tracks_t、dets_t 由公共数据模型声明，此处省略。 */
typedef void (*associate_fn)(tracks_t *tracks, dets_t *dets);

typedef struct {
    associate_fn associate;
} tracker_t;

void tracker_init(tracker_t *tracker, associate_fn fn);
void tracker_step(tracker_t *tracker, tracks_t *tracks, dets_t *dets);
```

主循环保存传入的函数，并在需要时调用：

```c
/* tracker.c：包含公共契约，不包含 nn.h 或 gnn.h。 */
void tracker_init(tracker_t *tracker, associate_fn fn)
{
    /* 调用方须传入有效的 tracker 和非空函数指针。 */
    tracker->associate = fn;
}

void tracker_step(tracker_t *tracker, tracks_t *tracks, dets_t *dets)
{
    tracker->associate(tracks, dets);
    /* 根据关联结果，继续更新航迹。 */
}
```

由 `main.c` 选择具体算法：

```c
/* main.c：包含算法声明，负责选择与装配。 */
tracker_t tracker;
tracker_init(&tracker, gnn_associate);
```

这组代码是关系示意，省略了数据模型、算法实现和错误处理。使用实例结构体保存函数指针，可以让不同跟踪器实例选择不同实现，避免把选择存成全局状态。

对应关系很直接：`associate_fn` 是抽象契约，NN、GNN 函数是具体实现，`tracker_step` 使用算法，`main.c` 选择并提供算法。测试时可以传入一个产生固定关联结果的函数，检查主循环后续的处理。

要符合 DIP，还需要保证公共数据类型和函数约定没有夹带某个算法的私有细节。把函数指针传进去是 DI；抽象是否合适，需要另外检查。

## 哪些写法容易混淆

### 传入具体类型，也用了 DI

```csharp
public Switch(Light light)
{
    _light = light;
}
```

设备从外面提供，这是依赖注入，创建权也已经移到了外部。但参数类型仍然是 `Light`，开关仍直接依赖具体设备。就本文希望让控制逻辑独立于设备实现的目标而言，DIP 还没有满足。

因此，“有没有 DI”和“是否符合 DIP”要分别判断。

### 一个配置值变化，信息还不够

假设信号处理中的 chirp 数从 384 改成 512，数值来自配置文件。仅凭这一点，无法判断依赖怎样传入。

如果算法自己读取配置文件，它仍然承担配置获取的工作。如果外面的入口读取配置、构造配置对象，再传给算法，那么就可以形成配置依赖的注入。还需要继续看代码，才能知道依赖关系与控制权怎样安排。

### 装配入口中的 new，有它的职责

`new` 出现在什么位置，要结合代码职责判断。

`Switch` 内的 `new Light()` 把设备选择写进了控制逻辑。组合根里的 `new Light()` 则是在完成对象装配。分析时，应该先划清高层流程与装配代码的边界。

同样，普通值对象、局部集合的创建，也不必机械地全部改成注入。值得分离的通常是会变化的实现、外部资源，以及需要独立测试的协作对象。

## 容器是装配工具

前面的手工装配已经完成了依赖注入：

```csharp
var sw = new Switch(new Light());
```

当对象越来越多，依赖层次变深，还要管理对象的共享与释放时，可以使用 DI 容器。在 .NET 中，下面的代码使用 `Microsoft.Extensions.DependencyInjection` 包：

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddTransient<ISwitchable, Light>();
services.AddTransient<Switch>();

using var provider = services.BuildServiceProvider();
var sw = provider.GetRequiredService<Switch>();
sw.Press(true);
```

装配入口声明“需要 `ISwitchable` 时提供 `Light`”，容器按照注册关系创建和组装对象。这类容器也常被称为 IoC 容器。

容器不会替我们设计接口。生命周期仍需结合业务选择，解析也应该留在适当的装配边界。如果 `Switch` 自己拿着 `IServiceProvider` 到处查找设备，依赖就重新藏进了方法体，构造函数也不再清楚地表达它的需要。

对象少时，手工装配容易理解。决定是否引入容器，可以看对象关系和生命周期管理是否已经带来实际的维护负担。

## 接口能不能承受下一次变化

把具体类型替换成接口后，还要检查抽象是否真的合适。

**先看接口有没有泄漏厂家细节。** `Open(VendorAHandle handle)` 会把厂家类型带到调用方。更合适的做法通常是让厂家句柄留在适配器内部，对外暴露业务需要的连接参数或操作。

**再看实现能否兑现接口的能力。** 如果一个接口同时要求单轴和双轴功能，单轴转台可能被迫实现自己做不到的方法。可以按能力拆分接口，让流程依赖它真正需要的部分；需要可选能力时，再明确支持条件。

**还要看输入输出是否足够。** 如果新的关联算法需要额外的上下文，原有函数签名可能就需要调整。抽象有适用范围，新增需求超出这个范围时，应该重新约定职责和数据，而不是把差异藏在类型转换、特殊参数或未说明的副作用里。

接口设计得合适，才能把变化留在预期的位置。接口数量本身，说明不了设计质量。

回到最初的开关：修改之后，它通过接口使用设备，具体设备由外部准备，再通过构造函数传进来。依赖关系符合 DIP 的要求，装配控制权的变化体现了 IoC，传入设备的做法是 DI。

检查自己的代码时，也可以沿着同一条线找下去：**设备或算法在哪里选择、在哪里创建，又怎样交给使用它的代码？下一次换实现时，需要修改哪些地方？**

## 示例与延伸阅读

- [完整 C# 示例与 xUnit 测试](https://github.com/luli100/luli100.github.io/tree/main/examples/who-depends-on-whom)：包含开关、灯、风扇与测试替身，使用 .NET 8 SDK 运行 `dotnet test`。
- Robert C. Martin：[Design Principles and Design Patterns](https://staff.cs.utu.fi/~jounsmed/doos_06/material/DesignPrinciplesAndPatterns.pdf)，其中的 DIP 章节讨论高层模块、抽象与具体实现的依赖关系。
- Martin Fowler：[Inversion of Control Containers and the Dependency Injection pattern](https://www.martinfowler.com/articles/injection.html)，讨论依赖注入、外部装配，以及配置和使用的分离。
- Microsoft Learn：[.NET dependency injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/overview)，介绍 .NET 容器的注册与使用。
