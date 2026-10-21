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
    public override Challenge GetChallenge => new PinChallenge();

    public PinChallengeState() { }
    public PinChallengeState(Challenge challenge) : base(challenge)
    {
        PinChallenge pc = (PinChallenge)challenge;
        current = pc.current;
        target = pc.target;
    }

    public override void ReadTo(Challenge challenge)
    {
        base.ReadTo(challenge);
        PinChallenge pc = (PinChallenge)challenge;
        pc.current = current;
        pc.target = target;
    }
    public override string ToString()
    {
        return $"{{{base.ToString()}: data - current: {current}, target: {target}}}";
    }
}
