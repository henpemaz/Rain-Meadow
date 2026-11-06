using Expedition;
using System;

namespace RainMeadow;

public class PearlDeliveryChallengeState : ChallengeState
{
    [OnlineField]
    int iterator;
    [OnlineField(nullable = true)]
    string? region;

    public override Type ChallengeType => typeof(PearlDeliveryChallenge);

    public PearlDeliveryChallengeState() { }
    public PearlDeliveryChallengeState(Challenge challenge) : base(challenge)
    {
        PearlDeliveryChallenge pdc = (PearlDeliveryChallenge)challenge;
        iterator = pdc.iterator;
        region = pdc.region;
    }

    public override void ReadTo(ref Challenge challenge)
    {
        base.ReadTo(ref challenge);
        PearlDeliveryChallenge pdc = (PearlDeliveryChallenge)challenge;
        pdc.iterator = iterator;
        pdc.region = region;
    }
    public override string ToString()
    {
        return $"{{{base.ToString()} - iterador: {iterator}, region: {region}}}";
    }
}
