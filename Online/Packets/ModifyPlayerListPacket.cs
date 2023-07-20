using System.IO;
using System.Net;
using System.Linq;
using Steamworks;

namespace RainMeadow
{
    public class ModifyPlayerListPacket : Packet
    {
        public override Type type => Packet.Type.ModifyPlayerList;

        public enum Operation : byte
        {
            Add,
            Remove,
        }
        Operation modifyOperation;
        OnlinePlayer[] players;

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
                var player = players[i].id as LocalLobbyManager.LocalPlayerId;
                writer.Write(player.id);

                if (modifyOperation != Operation.Add) continue;

                writer.Write((ushort)player.endPoint.Port);
            }
        }

        public override void Deserialize(BinaryReader reader)
        {
            modifyOperation = (Operation)reader.ReadByte();
            players = new OnlinePlayer[reader.ReadInt32()];
            for (int i = 0; i < players.Length; i++)
            {
                int netId = reader.ReadInt32();

                switch (modifyOperation)
                {
                    case Operation.Add:
                        var endPoint = new IPEndPoint(IPAddress.Loopback, (int)reader.ReadUInt16());
                        players[i] = (LobbyManager.instance as LocalLobbyManager).GetPlayerLocal(netId)
                            ?? new OnlinePlayer(new LocalLobbyManager.LocalPlayerId(netId, endPoint, endPoint.Port == UdpPeer.STARTING_PORT));
                        break;


                    case Operation.Remove:
                        players[i] = (LobbyManager.instance as LocalLobbyManager).GetPlayerLocal(netId);
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
                        (LobbyManager.instance as LocalLobbyManager).LocalPlayerJoined(players[i]);
                    }
                    break;

                case Operation.Remove:
                    RainMeadow.Debug("Removing players...\n\t" + string.Join<OnlinePlayer>("\n\t", players));
                    for (int i = 0; i < players.Length; i++)
                    {
                        (LobbyManager.instance as LocalLobbyManager).LocalPlayerLeft(players[i]);
                    }
                    break;
            }

        }
    }
}