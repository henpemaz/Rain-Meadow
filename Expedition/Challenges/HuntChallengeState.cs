using Expedition;
using System;

namespace RainMeadow;

public class HuntChallengeState : ChallengeState
{
    [OnlineField]
    int amount;
    [OnlineField]
    int current;
    [OnlineField(nullable = true)]
    string? target;

    public override Type ChallengeType => typeof(HuntChallenge);
    public override Challenge GetChallenge => new HuntChallenge();

    public HuntChallengeState() { }
    public HuntChallengeState(Challenge challenge) : base(challenge)
    {
        HuntChallenge hc = (HuntChallenge)challenge;
        amount = hc.amount;
        current = hc.current;
        target = hc.target.value;
    }

    public override void ReadTo(Challenge challenge)
    {
        base.ReadTo(challenge);
        HuntChallenge hc = (HuntChallenge)challenge;
        hc.amount = amount;
        hc.current = current;
        hc.target = (CreatureTemplate.Type)ExtEnumBase.Parse(typeof(CreatureTemplate.Type), target, false);
    }
    public override string ToString()
    {
        return $"{{{base.ToString()}: data - amount: {amount}, current: {current}, target: {target}}}";
    }
}
