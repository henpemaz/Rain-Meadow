using Expedition;

using System;

namespace RainMeadow;

public abstract class ChallengeState : OnlineState
{
    [OnlineField]
    public bool completed;    
    [OnlineField]
    bool hidden;
    [OnlineField]
    bool revealed;

    public abstract Type ChallengeType { get; }
    public abstract Challenge GetChallenge { get; }

    public ChallengeState() { }
    public ChallengeState(Challenge challenge)
    {
        completed = challenge.completed;
        hidden = challenge.hidden;
        revealed = challenge.revealed;
    }

    public virtual void ReadTo(Challenge challenge)
    {
        challenge.completed = completed;
        challenge.hidden = hidden;
        challenge.revealed = revealed;
    }
}
