using Expedition;
using System;

namespace RainMeadow;

public class PearlHoardChallengeState : ChallengeState
{
    [OnlineField]
    int amount;
    [OnlineField]
    bool common;
    [OnlineField(nullable = true)]
    string? region;

    public override Type ChallengeType => typeof(PearlHoardChallenge);
    public PearlHoardChallengeState() { }
    public PearlHoardChallengeState(Challenge challenge) : base(challenge)
    {
        PearlHoardChallenge phc = (PearlHoardChallenge)challenge;
        amount = phc.amount;
        common = phc.common;
        region = phc.region;
    }

    public override void ReadTo(ref Challenge challenge)
    {
        base.ReadTo(ref challenge);
        PearlHoardChallenge phc = (PearlHoardChallenge)challenge;
        phc.amount = amount;
        phc.common = common;
        phc.region = region;
    }
    public override string ToString()
    {
        return $"{{{base.ToString()} - common: {common}, region: {region}}}";
    }
}
