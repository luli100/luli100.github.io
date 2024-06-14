---
layout: post
title: STATE 模式 (2)
enable: true
---

### 状态迁移表

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
    private class Transition
    {
        public State startState;
        public Event trigger;
        public State endState;
        public Action action;
        public Transition(State start, Event e, State end, Action a)
        {
            this.startState = start;
            this.trigger = e;
            this.endState = end;
            this.action = a;
        }
    }

    private List<Transition> transitions = new List<Transition>();
    private State state = State.LOCKED;
    private Turnstile(TurnstileController controller)
    {
        transitions.Add(new Transition(State.LOCKED, Event.COIN, State.UNLOCKED,controller.Unlock));
        transitions.Add(new Transition(State.LOCKED, Event.PASS, State.LOCKED, controller.Alarm));
        transitions.Add(new Transition(State.UNLOCKED, Event.COIN, State.UNLOCKED, controller.ThankYou));
        transitions.Add(new Transition(State.UNLOCKED, Event.PASS, State.LOCKED, controller.Lock));
    }


    public void HandEvent(Event e)
    {
        foreach (var transition in transitions)
        {
            if (state == transition.startState &&
                e == transition.trigger)
            {
                state = transition.endState;
                transition.action();
            }
        }
    }
}
```