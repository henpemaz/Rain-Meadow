using Expedition;
using System;

namespace RainMeadow;

public class EchoChallengeState : ChallengeState
{
    [OnlineField(nullable = true)]
    string? ghost;

    public override Type ChallengeType => typeof(EchoChallenge);
    public override Challenge GetChallenge => new EchoChallenge();

    public EchoChallengeState() { }
    public EchoChallengeState(Challenge challenge) : base(challenge)
    {
        EchoChallenge ec = (EchoChallenge)challenge;
        ghost = ec.ghost.value;
    }

    public override void ReadTo(Challenge challenge)
    {
        base.ReadTo(challenge);
        EchoChallenge ec = (EchoChallenge)challenge;
        ec.ghost = (GhostWorldPresence.GhostID)ExtEnumBase.Parse(typeof(GhostWorldPresence.GhostID), ghost, false);
    }
    public override string ToString()
    {
        return $"{{{base.ToString()}: data - ghost: {ghost}}}";
    }
}
