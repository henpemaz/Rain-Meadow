using Steamworks;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using static RainMeadow.NetIO;

namespace RainMeadow
{
    public class LocalLobbyManager : LobbyManager
    {
        public class LocalPlayerId : MeadowPlayerId
        {
            public int id;
            public IPEndPoint endPoint;
            public bool isHost;

            public LocalPlayerId(int id, IPEndPoint endPoint, bool isHost) : base($"local:{id}")
            {
                this.id = id;
                this.endPoint = endPoint;
                this.isHost = isHost;
            }

            public override bool Equals(MeadowPlayerId other)
            {
                return other is LocalPlayerId otherL && otherL.id == id;
            }

            public override int GetHashCode()
            {
                return id;
            }
        }

        int me;
        string localGameMode = "Story";

        public LocalLobbyManager()
        {
            RainMeadow.DebugMe();
            UdpPeer.Startup();
            me = UdpPeer.port;
            mePlayer = new OnlinePlayer(new LocalPlayerId(me, UdpPeer.ownAddress, UdpPeer.isHost)) { isMe = true };
        }

        public override event LobbyListReceived_t OnLobbyListReceived;
        public override event LobbyJoined_t OnLobbyJoined;

        public override void RequestLobbyList()
        {
            RainMeadow.DebugMe();
            OnLobbyListReceived?.Invoke(true, UdpPeer.isHost ? new LobbyInfo[0] { } : new LobbyInfo[1] { new LobbyInfo(default, "local", localGameMode) });
        }

        public override void CreateLobby(LobbyVisibility visibility, string gameMode)
        {
            lobby = new Lobby(new OnlineGameMode.OnlineGameModeType(localGameMode), mePlayer);
            OnLobbyJoined?.Invoke(true);
        }

        public override void JoinLobby(LobbyInfo lobby)
        {
            RainMeadow.Debug("Joining local game...");
            var memory = new MemoryStream(16);
            var writer = new BinaryWriter(memory);
            Packet.Encode(new RequestJoinPacket(), writer, null);
            UdpPeer.Send(new IPEndPoint(IPAddress.Loopback, UdpPeer.STARTING_PORT), memory.GetBuffer(), (int)memory.Position, UdpPeer.PacketType.Reliable);
        }

        public void LobbyJoined()
        {
            lobby = new Lobby(new OnlineGameMode.OnlineGameModeType(localGameMode), GetLobbyOwner());
            OnLobbyJoined?.Invoke(true);
        }

        public override void LeaveLobby()
        {
            if (lobby != null)
            {
                var memory = new MemoryStream(16);
                var writer = new BinaryWriter(memory);
                Packet.Encode(new RequestJoinPacket(), writer, null);
                UdpPeer.Send(new IPEndPoint(IPAddress.Loopback, UdpPeer.STARTING_PORT), memory.GetBuffer(), (int)memory.Position, UdpPeer.PacketType.Termination);
                lobby = null;
            }
        }

        public override OnlinePlayer GetLobbyOwner()
        {
            return players.First(p => (p.id as LocalPlayerId).isHost);
        }

        public override void UpdatePlayersList()
        {
            // no op
        }

        internal OnlinePlayer GetPlayerLocal(int port)
        {
            return players.FirstOrDefault(p => (p.id as LocalPlayerId).id == port);
        }

        internal void LocalPlayerJoined(OnlinePlayer joiningPlayer)
        {
            if (players.Contains(joiningPlayer)) { return; }
            players.Add(joiningPlayer);

            if (lobby != null && lobby.owner.isMe)
            {
                // Tell the other players to create this player
                foreach (OnlinePlayer player in players)
                {
                    if (player.isMe)
                        continue;

                    NetIO.SendP2P(player, new ModifyPlayerListPacket(ModifyPlayerListPacket.Operation.Add, new OnlinePlayer[] { joiningPlayer }), SendType.Reliable);
                }

                // Tell joining peer to create everyone in the server
                NetIO.SendP2P(joiningPlayer, new ModifyPlayerListPacket(ModifyPlayerListPacket.Operation.Add, players.ToArray()), SendType.Reliable);
            }
        }

        internal void LocalPlayerLeft(OnlinePlayer leavingPlayer)
        {
            if (!players.Contains(leavingPlayer)) { return; }
            players.Remove(leavingPlayer);

            if (lobby.owner.isMe)
            {
                // Tell the other players to create this player
                foreach (OnlinePlayer player in players)
                {
                    if (player.isMe)
                        continue;

                    NetIO.SendP2P(player, new ModifyPlayerListPacket(ModifyPlayerListPacket.Operation.Remove, new OnlinePlayer[] { leavingPlayer }), SendType.Reliable);
                }
            }
        }
    }
}