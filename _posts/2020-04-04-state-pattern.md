---
layout: post
title: STATE 模式 (1)
enable: true
---

### 嵌套 switch/case 语句

```C#
public enum State
{
    LOCKED,
    UNLOCKED
}

public enum Event
{
    COIN,
    PASS
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
    private State state = State.LOCKED;
    private TurnstileController turnstileCtrller;
    private Turnstile(TurnstileController action)
    {
        this.turnstileCtrller = action;
    }

    public void HandEvent(Event e)
    {
        switch (state)
        {
            case State.LOCKED:
                {
                    switch (e)
                    {
                        case Event.COIN:
                            state = State.UNLOCKED;
                            turnstileCtrller.Unlock();
                            break;
                        case Event.PASS:
                            turnstileCtrller.Alarm();
                            break;
                        default:
                            break;
                    }
                }
                break;
            case State.UNLOCKED:
                {
                    switch(e)
                    {
                        case Event.COIN:
                            turnstileCtrller.ThankYou();
                            break;
                        case Event.PASS:
                            state = State.LOCKED;
                            turnstileCtrller.Lock();
                            break;
                        default:
                            break;
                    }
                }
                break;
            default:
                break;
        }
    }
}
```
