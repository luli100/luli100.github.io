namespace DependencyExamples;

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
