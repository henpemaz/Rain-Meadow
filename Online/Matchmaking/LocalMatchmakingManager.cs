using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using static RainMeadow.NetIO;

namespace RainMeadow
{
    public class LocalMatchmakingManager : MatchmakingManager
    {
#if ARENAP2P
        public static string localGameMode = "ArenaCompetitive";
#elif STORYP2P
        public static string localGameMode = "Story";
#elif FREEROAMP2P
        public static string localGameMode = "FreeRoam";
#else
        public static string localGameMode = "Meadow";
#endif

        public class LocalPlayerId : MeadowPlayerId
        {
            public int id;
            public IPEndPoint? endPoint;
            public bool isHost;

            public LocalPlayerId() { }
            public LocalPlayerId(int id, IPEndPoint endPoint, bool isHost) : base($"local:{id}")
            {
                this.id = id;
                this.endPoint = endPoint;
                this.isHost = isHost;
            }

            public void reset()
            {
                this.id = default;
                this.endPoint = default;
                this.isHost = default;
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

        public string? lobbyPassword;
        public LobbyInfo lobbyInfo;

        private int me = -1;
        private IPEndPoint currentLobbyHost = null;

        public LocalMatchmakingManager()
        {
            OnlineManager.mePlayer = new OnlinePlayer(new LocalPlayerId(me, null, false)) { isMe = true };
        }

        public override event LobbyListReceived_t OnLobbyListReceived;
        public override event PlayerListReceived_t OnPlayerListReceived;
        public override event LobbyJoined_t OnLobbyJoined;

        public override void RequestLobbyList()
        {
            RainMeadow.DebugMe();
            //OnLobbyListReceived?.Invoke(true, new LobbyInfo[0] { });
            // Create the proper list
            var fakeEndpoint = new IPEndPoint(IPAddress.Loopback, UdpPeer.STARTING_PORT);
            OnLobbyListReceived?.Invoke(true, new LobbyInfo[25] {
                new LobbyInfo(fakeEndpoint, "Ancient Liberties", "Meadow", 2, true, 9),
                new LobbyInfo(fakeEndpoint, "Lonesome Song", "Story", 8, false, 9),
                new LobbyInfo(fakeEndpoint, "Cracked Crescents Abound", "ArenaCompetitive", 11, true, 18),
                new LobbyInfo(fakeEndpoint, "Boundless Opportunities", "Meadow", 20, false, 22),
                new LobbyInfo(fakeEndpoint, "Unearthed Experience", "Story", 13, true, 24),
                new LobbyInfo(fakeEndpoint, "Nonstop Vigilance", "ArenaCompetitive", 30, false, 31),
                new LobbyInfo(fakeEndpoint, "Solemn Overture", "Meadow", 13, true, 19),
                new LobbyInfo(fakeEndpoint, "Roaring Moon", "Story", 20, false, 23),
                new LobbyInfo(fakeEndpoint, "Silent Call", "ArenaCompetitive", 2, true, 17),
                new LobbyInfo(fakeEndpoint, "Blind Allegiance", "Meadow", 13, false, 18),
                new LobbyInfo(fakeEndpoint, "Unbroken Resolute", "Story", 11, true, 30),
                new LobbyInfo(fakeEndpoint, "Thirty-two Pebbles", "ArenaCompetitive", 23, false, 32),
                new LobbyInfo(fakeEndpoint, "Three Feathers Uncovered", "Meadow", 8, true, 14),
                new LobbyInfo(fakeEndpoint, "Ten Marbles Colored", "Story", 25, false, 30),
                new LobbyInfo(fakeEndpoint, "Eight Rusted Memories", "ArenaCompetitive", 28, true, 29),
                new LobbyInfo(fakeEndpoint, "One Light Broken", "Meadow", 26, false, 11),
                new LobbyInfo(fakeEndpoint, "Two Seeds Grown", "Story", 19, true, 20),
                new LobbyInfo(fakeEndpoint, "Pink Lizard 🔥", "ArenaCompetitive", 9, false, 24),
                new LobbyInfo(fakeEndpoint, "Them", "Meadow", 6, true, 28),
                new LobbyInfo(fakeEndpoint, "Person", "Story", 2147483647, false, 2147483647),
                new LobbyInfo(fakeEndpoint, "The One Who Waits", "ArenaCompetitive", 11, true, 12),
                new LobbyInfo(fakeEndpoint, "Fisher Price Pebbles (lmao)", "Meadow", 21, false, 26),
                new LobbyInfo(fakeEndpoint, "notchoc", "Story", -7, true, -16),
                new LobbyInfo(fakeEndpoint, "Glistening Sanctuaries", "ArenaCompetitive", 4, true, 15),
                new LobbyInfo(fakeEndpoint, "Flaming Hot Cheetos", "Meadow", 18, true, 17),
            });
        }

        public void sessionSetup(bool isHost)
        {
            RainMeadow.DebugMe();
            UdpPeer.Startup();
            me = UdpPeer.port;
            isHost = me == UdpPeer.STARTING_PORT;
            var thisPlayer = (LocalPlayerId)OnlineManager.mePlayer.id;
            thisPlayer.name = $"local:{me}";
            thisPlayer.isHost = isHost;
            thisPlayer.id = me;
            thisPlayer.endPoint = UdpPeer.ownEndPoint;
        }

        public void sessionShutdown()
        {
            UdpPeer.Shutdown();

            var thisPlayer = (LocalPlayerId)OnlineManager.mePlayer.id;
            thisPlayer.reset();
        }

        // public override void CreateLobby(LobbyVisibility visibility, string gameMode, string? password, int? maxPlayerCount, string name)
        public override void CreateLobby(LobbyVisibility visibility, string gameMode, string? password, int? maxPlayerCount)
        {
            sessionSetup(true);
            if (!((LocalPlayerId)OnlineManager.mePlayer.id).isHost)
            {
                OnLobbyJoined?.Invoke(false, "use the host");
                return;
            }

            OnlineManager.lobby = new Lobby(new OnlineGameMode.OnlineGameModeType(localGameMode), OnlineManager.mePlayer, password);
            OnLobbyJoined?.Invoke(true);
        }

        public override void RequestJoinLobby(LobbyInfo lobby, string? password)
        {
            sessionSetup(false);
            if (((LocalPlayerId)OnlineManager.mePlayer.id).isHost)
            {
                OnLobbyJoined?.Invoke(false, "use the client");
                return;
            }
            RainMeadow.Debug("Trying to join local game...");
            if (lobby.ipEndpoint == null)
            {
                RainMeadow.Debug("Failed to join local game...");
                return;
            }
            lobbyPassword = password;
            var memory = new MemoryStream(16);
            var writer = new BinaryWriter(memory);
            Packet.Encode(new RequestJoinPacket(), writer, null);
            UdpPeer.Send(lobby.ipEndpoint, memory.GetBuffer(), (int)memory.Position, UdpPeer.PacketType.Reliable);
        }
        public void LobbyJoined()
        {
            OnlineManager.lobby = new Lobby(new OnlineGameMode.OnlineGameModeType(localGameMode), GetLobbyOwner(), lobbyPassword);
            var lobbyOwner = (LocalPlayerId)OnlineManager.lobby.owner.id;
            currentLobbyHost = lobbyOwner.endPoint;
        }
        public override void JoinLobby(bool success)
        {
            if (success)
            {
                OnLobbyJoined?.Invoke(true);
            }
            else
            {
                LeaveLobby();
                RainMeadow.Debug("Failed to join local game. Wrong Password");
                OnLobbyJoined?.Invoke(false, "Wrong password!");
            }

        }

        public override void LeaveLobby()
        {
            if (OnlineManager.lobby != null)
            {
                if (!OnlineManager.lobby.isOwner)
                {
                    var memory = new MemoryStream(16);
                    var writer = new BinaryWriter(memory);
                    Packet.Encode(new RequestLeavePacket(), writer, null);
                    UdpPeer.Send(currentLobbyHost, memory.GetBuffer(), (int)memory.Position, UdpPeer.PacketType.Reliable);
                }
            }
        }

        public override OnlinePlayer GetLobbyOwner()
        {
            //fix this
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

            if (OnlineManager.lobby != null && OnlineManager.lobby.isOwner)
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

            HandleDisconnect(leavingPlayer);

            if (OnlineManager.lobby.isOwner)
            {
                // Tell the other players to remove this player
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
            OnPlayerListReceived?.Invoke(playerList.ToArray());
        }

        public override string GetLobbyID()
        {
            return "some_id";
        }
    }
}
