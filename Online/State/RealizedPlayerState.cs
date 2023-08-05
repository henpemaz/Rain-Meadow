using UnityEngine;

namespace RainMeadow
{
    public class RealizedPlayerState : RealizedCreatureState
    {
        private byte animationIndex;
        private short animationFrame;
        private byte bodyModeIndex;
        private bool standing;
        private ushort inputs;
        private float analogInputX;
        private float analogInputY;

        bool hasAnimationValue;
        bool hasInputValue;

        public override RealizedPhysicalObjectState EmptyDelta() => new RealizedPlayerState();
        public RealizedPlayerState() { }
        public RealizedPlayerState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            Player p = onlineEntity.apo.realizedObject as Player;
            animationIndex = (byte)p.animation.Index;
            animationFrame = (short)p.animationFrame;
            bodyModeIndex = (byte)p.bodyMode.Index;
            standing = p.standing;

            var i = p.input[0];
            inputs = (ushort)(
                  (i.x == 1 ? 1 << 0 : 0)
                | (i.x == -1 ? 1 << 1 : 0)
                | (i.y == 1 ? 1 << 2 : 0)
                | (i.y == -1 ? 1 << 3 : 0)
                | (i.downDiagonal == 1 ? 1 << 4 : 0)
                | (i.downDiagonal == -1 ? 1 << 5 : 0)
                | (i.pckp ? 1 << 6 : 0)
                | (i.jmp ? 1 << 7 : 0)
                | (i.thrw ? 1 << 8 : 0)
                | (i.mp ? 1 << 9 : 0));

            analogInputX = i.analogueDir.x;
            analogInputY = i.analogueDir.y;
        }
        public Player.InputPackage GetInput()
        {
            Player.InputPackage i = default;
            if (((inputs >> 0) & 1) != 0) i.x = 1;
            if (((inputs >> 1) & 1) != 0) i.x = -1;
            if (((inputs >> 2) & 1) != 0) i.y = 1;
            if (((inputs >> 3) & 1) != 0) i.y = -1;
            if (((inputs >> 4) & 1) != 0) i.downDiagonal = 1;
            if (((inputs >> 5) & 1) != 0) i.downDiagonal = -1;
            if (((inputs >> 6) & 1) != 0) i.pckp = true;
            if (((inputs >> 7) & 1) != 0) i.jmp = true;
            if (((inputs >> 8) & 1) != 0) i.thrw = true;
            if (((inputs >> 9) & 1) != 0) i.mp = true;
            i.analogueDir.x = analogInputX;
            i.analogueDir.y = analogInputY;
            return i;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if ((onlineEntity as OnlineCreature).apo.realizedObject is Player pl)
            {
                var wasAnimation = pl.animation;
                pl.animation = new Player.AnimationIndex(Player.AnimationIndex.values.GetEntry(animationIndex));
                if (wasAnimation != pl.animation) pl.animationFrame = animationFrame;
                pl.bodyMode = new Player.BodyModeIndex(Player.BodyModeIndex.values.GetEntry(bodyModeIndex));
                pl.standing = standing;
            }
        }

        public override StateType stateType => StateType.RealizedPlayerState;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            if (IsDelta) serializer.Serialize(ref hasAnimationValue);
            if (!IsDelta || hasAnimationValue)
            {
                serializer.Serialize(ref animationIndex);
                serializer.Serialize(ref animationFrame);
                serializer.Serialize(ref bodyModeIndex);
                serializer.Serialize(ref standing);
            }
            if (IsDelta) serializer.Serialize(ref hasInputValue);
            if (!IsDelta || hasInputValue)
            {
                serializer.Serialize(ref inputs);
                serializer.SerializeHalf(ref analogInputX);
                serializer.SerializeHalf(ref analogInputY);
            }
        }

        public override long EstimatedSize(bool inDeltaContext)
        {
            var val = base.EstimatedSize(inDeltaContext);
            if (IsDelta) val += 2;
            if (!IsDelta || hasAnimationValue)
            {
                val += 5;
            }
            if (!IsDelta || hasInputValue)
            {
                val += 6;
            }
            return val;
        }

        public override RealizedPhysicalObjectState Delta(RealizedPhysicalObjectState _other)
        {
            var other = (RealizedPlayerState)_other;
            var delta = (RealizedPlayerState)base.Delta(_other);
            delta.animationIndex = animationIndex;
            delta.animationFrame = animationFrame;
            delta.bodyModeIndex = bodyModeIndex;
            delta.standing = standing;
            delta.hasAnimationValue = animationIndex != other.animationIndex || bodyModeIndex != other.bodyModeIndex || standing != other.standing;
            delta.inputs = inputs;
            delta.analogInputX = analogInputX;
            delta.analogInputY = analogInputY;
            delta.hasInputValue = inputs != other.inputs || analogInputX != other.analogInputX || analogInputY != other.analogInputY;
            delta.IsEmptyDelta &= !delta.hasAnimationValue && !delta.hasInputValue;
            return delta;
        }

        public override RealizedPhysicalObjectState ApplyDelta(RealizedPhysicalObjectState _other)
        {
            var other = (RealizedPlayerState)_other;
            var result = (RealizedPlayerState)base.ApplyDelta(_other);
            if (other.hasAnimationValue)
            {
                result.animationIndex = other.animationIndex;
                result.animationFrame = other.animationFrame;
                result.bodyModeIndex = other.bodyModeIndex;
                result.standing = other.standing;
            }
            else
            {
                result.animationIndex = animationIndex;
                result.animationFrame = animationFrame;
                result.bodyModeIndex = bodyModeIndex;
                result.standing = standing;
            }
            if (other.hasInputValue)
            {
                result.inputs = other.inputs;
                result.analogInputX = other.analogInputX;
                result.analogInputY = other.analogInputY;
            }
            else
            {
                result.inputs = inputs;
                result.analogInputX = analogInputX;
                result.analogInputY = analogInputY;
            }
            return result;
        }
    }
}
