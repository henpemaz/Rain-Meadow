using Expedition;
using System;

namespace RainMeadow;

public class AchievementChallengeState : ChallengeState
{
    [OnlineField(nullable = true)]
    string? ID;

    public override Type ChallengeType => typeof(AchievementChallenge);
    public override Challenge GetChallenge => new AchievementChallenge();

    public AchievementChallengeState() { }
    public AchievementChallengeState(Challenge challenge) : base(challenge)
    {
        var achievementChallenge = (AchievementChallenge)challenge;
        ID = achievementChallenge.ID.value;
    }

    public override void ReadTo(Challenge challenge)
    {
        base.ReadTo(challenge);
        var achievementChallenge = (AchievementChallenge)challenge;
        achievementChallenge.ID = (WinState.EndgameID)ExtEnumBase.Parse(typeof(WinState.EndgameID), ID, false);

    }

    public override string ToString()
    {
        return $"{{{base.ToString()}:  data - ID: {ID}}}";
    }
}
