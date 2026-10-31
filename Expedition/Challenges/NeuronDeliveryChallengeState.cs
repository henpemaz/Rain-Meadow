using Expedition;
using System;

namespace RainMeadow;

public class NeuronDeliveryChallengeState : ChallengeState
{
    [OnlineField]
    int delivered;
    [OnlineField]
    int neurons;

    public override Type ChallengeType => typeof(NeuronDeliveryChallenge);

    public NeuronDeliveryChallengeState() { }
    public NeuronDeliveryChallengeState(Challenge challenge) : base(challenge)
    {
        NeuronDeliveryChallenge ndc = (NeuronDeliveryChallenge)challenge;
        delivered = ndc.delivered;
        neurons = ndc.neurons;
    }

    public override void ReadTo(ref Challenge challenge)
    {
        base.ReadTo(ref challenge);
        NeuronDeliveryChallenge ndc = (NeuronDeliveryChallenge)challenge;
        ndc.delivered = delivered;
        ndc.neurons = neurons;
    }
    public override string ToString()
    {
        return $"{{{base.ToString()} - delivered: {delivered}, neurons: {neurons}}}";
    }
}
