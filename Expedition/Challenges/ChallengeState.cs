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

    public ChallengeState() { }
    public ChallengeState(Challenge challenge)
    {
        completed = challenge.completed;
        hidden = challenge.hidden;
        revealed = challenge.revealed;
    }

    public Challenge CreateChallenge()
    {
        return (Challenge)Activator.CreateInstance(ChallengeType);
    }

    public virtual void ReadTo(ref Challenge challenge)
    {
        
        if (challenge?.GetType() != ChallengeType)
        {
            challenge = CreateChallenge();         
        }

        challenge.completed = completed;
        challenge.hidden = hidden;
        challenge.revealed = revealed;
    }

    public override string ToString()
    {
        return $"{base.ToString()} - completed: {completed}, hidden: {hidden}, revealed: {revealed}";
    }
}
