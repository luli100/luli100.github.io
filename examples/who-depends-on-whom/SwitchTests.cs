using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DependencyExamples;

public sealed class SwitchTests
{
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
