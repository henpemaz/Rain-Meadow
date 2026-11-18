using Expedition;
using System;

namespace RainMeadow;

public class ItemHoardChallengeState : ChallengeState
{
    [OnlineField]
    int amount;
    [OnlineField(nullable = true)]
    string? target;

    public override Type ChallengeType => typeof(ItemHoardChallenge);

    public ItemHoardChallengeState() { }
    public ItemHoardChallengeState(Challenge challenge) : base(challenge)
    {
        ItemHoardChallenge ihc = (ItemHoardChallenge)challenge;
        amount = ihc.amount;
        target = ihc.target.value;
    }

    public override void ReadTo(ref Challenge challenge)
    {
        base.ReadTo(ref challenge);
        ItemHoardChallenge ihc = (ItemHoardChallenge)challenge;
        ihc.amount = amount;
        ihc.target = (AbstractPhysicalObject.AbstractObjectType)ExtEnumBase.Parse(typeof(AbstractPhysicalObject.AbstractObjectType), target, false);
    }
    public override string ToString()
    {
        return $"{{{base.ToString()} - amount: {amount}, target: {target}}}";
    }
}
