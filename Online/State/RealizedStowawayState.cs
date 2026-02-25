using MoreSlugcats;
using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class RealizedStowawayState : RealizedCreatureState
    {
        // Todo figure out whats needed and what isn't
        // TODO sync tentacle firing correctly so it fires at the same time for everyone
        [OnlineField]
        Generics.DynamicOrderedStates<StowawayTentacleState> heads;

        [OnlineField]
        bool mawOpen;
        //[OnlineFieldHalf]
        //Vector2 originalPos;
        //[OnlineFieldHalf]
        //Vector2 placedDirection;
        [OnlineFieldHalf]
        Vector2 currentDirection;
        [OnlineFieldHalf]
        float sleepScale;
        [OnlineFieldHalf]
        float[] headCooldown;
        [OnlineField(group = "counters")]
        int spitCooldown;
        [OnlineField(group = "counters")]
        int huntDelay;
        public RealizedStowawayState() { }

        public RealizedStowawayState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            StowawayBug stowaway = (StowawayBug)onlineEntity.realizedCreature;

            mawOpen = stowaway.mawOpen;

            //originalPos = stowaway.originalPos;

            //placedDirection = stowaway.placedDirection;
            currentDirection = stowaway.currentDirection;

            headCooldown = stowaway.headCooldown;

            sleepScale = stowaway.sleepScale;

            spitCooldown = stowaway.spitCooldown;
            huntDelay = stowaway.huntDelay;

            heads = new(stowaway.heads.Select((t, i) => new StowawayTentacleState(t, i)).ToList());
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if ((onlineEntity as OnlineCreature).apo.realizedObject is not StowawayBug stowaway) { RainMeadow.Error("target not realized: " + onlineEntity); return; }

            stowaway.mawOpen = mawOpen;

            //stowaway.originalPos = originalPos;

            //stowaway.placedDirection = placedDirection;
            stowaway.currentDirection = currentDirection;

            stowaway.headCooldown = headCooldown;

            stowaway.sleepScale = sleepScale;

            stowaway.spitCooldown = spitCooldown;
            stowaway.huntDelay = huntDelay;

            for(int i = 0; i < stowaway.heads.Length; i++)
            {
                heads.list[i].ReadTo(stowaway.heads[i], i);
            }
        }

        //public override bool ShouldPosBeLenient(PhysicalObject po)
        //{
        //    return true;
        //}
    }

    public class StowawayTentacleState : OnlineState
    {
        [OnlineFieldHalf]
        float retractFac;
        [OnlineField]
        bool fired;
        [OnlineFieldHalf(group = "counters")]
        float cooldown;
        [OnlineFieldHalf]
        Vector2 pos;
        public StowawayTentacleState() { }

        public StowawayTentacleState(Tentacle tentacle, int index)
        {
            StowawayBug owner = (StowawayBug)tentacle.owner;

            retractFac = tentacle.retractFac;

            fired = owner.headFired[index];
            cooldown = owner.headCooldown[index];
            pos = tentacle.Tip.pos;
            //floatGrabDest = tentacle.floatGrabDest;
        }

        public void ReadTo(Tentacle tentacle, int index)
        {
            StowawayBug owner = (StowawayBug)tentacle.owner;

            tentacle.retractFac = retractFac;

            owner.headFired[index] = fired;
            owner.headCooldown[index] = cooldown;

            tentacle.Tip.pos = pos;
            //tentacle.floatGrabDest = floatGrabDest;
        }
    }
}
