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
    public override Challenge GetChallenge => new PearlDeliveryChallenge();

    public PearlDeliveryChallengeState() { }
    public PearlDeliveryChallengeState(Challenge challenge) : base(challenge)
    {
        PearlDeliveryChallenge pdc = (PearlDeliveryChallenge)challenge;
        iterator = pdc.iterator;
        region = pdc.region;
    }

    public override void ReadTo(Challenge challenge)
    {
        base.ReadTo(challenge);
        PearlDeliveryChallenge pdc = (PearlDeliveryChallenge)challenge;
        pdc.iterator = iterator;
        pdc.region = region;
    }
    public override string ToString()
    {
        return $"{{{base.ToString()}: data - iterador: {iterator}, region: {region}}}";
    }
}
