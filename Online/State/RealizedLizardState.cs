using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class LimbState : OnlineState
    {
        [OnlineFieldHalf]
        private Vector2 pos;
        [OnlineField]
        private Limb.Mode mode;
        [OnlineFieldHalf(nullable = true)]
        private Vector2? huntPos;

        public LimbState() { }
        public LimbState(Limb limb)
        {
            var refPos = limb.connection.pos;
            this.pos = limb.pos - refPos;
            this.mode = limb.mode;
            if (limb.mode == Limb.Mode.HuntAbsolutePosition) this.huntPos = limb.absoluteHuntPos - refPos;
            else if (limb.mode == Limb.Mode.HuntRelativePosition) this.huntPos = limb.relativeHuntPos;
        }

        public void ReadTo(Limb limb)
        {
            var refPos = limb.connection.pos;
            limb.pos = pos + refPos;
            limb.mode = mode;
            if (limb.mode == Limb.Mode.HuntAbsolutePosition) limb.absoluteHuntPos = this.huntPos.Value + refPos;
            else if (limb.mode == Limb.Mode.HuntRelativePosition) limb.relativeHuntPos = this.huntPos.Value;
        }
    }

    public class LizLimbState : LimbState
    {
        [OnlineField]
        private bool reachingForTerrain;

        public LizLimbState() { }
        public LizLimbState(LizardLimb lizLimb) : base(lizLimb)
        {
            this.reachingForTerrain = lizLimb.reachingForTerrain;
        }
    }


    public class RealizedLizardState : RealizedPhysicalObjectState
    {
        [OnlineFieldHalf(nullable = true)]
        private Vector2? gripPoint;
        [OnlineField(nullable = true)]
        private Generics.DynamicOrderedStates<LizLimbState> limbState;

        public RealizedLizardState() { }
        public RealizedLizardState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var liz = onlineEntity.apo.realizedObject as Lizard;
            var mainpos = liz.mainBodyChunk.pos;
            if (liz.gripPoint.HasValue) gripPoint = liz.gripPoint - mainpos; // relative to keep accuracy as half

            if (liz.graphicsModule is LizardGraphics lg)
            {
                this.limbState = new(lg.limbs.Select(l => new LizLimbState(l)).ToList());
            }
            else
            {
                this.limbState = new(new());
            }
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if ((onlineEntity as OnlinePhysicalObject).apo.realizedObject is Lizard liz)
            {
                var mainpos = liz.mainBodyChunk.pos;
                if (gripPoint.HasValue)
                {
                    liz.gripPoint = gripPoint.Value + mainpos;
                }
                else
                {
                    liz.gripPoint = null;
                }

                if (liz.graphicsModule is LizardGraphics lg)
                {
                    for (int i = 0; i < limbState.list.Count; i++)
                    {
                        limbState.list[i].ReadTo(lg.limbs[i]);
                    }
                }
            }
        }
    }
}