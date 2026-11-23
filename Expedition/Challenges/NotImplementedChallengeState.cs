using Expedition;
using System;
using System.Text.RegularExpressions;

namespace RainMeadow;

public class NotImplementedChallengeState : ChallengeState
{
    [OnlineField(nullable = true)]
    string type;
    [OnlineField(nullable = true)]
    string challengeString;

    public override Type ChallengeType => typeof(AchievementChallenge); // putting anything

    public NotImplementedChallengeState() { }
    public NotImplementedChallengeState(Challenge challenge) : base(challenge)
    {
        type = challenge.GetType().Name;
        challengeString = Regex.Split(challenge.ToString(), "~")[1];
    }

    public override void ReadTo(ref Challenge challenge)
    {
        if (challenge.GetType().Name != type)
        {
            challenge = (Challenge)Activator.CreateInstance(ChallengeOrganizer.availableChallengeTypes.Find((Challenge c) => c.GetType().Name == type).GetType());
            challenge.FromString(challengeString);
        }

        if (Regex.Split(challenge.ToString(), "~")[1] != challengeString)
        {
            challenge.FromString(challengeString);
        }
    }

    public override string ToString()
    {
        return $"{type}~{challengeString}";
    }
}
