using System.IO;
using System.Net;

namespace RainMeadow
{
    public class ModifyPlayerListPacket : Packet
    {
        public override Type type => Type.ModifyPlayerList;

        public enum Operation : byte
        {
            Add,
            Remove,
        }

        private Operation modifyOperation;
        private OnlinePlayer[] players;

        public ModifyPlayerListPacket() : base() { }
        public ModifyPlayerListPacket(Operation modifyOperation, OnlinePlayer[] players) : base()
        {
            this.modifyOperation = modifyOperation;
            this.players = players;
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)modifyOperation);
            writer.Write(players.Length);
            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i].id as LocalMatchmakingManager.LocalPlayerId;
                writer.Write(player.endPoint.Address.GetAddressBytes().Length);
                writer.Write(player.endPoint.Address.GetAddressBytes());
                writer.Write(player.endPoint.Port);
            }
        }

        public override void Deserialize(BinaryReader reader)
        {
            modifyOperation = (Operation)reader.ReadByte();
            players = new OnlinePlayer[reader.ReadInt32()];
            for (int i = 0; i < players.Length; i++)
            {
                int address_size = reader.ReadInt32();
                byte[] address = reader.ReadBytes(address_size);
                int port = reader.ReadInt32();
                IPEndPoint endPoint = new IPEndPoint(new IPAddress(address), port);

                switch (modifyOperation)
                {
                    case Operation.Add:
                        //players[i] = (LobbyManager.instance as LocalLobbyManager).GetPlayerLocal(netId)
                        //    ?? new OnlinePlayer(new LocalLobbyManager.LocalPlayerId(netId, endPoint, endPoint.Port == UdpPeer.STARTING_PORT));
                        players[i] = (MatchmakingManager.instances[MatchmakingManager.MatchMaker.Local] as LANMatchmakingManager).GetPlayerLAN(endPoint);
                        if (players[i] == null)
                        {
                            RainMeadow.Debug("Player not found: " + endPoint.ToString());
                            players[i] = new OnlinePlayer(new LocalMatchmakingManager.LocalPlayerId(-1, endPoint, endPoint.Port == UdpPeer.STARTING_PORT));
                        }
                        break;


                    case Operation.Remove:
                        players[i] = (MatchmakingManager.instances[MatchmakingManager.MatchMaker.Local] as LANMatchmakingManager).GetPlayerLAN(endPoint);
                        break;
                }
            }
        }

        public override void Process()
        {
            switch (modifyOperation)
            {
                case Operation.Add:
                    RainMeadow.Debug("Adding players...\n\t" + string.Join<OnlinePlayer>("\n\t", players));
                    for (int i = 0; i < players.Length; i++)
                    {
                        (MatchmakingManager.instances[MatchmakingManager.MatchMaker.Local] as LANMatchmakingManager).AcknoledgeLANPlayer(players[i]);
                    }
                    break;

                case Operation.Remove:
                    RainMeadow.Debug("Removing players...\n\t" + string.Join<OnlinePlayer>("\n\t", players));
                    for (int i = 0; i < players.Length; i++)
                    {
                        (MatchmakingManager.instances[MatchmakingManager.MatchMaker.Local] as LANMatchmakingManager).RemoveLANPlayer(players[i]);
                    }
                    break;
            }

        }
    }
}