using UnityEngine;

namespace RainMeadow
{
    public class RealizedPlayerState : RealizedCreatureState
    {
        static int lastupdate = 0;
        private byte animationIndex;
        private short animationFrame;
        private byte bodyModeIndex;
        private bool standing;
        private ushort inputs;
        private Vector2 analogInput;
        public RealizedPlayerState() { }
        public RealizedPlayerState(OnlineEntity onlineEntity) : base(onlineEntity)
        {
            lastupdate++;
            
            Player p = onlineEntity.entity.realizedObject as Player;
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

            analogInput = i.analogueDir;
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
            i.analogueDir = analogInput;
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

            ushort half = 0;
            if (serializer.isWriting) {
                half = Mathf.FloatToHalf(analogInput.x);
                serializer.Serialize(ref half);

                half = Mathf.FloatToHalf(analogInput.y);
                serializer.Serialize(ref half);
            }

            if (serializer.isReading) {
                serializer.Serialize(ref half);
                analogInput.x = Mathf.HalfToFloat(half);

                serializer.Serialize(ref half);
                analogInput.y = Mathf.HalfToFloat(half);
            }
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if (onlineEntity.entity.realizedObject is Player pl)
            {
                pl.animation = new Player.AnimationIndex(Player.AnimationIndex.values.GetEntry(animationIndex));
                pl.animationFrame = animationFrame;
                pl.bodyMode = new Player.BodyModeIndex(Player.BodyModeIndex.values.GetEntry(bodyModeIndex));
                pl.standing = standing;
            }
        }
    }
}
