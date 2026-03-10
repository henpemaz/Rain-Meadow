using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Watcher;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class RealizedMothState : RealizedCreatureState
    {
        [OnlineFieldHalf]
        Vector2 moveDirection;
        [OnlineFieldHalf]
        float flipness;
        [OnlineFieldHalf]
        float stance;

        [OnlineField]
        bool drinkingChunk;
        [OnlineField(nullable = true)]
        BodyChunkRef? drinkChunk;

        [OnlineField(group = "counters")]
        int wantToFlyCounter;

        [OnlineField]
        MothLegState[] legState;
        [OnlineField]
        MothWingState[] wingState;

        public RealizedMothState() { }

        public RealizedMothState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            BigMoth bigMoth = (BigMoth)onlineEntity.realizedCreature;

            moveDirection = bigMoth.moveDirection;
            flipness = bigMoth.flipness;
            stance = bigMoth.stance;

            wantToFlyCounter = bigMoth.wantToFlyCounter;
            drinkingChunk = bigMoth.drinkingChunk;
            drinkChunk = BodyChunkRef.FromBodyChunk(bigMoth.drinkChunk) ?? null;

            if (bigMoth.dead)
            {
                legState = [];
                wingState = [];
                return;
            }

            // We won't track legs for small moths as they hardly do anything other than
            // mildly annoy the player which we'll just let the clients handle
            // and a bunch of them at once consume a ton of bandwidth.

            if (!bigMoth.Small)
            {

                var legs = new List<BigMoth.MothLeg>();

                for (int i = 0; i < bigMoth.legs.GetLength(0); i++)
                {
                    for (int j = 0; j < bigMoth.legs.GetLength(1); j++)
                    {
                        legs.Add(bigMoth.legs[i, j]);
                    }
                }

                legState = legs.Select(l => new MothLegState(l)).ToArray();
            }
            else
            {
                legState = [];
            }
            wingState = bigMoth.wingPairs.Select(w => new MothWingState(w)).ToArray();
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            if ((onlineEntity as OnlinePhysicalObject).apo.realizedObject is not BigMoth bigMoth) return;

            bigMoth.moveDirection = moveDirection;
            bigMoth.flipness = flipness;
            bigMoth.stance = stance;

            bigMoth.drinkingChunk = drinkingChunk;
            bigMoth.wantToFlyCounter = wantToFlyCounter;

            bigMoth.drinkChunk = drinkChunk?.ToBodyChunk();

            if (legState.Length > 0 && legState.Length == 4)
            {
                for (int i = 0; i < legState.Length; i++)
                {
                    legState[i].ReadTo(bigMoth.legs[i / 2, i % 2]);
                }
            }
            if (wingState.Length > 0 && wingState.Length == 2)
            {
                for (int i = 0; i < wingState.Length; i++)
                {
                    wingState[i].ReadTo(bigMoth.wingPairs[i]);
                }
            }
        }
    }

    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class MothLegState : OnlineState
    {
        [OnlineField(nullable = true)]
        BodyChunkRef? grabbedChunk;

        [OnlineFieldHalf (nullable = true)]
        Vector2? stepPos;
        [OnlineField]
        bool securedFoot;

        public MothLegState() { }

        public MothLegState(BigMoth.MothLeg mothLeg)
        {
            grabbedChunk = BodyChunkRef.FromBodyChunk(mothLeg.grabbedChunk) ?? null;

            stepPos = mothLeg.stepPos;
            securedFoot = mothLeg.securedFoot;
        }

        public void ReadTo(BigMoth.MothLeg mothLeg)
        {
            mothLeg.grabbedChunk = grabbedChunk?.ToBodyChunk();

            mothLeg.stepPos = stepPos;
            mothLeg.securedFoot = securedFoot;
        }
    }

    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class MothWingState : OnlineState
    {
        [OnlineField]
        byte mode;
        [OnlineFieldHalf]
        float flapness;


        public MothWingState() { }

        public MothWingState(BigMoth.MothWingPair mothWing)
        {
            mode = Mode(mothWing.mode);
            flapness = mothWing.flapness;
        }

        public void ReadTo(BigMoth.MothWingPair mothWing)
        {
            mothWing.mode = Mode(mode);
            mothWing.flapness = flapness;
        }

        private byte Mode(BigMoth.MothWingPair.Mode mode)
        {
            if (mode == BigMoth.MothWingPair.Mode.Glide) return 1;
            if (mode == BigMoth.MothWingPair.Mode.ReadyFlap) return 2;
            if (mode == BigMoth.MothWingPair.Mode.Flap) return 3;
            return 0;
        }

        private BigMoth.MothWingPair.Mode Mode(byte mode)
        {
            switch(mode)
            {
                default: return null;
                case 1: return BigMoth.MothWingPair.Mode.Glide;
                case 2: return BigMoth.MothWingPair.Mode.ReadyFlap;
                case 3: return BigMoth.MothWingPair.Mode.Flap;
            }
        }
    }
}