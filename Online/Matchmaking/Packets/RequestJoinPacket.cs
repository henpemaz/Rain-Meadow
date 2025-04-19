using System.IO;
using MonoMod.Utils;

namespace RainMeadow
{
    public class RequestJoinPacket : Packet
    {
        public string LanUserName = "";
        public int Index = -1;
        public override Type type => Type.RequestJoin;

        public RequestJoinPacket() {}
        public RequestJoinPacket(string name, int index) {
            LanUserName = name;
            Index = index;
        }

        public override void Process()
        {
            RainMeadow.DebugMe();
            if (OnlineManager.lobby != null && MatchmakingManager.currentDomain == MatchmakingManager.MatchMakingDomain.LAN)
            {
                var matchmaker = (LANMatchmakingManager)MatchmakingManager.instances[MatchmakingManager.MatchMakingDomain.LAN];

                if (LanUserName.Length > 0) 
                {
                    processingPlayer.id.name = LanUserName;
                }
                if (Index > -1)
                {
                    (processingPlayer.id as LANMatchmakingManager.LANPlayerId)!.index = Index;
                }
                // Tell everyone else about them
                RainMeadow.Debug("Telling client they got in.");
                matchmaker.AcknoledgeLANPlayer(processingPlayer);

                // Tell them they are in
                OnlineManager.netIO.SendP2P(processingPlayer, new JoinLobbyPacket(
                    matchmaker.maxplayercount,
                    "LAN Lobby",
                    OnlineManager.lobby.hasPassword,
                    OnlineManager.lobby.gameModeType.value,
                    OnlineManager.players.Count,
                    RainMeadowModManager.ModArrayToString(RainMeadowModManager.GetRequiredMods()),
                    RainMeadowModManager.ModArrayToString(RainMeadowModManager.GetBannedMods())
                ), NetIO.SendType.Reliable);

            }
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteNullTerminatedString(LanUserName);
            writer.Write(Index);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            LanUserName = reader.ReadNullTerminatedString();
            Index = reader.ReadInt32();
        }
    }
}