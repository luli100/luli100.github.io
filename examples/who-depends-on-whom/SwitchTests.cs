using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DependencyExamples;

public sealed class SwitchTests
{
    [Fact]
    public void Press_On_ShouldTurnDeviceOn()
    {
        var fake = new FakeDevice();
        var sw = new Switch(fake);      // 把假设备注入进去

        sw.Press(true);

        Assert.True(fake.IsOn);         // 验证的是开关的逻辑，不是灯
    }

    [Fact]
    public void Press_Off_ShouldTurnDeviceOff()
    {
        var fake = new FakeDevice();
        fake.TurnOn();                  // 先打开，关闭才有可验证的变化
        var sw = new Switch(fake);

        sw.Press(false);

        Assert.False(fake.IsOn);
    }

    [Fact]
    public void Constructor_RejectsMissingDevice()
    {
        Assert.Throws<ArgumentNullException>(() => new Switch(null!));
    }

    [Fact]
    public void ManualAssembly_SupportsBothDevices()
    {
        var lightSwitch = new Switch(new Light());
        var fanSwitch = new Switch(new Fan());

        lightSwitch.Press(true);
        lightSwitch.Press(false);
        fanSwitch.Press(true);
        fanSwitch.Press(false);
    }

    [Fact]
    public void Container_CanAssembleTheSameSwitch()
    {
        var services = new ServiceCollection();
        services.AddTransient<ISwitchable, Light>();
        services.AddTransient<Switch>();

        using var provider = services.BuildServiceProvider();
        var sw = provider.GetRequiredService<Switch>();

        sw.Press(true);
    }
}
