using static RainMeadow.Serializer;

namespace RainMeadow
{
    public class TickReference : ICustomSerializable
    {
        public ushort fromPlayer;
        public uint tick;

        public TickReference() { }
        public TickReference(OnlinePlayer fromPlayer, uint tick)
        {
            this.fromPlayer = fromPlayer.inLobbyId;
            this.tick = tick;
        }

        public TickReference(OnlinePlayer player)
        {
            this.fromPlayer = player.inLobbyId;
            this.tick = player.tick;
        }

        public float TimeSinceTick()
        {
            var player = OnlineManager.lobby.PlayerFromId(fromPlayer);
            if (player == null) { RainMeadow.Error("Player not found: " + fromPlayer); return 0; }
            return (player.tick - tick) / (float)OnlineManager.instance.framesPerSecond;
        }

        public void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref fromPlayer);
            serializer.Serialize(ref tick);
        }
    }
}