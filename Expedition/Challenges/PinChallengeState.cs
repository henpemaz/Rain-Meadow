using Expedition;
using System;

namespace RainMeadow;

public class PinChallengeState : ChallengeState
{
    [OnlineField]
    int current;
    [OnlineField]
    int target;

    public override Type ChallengeType => typeof(PinChallenge);
    public PinChallengeState() { }
    public PinChallengeState(Challenge challenge) : base(challenge)
    {
        PinChallenge pc = (PinChallenge)challenge;
        current = pc.current;
        target = pc.target;
    }

    public override void ReadTo(ref Challenge challenge)
    {
        base.ReadTo(ref challenge);
        PinChallenge pc = (PinChallenge)challenge;
        pc.current = current;
        pc.target = target;
    }
    public override string ToString()
    {
        return $"{{{base.ToString()} - current: {current}, target: {target}}}";
    }
}
