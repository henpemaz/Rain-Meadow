using MoreSlugcats;
using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class RealizedStowawayState : RealizedCreatureState
    {        
        [OnlineField]
        Generics.DynamicOrderedStates<StowawayTentacleState> heads;

        [OnlineField]
        bool mawOpen;
        [OnlineField(group = "setup")]
        bool activeThisCycle;
               
        [OnlineField]
        byte behavior; // heads can be buggy if not synced

        [OnlineFieldHalf(group = "setup")]
        float headLength; // needed cause can somethimes be diferent, and when diferent this makes heads to be buggy        
        
        [OnlineFieldHalf]
        Vector2 currentDirection;

        public RealizedStowawayState() { }

        public RealizedStowawayState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            StowawayBug stowaway = (StowawayBug)onlineEntity.realizedCreature;

            behavior = (byte)stowaway.AI.behavior.index;            
            mawOpen = stowaway.mawOpen;
            currentDirection = stowaway.currentDirection;                        
            headLength = stowaway.headLength;
            activeThisCycle = stowaway.AI.activeThisCycle;

            heads = new(stowaway.heads.Select((t, i) => new StowawayTentacleState(t, i)).ToList());
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            if ((onlineEntity as OnlineCreature).apo.realizedObject is not StowawayBug stowaway) { RainMeadow.Error("target not realized: " + onlineEntity); return; }

            stowaway.AI.behavior = behavior switch
            {
                0 => StowawayBugAI.Behavior.Idle,
                1 => StowawayBugAI.Behavior.EscapeRain,
                2 => StowawayBugAI.Behavior.Hidden,
                3 => StowawayBugAI.Behavior.Attacking,
                4 => StowawayBugAI.Behavior.Digesting,
                5 => StowawayBugAI.Behavior.Sleeping
            };

            stowaway.mawOpen = mawOpen;
            stowaway.currentDirection = currentDirection;            
            stowaway.headLength = headLength;
            stowaway.AI.activeThisCycle = activeThisCycle;


            for (int i = 0; i < stowaway.heads.Length; i++)
            {
                heads.list[i].ReadTo(stowaway.heads[i], i);
            }
        }
    }

    public class StowawayTentacleState : OnlineState
    {
        const int chunksToSync = 3;
        
        [OnlineFieldHalf]
        float retractFac;        
        [OnlineFieldHalf]
        float idealLength;

        [OnlineFieldHalf(nullable = true)]
        Vector2? grabdest;
        [OnlineFieldHalf]
        Vector2[] vel;
        [OnlineFieldHalf]
        Vector2[] pos;

        public StowawayTentacleState() { }

        public StowawayTentacleState(Tentacle tentacle, int index)
        {
            retractFac = tentacle.retractFac;
            
            pos = new Vector2[chunksToSync];
            vel = new Vector2[chunksToSync];

            for (int i = 0; i < chunksToSync; i++)
            {
                pos[i] = tentacle.tChunks[tentacle.tChunks.Length - i - 1].pos;
                vel[i] = tentacle.tChunks[tentacle.tChunks.Length - i - 1].vel;
            }

            idealLength = tentacle.idealLength;
            grabdest = tentacle.floatGrabDest;
        }

        public void ReadTo(Tentacle tentacle, int index)
        {
            tentacle.retractFac = retractFac;

            for (int i = 0; i < chunksToSync; i++)
            {
                tentacle.tChunks[tentacle.tChunks.Length - i - 1].pos = pos[i];
                tentacle.tChunks[tentacle.tChunks.Length - i - 1].vel = vel[i];
            }

            tentacle.idealLength = idealLength;
            tentacle.floatGrabDest = grabdest;

        }
    }
}
