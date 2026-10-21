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
    public override Challenge GetChallenge => new NeuronDeliveryChallenge();

    public NeuronDeliveryChallengeState() { }
    public NeuronDeliveryChallengeState(Challenge challenge) : base(challenge)
    {
        NeuronDeliveryChallenge ndc = (NeuronDeliveryChallenge)challenge;
        delivered = ndc.delivered;
        neurons = ndc.neurons;
    }

    public override void ReadTo(Challenge challenge)
    {
        base.ReadTo(challenge);
        NeuronDeliveryChallenge ndc = (NeuronDeliveryChallenge)challenge;
        ndc.delivered = delivered;
        ndc.neurons = neurons;
    }
    public override string ToString()
    {
        return $"{{{base.ToString()}: data - delivered: {delivered}, neurons: {neurons}}}";
    }
}
