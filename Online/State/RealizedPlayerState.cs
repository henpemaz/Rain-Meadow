﻿using UnityEngine;

namespace RainMeadow
{
    public class RealizedPlayerState : RealizedCreatureState
    {
        private byte animationIndex;
        private short animationFrame;
        private byte bodyModeIndex;
        private bool standing;
        private ushort inputs;
        private ushort analogInputX;
        private ushort analogInputY;

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

            analogInputX = Mathf.FloatToHalf(i.analogueDir.x);
            analogInputY = Mathf.FloatToHalf(i.analogueDir.y);
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
            i.analogueDir.x = Mathf.HalfToFloat(analogInputX);
            i.analogueDir.x = Mathf.HalfToFloat(analogInputY);
            return i;
        }

        public override StateType stateType => StateType.RealizedPlayerState;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref animationIndex);
            serializer.Serialize(ref animationFrame);
            serializer.Serialize(ref bodyModeIndex);
            serializer.Serialize(ref standing);
            serializer.Serialize(ref inputs);
            serializer.Serialize(ref analogInputX);
            serializer.Serialize(ref analogInputY);
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if ((onlineEntity as OnlineCreature).apo.realizedObject is Player pl)
            {
                pl.animation = new Player.AnimationIndex(Player.AnimationIndex.values.GetEntry(animationIndex));
                pl.animationFrame = animationFrame;
                pl.bodyMode = new Player.BodyModeIndex(Player.BodyModeIndex.values.GetEntry(bodyModeIndex));
                pl.standing = standing;
            }
        }
    }
}
