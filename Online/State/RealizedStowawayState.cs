using MoreSlugcats;
using UnityEngine;

namespace RainMeadow
{
    public class RealizedStowawayState : RealizedCreatureState
    {
        [OnlineField]
        bool mawOpen;
        [OnlineFieldHalf]
        Vector2 originalPos;
        [OnlineFieldHalf]
        Vector2 placedDirection;
        [OnlineFieldHalf]
        Vector2 currentDirection;
        [OnlineFieldHalf]
        Vector2 goalDirection;
        [OnlineFieldHalf]
        float sleepScale;
        [OnlineField(group = "counters")]
        int spitCooldown;
        [OnlineField(group = "counters")]
        int huntDelay;
        public RealizedStowawayState() { }

        public RealizedStowawayState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            StowawayBug stowaway = (StowawayBug)onlineEntity.realizedCreature;

            mawOpen = stowaway.mawOpen;

            originalPos = stowaway.originalPos;

            placedDirection = stowaway.placedDirection;
            currentDirection = stowaway.currentDirection;
            goalDirection = stowaway.goalDirection;

            sleepScale = stowaway.sleepScale;

            spitCooldown = stowaway.spitCooldown;
            huntDelay = stowaway.huntDelay;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if ((onlineEntity as OnlineCreature).apo.realizedObject is not StowawayBug stowaway) { RainMeadow.Error("target not realized: " + onlineEntity); return; }

            stowaway.mawOpen = mawOpen;

            stowaway.originalPos = originalPos;

            stowaway.placedDirection = placedDirection;
            stowaway.currentDirection = currentDirection;
            stowaway.goalDirection = goalDirection;

            stowaway.sleepScale = sleepScale;

            stowaway.spitCooldown = spitCooldown;
            stowaway.huntDelay = huntDelay;
        }
    }
}
