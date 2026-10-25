using Expedition;
using System;

namespace RainMeadow;

public class AchievementChallengeState : ChallengeState
{
    [OnlineField(nullable = true)]
    string? ID;

    public override Type ChallengeType => typeof(AchievementChallenge);

    public AchievementChallengeState() { }
    public AchievementChallengeState(Challenge challenge) : base(challenge)
    {
        var achievementChallenge = (AchievementChallenge)challenge;
        ID = achievementChallenge.ID.value;
    }

    public override void ReadTo(ref Challenge challenge)
    {
        base.ReadTo(ref challenge);
        var achievementChallenge = (AchievementChallenge)challenge;
        achievementChallenge.ID = (WinState.EndgameID)ExtEnumBase.Parse(typeof(WinState.EndgameID), ID, false);

    }

    public override string ToString()
    {
        return $"{{{base.ToString()} - ID: {ID}}}";
    }
}
