using Expedition;
using System;
using UnityEngine;

namespace RainMeadow;

public class VistaChallengeState : ChallengeState
{
    [OnlineField]
    Vector2 location;
    [OnlineField(nullable = true)]
    string? region;
    [OnlineField(nullable = true)]
    string? room;

    public override Type ChallengeType => typeof(VistaChallenge);
    public override Challenge GetChallenge => new VistaChallenge();

    public VistaChallengeState() { }
    public VistaChallengeState(Challenge challenge) : base(challenge)
    {
        VistaChallenge vc = (VistaChallenge)challenge;
        location = vc.location;
        region = vc.region;
        room = vc.room;
    }

    public override void ReadTo(Challenge challenge)
    {
        base.ReadTo(challenge);
        VistaChallenge vc = (VistaChallenge)challenge;
        vc.location = location;
        vc.region = region;
        vc.room = room;
    }
    public override string ToString()
    {
        return $"{{{base.ToString()}: data - location: {location}, region: {region}, room: {room}}}";
    }
}
