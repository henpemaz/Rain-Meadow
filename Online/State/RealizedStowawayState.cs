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
        bool[] headsfired;// = new bool[3];

        [OnlineField]
        bool mawOpen;
       
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
        [OnlineField]
        byte behavior;

        [OnlineField]
        float digestPrey;

        [OnlineField]
        float biting;

        public RealizedStowawayState() { }

        public RealizedStowawayState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            StowawayBug stowaway = (StowawayBug)onlineEntity.realizedCreature;

            behavior = (byte)stowaway.AI.behavior.index;
            headsfired = stowaway.headFired;
            mawOpen = stowaway.mawOpen;
            //originalPos = stowaway.originalPos;
            //placedDirection = stowaway.placedDirection;
            currentDirection = stowaway.currentDirection;
            headCooldown = stowaway.headCooldown;
            sleepScale = stowaway.sleepScale;
            spitCooldown = stowaway.spitCooldown;
            huntDelay = stowaway.huntDelay;

            if (stowaway.graphicsModule is not null)
            {
                StowawayBugGraphics stowawayGraphics = (stowaway.graphicsModule as MoreSlugcats.StowawayBugGraphics);
                biting = stowawayGraphics.biting;
                digestPrey = stowawayGraphics.digestPrey;
            }

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

            stowaway.headFired = headsfired;
            stowaway.mawOpen = mawOpen;            
            stowaway.currentDirection = currentDirection;
            stowaway.headCooldown = headCooldown;
            stowaway.sleepScale = sleepScale;
            stowaway.spitCooldown = spitCooldown;
            stowaway.huntDelay = huntDelay;

            if (stowaway.graphicsModule is not null)
            {
                StowawayBugGraphics stowawayGraphics = (stowaway.graphicsModule as MoreSlugcats.StowawayBugGraphics);

                if (stowawayGraphics.biting < .01f && stowawayGraphics.biting < biting)
                {                 
                    for (int n = UnityEngine.Random.Range(1, 5); n > 0; n--)
                    {
                        stowaway.room.AddObject(new WaterDrip(stowaway.bodyChunks[1].pos, RWCustom.Custom.DirVec(stowaway.firstChunk.pos, stowaway.bodyChunks[1].pos) * 10f + RWCustom.Custom.RNV(), true));
                    }
                    stowaway.room.PlaySound(SoundID.Lizard_Jaws_Shut_Miss_Creature, stowaway.firstChunk);
                }
                stowawayGraphics.biting = biting;

                if (stowawayGraphics.digestPrey < 0.01f && stowawayGraphics.digestPrey < digestPrey)
                {            
                    for (int i = UnityEngine.Random.Range(4, 8); i > 0; i--)
                    {
                        stowaway.room.AddObject(new WaterDrip(stowaway.bodyChunks[1].pos, default(UnityEngine.Vector2) + RWCustom.Custom.RNV(), true));
                    }                    
                    stowaway.LoseAllGrasps();                    
                    stowaway.room.PlaySound(SoundID.Bro_Digestion_Init, stowaway.firstChunk);
                    stowaway.room.PlaySound(SoundID.Lizard_Jaws_Grab_Player, stowaway.firstChunk);
                }
                stowawayGraphics.digestPrey = digestPrey;
            }

            for (int i = 0; i < stowaway.heads.Length; i++)
            {
                heads.list[i].ReadTo(stowaway.heads[i], i);
            }
        }
        protected override ArtificialIntelligenceState? GetCreatureAIState(OnlineCreature onlineCreature)
        {
            return new ArtificialIntelligenceState(onlineCreature.abstractCreature.abstractAI.RealAI);
        }
    }

    public class StowawayTentacleState : OnlineState
    {
        [OnlineFieldHalf]
        float retractFac;
        [OnlineField]
        bool fired;
        [OnlineFieldHalf(group = "counters")]
        float hcooldown;
        [OnlineField(group = "counters")]
        int scooldown;
        [OnlineFieldHalf]
        Vector2[] vel;
        [OnlineFieldHalf]
        Vector2[] pos;

        [OnlineField(nullable = true)]
        Vector2? grabdest;
        [OnlineFieldHalf]
        float idealLength;

        int chunksToSync = 3;

        public StowawayTentacleState() { }

        public StowawayTentacleState(Tentacle tentacle, int index)
        {
            StowawayBug owner = (StowawayBug)tentacle.owner;

            retractFac = tentacle.retractFac;
            fired = owner.headFired[index];
            hcooldown = owner.headCooldown[index];
            scooldown = owner.spitCooldown;

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
            StowawayBug owner = (StowawayBug)tentacle.owner;

            tentacle.retractFac = retractFac;

            owner.headFired[index] = fired;
            owner.headCooldown[index] = hcooldown;
            owner.spitCooldown = scooldown;

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
