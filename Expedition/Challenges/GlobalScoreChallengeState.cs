using Expedition;
using System;

namespace RainMeadow;

public class GlobalScoreChallengeState : ChallengeState
{        
    [OnlineField]
    int score;
    [OnlineField]
    int target;

    public override Type ChallengeType => typeof(GlobalScoreChallenge);
    public GlobalScoreChallengeState() { }
    public GlobalScoreChallengeState(Challenge challenge) : base(challenge)
    {
        GlobalScoreChallenge gsc = (GlobalScoreChallenge)challenge;
        score = gsc.score;
        target = gsc.target;
    }

    public override void ReadTo(ref Challenge challenge)
    {
        base.ReadTo(ref challenge);
        GlobalScoreChallenge gsc = (GlobalScoreChallenge)challenge;
        gsc.score = score;
        gsc.target = target;
    }
    
    public override string ToString()
    {
        return $"{{{base.ToString()} - score: {score}, target: {target}}}";
    }
}
