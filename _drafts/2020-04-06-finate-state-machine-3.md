---
layout: post
title: 实现有限状态机 FSM (3)
enable: true
---

### STATE 模式

```C#
public interface TurnstileState
{
    void Coin(Turnstile t);
    void Pass(Turnstile t);
}

public class LockedTurnstileState : TurnstileState
{
    public void Coin(Turnstile t)
    {
        t.SetUnlocked();
        t.Unlock();
    }

    public void Pass(Turnstile t)
    {
        t.Alarm();
    }
}

public class UnlockedTurnstileState : TurnstileState
{
    public void Coin(Turnstile t)
    {
        t.ThankYou();
    }

    public void Pass(Turnstile t)
    {
        t.SetLocked();
        t.Lock();
    }
}

public interface TurnstileController
{
    void Lock();
    void Unlock();
    void ThankYou();
    void Alarm();
}

public class Turnstile
{
    private static readonly TurnstileState lockedState = new LockedTurnstileState();
    private static readonly TurnstileState unlockedState = new UnlockedTurnstileState();
    private TurnstileState state = unlockedState;

    private TurnstileController turnstileCtrller;
    public Turnstile(TurnstileController controller)
    {
        this.turnstileCtrller = controller;
    }

    public void Coin()
    {
        state.Coin(this);
    }

    public void Pass()
    {
        state.Pass(this);
    }

    public void SetLocked()
    {
        state = lockedState;
    }

    public void SetUnlocked()
    {
        state = unlockedState;
    }

    public Boolean IsLocked()
    {
        return state == lockedState;
    }

    public Boolean IsUnlocked()
    {
        return state == unlockedState;
    }

    public void ThankYou()
    {
        turnstileCtrller.ThankYou();
    }

    public void Alarm()
    {
        turnstileCtrller.Alarm();
    }

    public void Lock()
    {
        turnstileCtrller.Lock();
    }

    public void Unlock()
    {
        turnstileCtrller.Unlock();
    }
}
```