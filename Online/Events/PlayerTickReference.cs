using System;

namespace RainMeadow
{
    public class PlayerTickReference
    {
        internal OnlinePlayer fromPlayer;
        internal ulong tick;

        public PlayerTickReference() { }
        public PlayerTickReference(OnlinePlayer fromPlayer, ulong tick)
        {
            this.fromPlayer = fromPlayer;
            this.tick = tick;
        }

        internal bool ChecksOut()
        {
            return OnlineManager.IsNewerOrEqual(fromPlayer.tick, tick);
        }

        internal void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref fromPlayer);
            serializer.Serialize(ref tick);
        }
    }
}