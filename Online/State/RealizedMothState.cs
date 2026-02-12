using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Watcher;

namespace RainMeadow
{
    public class RealizedMothState : RealizedCreatureState
    {
        [OnlineFieldHalf]
        public Vector2 moveDirection;
        [OnlineField]
        bool drinkingChunk;
        [OnlineField(nullable = true)]
        BodyChunkRef? drinkChunk;
        [OnlineField]
        Generics.DynamicOrderedStates<MothLegState> legState;

        public RealizedMothState() { }

        public RealizedMothState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            BigMoth bigMoth = (BigMoth)onlineEntity.realizedCreature;

            moveDirection = bigMoth.moveDirection;
            drinkingChunk = bigMoth.drinkingChunk;
            drinkChunk = BodyChunkRef.FromBodyChunk(bigMoth.drinkChunk) ?? null;

            // We won't track legs for small moths as they hardly do anything other than
            // mildly annoy the player which we'll just let the clients handle
            // and a bunch of them at once consume a ton of bandwidth.

            if (bigMoth.Small)
            {
                legState = new(new List<MothLegState>());
                return;
            }

            var legs = new List<BigMoth.MothLeg>();

            for (int i = 0; i < bigMoth.legs.GetLength(0); i++)
            {
                for (int j = 0; j < bigMoth.legs.GetLength(1); j++)
                {
                    legs.Add(bigMoth.legs[i, j]);
                }
            }

            legState = new(legs.Select(l => new MothLegState(l)).ToList());
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            if ((onlineEntity as OnlinePhysicalObject).apo.realizedObject is not BigMoth bigMoth) return;

            bigMoth.moveDirection = moveDirection;
            bigMoth.drinkingChunk = drinkingChunk;

            if (drinkingChunk)
            {
                bigMoth.drinkChunk = drinkChunk.ToBodyChunk();
            }

            if (legState.list.Count == 0) return; // SmallMoth legs aren't tracked

            for (int i = 0; i < legState.list.Count; i++)
            {
                legState.list[i].ReadTo(bigMoth.legs[i / 2, i % 2]);
            }
        }

    }

    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class MothLegState : OnlineState
    {
        [OnlineField(nullable = true)]
        BodyChunkRef? grabbedChunk;
        [OnlineFieldHalf]
        Vector2 footPos;
        public MothLegState() { }

        public MothLegState(BigMoth.MothLeg mothLeg)
        {
            this.grabbedChunk = BodyChunkRef.FromBodyChunk(mothLeg.grabbedChunk) ?? null;
            this.footPos = mothLeg.footPos;
        }

        public void ReadTo(BigMoth.MothLeg mothLeg)
        {
            mothLeg.grabbedChunk = grabbedChunk?.ToBodyChunk() ?? null;
            mothLeg.footPos = footPos;
        }
    }
}