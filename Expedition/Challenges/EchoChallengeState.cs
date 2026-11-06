using Expedition;
using System;

namespace RainMeadow;

public class EchoChallengeState : ChallengeState
{
    [OnlineField(nullable = true)]
    string? ghost;

    public override Type ChallengeType => typeof(EchoChallenge);
    public EchoChallengeState() { }
    public EchoChallengeState(Challenge challenge) : base(challenge)
    {
        EchoChallenge ec = (EchoChallenge)challenge;
        ghost = ec.ghost.value;
    }

    public override void ReadTo(ref Challenge challenge)
    {
        base.ReadTo(ref challenge);
        EchoChallenge ec = (EchoChallenge)challenge;
        ec.ghost = (GhostWorldPresence.GhostID)ExtEnumBase.Parse(typeof(GhostWorldPresence.GhostID), ghost, false);
    }
    public override string ToString()
    {
        return $"{{{base.ToString()} : ghost: {ghost}}}";
    }
}
