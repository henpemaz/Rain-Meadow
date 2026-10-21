using Expedition;
using System;

namespace RainMeadow;

public class CycleScoreChallengeState : ChallengeState
{
    [OnlineField]
    int score;
    [OnlineField]
    int target;

    public override Type ChallengeType => typeof(CycleScoreChallenge);
    public override Challenge GetChallenge => new CycleScoreChallenge();

    public CycleScoreChallengeState() { }
    public CycleScoreChallengeState(Challenge challenge) : base(challenge)
    {
        CycleScoreChallenge csc = (CycleScoreChallenge)challenge;
        score = csc.score;
        target = csc.target;
    }

    public override void ReadTo(Challenge challenge)
    {
        base.ReadTo(challenge);
        CycleScoreChallenge csc = (CycleScoreChallenge)challenge;
        csc.score = score;
        csc.target = target;
    }

    public override string ToString()
    {
        return $"{{{base.ToString()}: data - score: { score}, target: { target} }}";
    }
}
