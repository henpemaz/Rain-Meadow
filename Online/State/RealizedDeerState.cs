using UnityEngine;

namespace RainMeadow
{
    public class RealizedDeerState : RealizedCreatureState
    {
        [OnlineFieldHalf]
        public Vector2 moveDirection;
        [OnlineFieldHalf]
        public float flipDir;
        [OnlineFieldHalf]
        public float resting;

        public RealizedDeerState() { }
        public RealizedDeerState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            Deer deer = (Deer)onlineEntity.realizedCreature;
            moveDirection = deer.moveDirection;
            flipDir = deer.flipDir;
            resting = deer.resting;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if ((onlineEntity as OnlineCreature).apo.realizedObject is not Deer deer) { RainMeadow.Error("target not realized: " + onlineEntity); return; }

            deer.moveDirection = moveDirection;
            deer.flipDir = flipDir;
            deer.resting = resting;
        }
    }
}

