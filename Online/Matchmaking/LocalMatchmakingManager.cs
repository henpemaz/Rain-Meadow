using System.IO;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using static RainMeadow.NetIO;

namespace RainMeadow
{
    public class LocalMatchmakingManager : MatchmakingManager
    {
        public class LocalPlayerId : MeadowPlayerId
        {
            public int id;
            public IPEndPoint endPoint;
            public bool isHost;

            public LocalPlayerId() { }
            public LocalPlayerId(int id, IPEndPoint endPoint, bool isHost) : base($"local:{id}")
            {
                this.id = id;
                this.endPoint = endPoint;
                this.isHost = isHost;
            }

            public override void CustomSerialize(Serializer serializer)
            {
                serializer.Serialize(ref id);
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

        public override MeadowPlayerId GetEmptyId()
        {
            return new LocalPlayerId();
        }

        private int me;
#if ARENAP2P
        private string localGameMode = "ArenaCompetitive";
#elif STORYP2P
        private string localGameMode = "Story";
#else
        private string localGameMode = "FreeRoam";
#endif

        public LocalMatchmakingManager()
        {
            RainMeadow.DebugMe();
            UdpPeer.Startup();
            me = UdpPeer.port;
            OnlineManager.mePlayer = new OnlinePlayer(new LocalPlayerId(me, UdpPeer.ownAddress, UdpPeer.isHost)) { isMe = true };
        }

        public override event LobbyListReceived_t OnLobbyListReceived;
        public override event PlayerListReceived_t OnPlayerListReceived;
        public override event LobbyJoined_t OnLobbyJoined;

        public override void RequestLobbyList()
        {
            RainMeadow.DebugMe();
            OnLobbyListReceived?.Invoke(true, UdpPeer.isHost ? new LobbyInfo[0] { } : new LobbyInfo[1] { new LobbyInfo(default, "local", localGameMode, 0) });
        }

        public override void CreateLobby(LobbyVisibility visibility, string gameMode)
        {
            if (!UdpPeer.isHost)
            {
                OnLobbyJoined?.Invoke(false);
                return;
            }
            OnlineManager.lobby = new Lobby(new OnlineGameMode.OnlineGameModeType(localGameMode), OnlineManager.mePlayer);
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
            OnlineManager.lobby = new Lobby(new OnlineGameMode.OnlineGameModeType(localGameMode), GetLobbyOwner());
            OnLobbyJoined?.Invoke(true);
        }

        public override void LeaveLobby()
        {
            if (OnlineManager.lobby != null)
            {
                var memory = new MemoryStream(16);
                var writer = new BinaryWriter(memory);
                Packet.Encode(new RequestJoinPacket(), writer, null);
                UdpPeer.Send(new IPEndPoint(IPAddress.Loopback, UdpPeer.STARTING_PORT), memory.GetBuffer(), (int)memory.Position, UdpPeer.PacketType.Termination);
                OnlineManager.lobby = null;
            }
        }

        public override OnlinePlayer GetLobbyOwner()
        {
            return OnlineManager.players.First(p => (p.id as LocalPlayerId).isHost);
        }

        public OnlinePlayer GetPlayerLocal(int port)
        {
            return OnlineManager.players.FirstOrDefault(p => (p.id as LocalPlayerId).id == port);
        }

        public void LocalPlayerJoined(OnlinePlayer joiningPlayer)
        {
            if (OnlineManager.players.Contains(joiningPlayer)) { return; }
            RainMeadow.Debug($"Added {joiningPlayer} to the lobby matchmaking player list");
            OnlineManager.players.Add(joiningPlayer);

            if (OnlineManager.lobby != null && OnlineManager.lobby.owner.isMe)
            {
                // Tell the other players to create this player
                foreach (OnlinePlayer player in OnlineManager.players)
                {
                    if (player.isMe || player == joiningPlayer)
                        continue;

                    SendP2P(player, new ModifyPlayerListPacket(ModifyPlayerListPacket.Operation.Add, new OnlinePlayer[] { joiningPlayer }), SendType.Reliable);
                }

                // Tell joining peer to create everyone in the server
                SendP2P(joiningPlayer, new ModifyPlayerListPacket(ModifyPlayerListPacket.Operation.Add, OnlineManager.players.ToArray()), SendType.Reliable);
            }
            UpdatePlayersList();
        }

        public void LocalPlayerLeft(OnlinePlayer leavingPlayer)
        {
            if (!OnlineManager.players.Contains(leavingPlayer)) { return; }

            OnlineManager.players.Remove(leavingPlayer);

            if (OnlineManager.lobby.owner.isMe)
            {
                // Tell the other players to create this player
                foreach (OnlinePlayer player in OnlineManager.players)
                {
                    if (player.isMe)
                        continue;

                    SendP2P(player, new ModifyPlayerListPacket(ModifyPlayerListPacket.Operation.Remove, new OnlinePlayer[] { leavingPlayer }), SendType.Reliable);
                }
            }
            UpdatePlayersList();
        }

        public void UpdatePlayersList()
        {
            List<PlayerInfo> playersinfo = new List<PlayerInfo>();
            foreach (OnlinePlayer player in OnlineManager.players)
            {
                if (!player.isMe)
                    playersinfo.Add(new PlayerInfo(default, player.id.name));
            }
            OnPlayerListReceived?.Invoke(playersinfo.ToArray());
        }
    }
}