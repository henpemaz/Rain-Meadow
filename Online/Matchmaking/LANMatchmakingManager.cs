using System;
using System.Net;
using System.Linq;
using System.IO;
using Menu;
using System.Net.NetworkInformation;
using System.Security.Policy;
using System.Diagnostics;
using System.Security.Cryptography;


namespace RainMeadow {
    

    public class LANMatchmakingManager : MatchmakingManager {
        public class LANLobbyInfo : LobbyInfo {
            public IPEndPoint endPoint;
            public LANLobbyInfo(IPEndPoint endPoint, string name, string mode, int playerCount, bool hasPassword, int maxPlayerCount) : 
                base(name, mode, playerCount, hasPassword, maxPlayerCount) {
                this.endPoint = endPoint;
            }
        }

        public class LANPlayerId : MeadowPlayerId
        {
            override public void OpenProfileLink() {
                OnlineManager.instance.manager.ShowDialog(new DialogNotify("You need Steam active to play Rain Meadow", OnlineManager.instance.manager, null));
            }

            public IPEndPoint? endPoint;
            public byte[] machash = new byte[0];

            public LANPlayerId() { }
            public LANPlayerId(IPEndPoint endPoint, bool makemachash) : base(endPoint.ToString())
            {
                this.endPoint = endPoint;
                if (makemachash) {
                    var buffer = UdpPeer.getOurMacHash();
                } 
            }

            public void reset()
            {
                this.endPoint = default;
                this.machash = new byte[0];
            }

            public override void CustomSerialize(Serializer serializer)
            {
                var endpointBytes = endPoint?.Address.GetAddressBytes() ?? new byte[0];
                var port = endPoint?.Port ?? 0;
                serializer.Serialize(ref endpointBytes);
                serializer.Serialize(ref port);
                serializer.Serialize(ref machash);

                if (serializer.IsReading) {
                    endPoint = new IPEndPoint(new IPAddress(endpointBytes), port);
                }
            }

            public override bool Equals(MeadowPlayerId other)
            {
                if (other is LANPlayerId otherL) {
                    return machash == otherL.machash;
                } else return false;
            }

            public override int GetHashCode() {
                return endPoint?.GetHashCode() ?? 0;
            }
        }
        public override void initializeMePlayer() {
            if (UdpPeer.udpClient == null) {
                return;
            }

            OnlineManager.mePlayer = new OnlinePlayer(new LANPlayerId(UdpPeer.ownEndPoint, true)) { isMe = true };
        }
        public void sessionSetup(bool isHost)
        {
            RainMeadow.DebugMe();
            UdpPeer.Startup();
            initializeMePlayer();
        }

        public void sessionShutdown()
        {
            UdpPeer.Shutdown();
            var thisPlayer = (LANPlayerId)OnlineManager.mePlayer.id;
            thisPlayer.reset();
        }

        public IPEndPoint? currentLobbyHost = null;

        public override event LobbyListReceived_t OnLobbyListReceived;
        public override event PlayerListReceived_t OnPlayerListReceived;
        public override event LobbyJoined_t OnLobbyJoined;

        public OnlinePlayer broadcastPlayer = new OnlinePlayer(
                new LANPlayerId(new IPEndPoint(IPAddress.Broadcast, UdpPeer.STARTING_PORT), false)
            );
        
        static LANLobbyInfo[] lobbyinfo = new LANLobbyInfo[0];
        public override void RequestLobbyList() {
            lobbyinfo = new LANLobbyInfo[1] { new LANLobbyInfo(new IPEndPoint(IPAddress.Any, 0), "LOCAL_MEADOW", "Meadow", 0, false, 32) };
            OnLobbyListReceived?.Invoke(true, lobbyinfo);
            // To create a proper list, we need to send a message to the broadcast endpoint.
            // and wait for responces from possible hosts.
            var memory = new MemoryStream(16);
            var writer = new BinaryWriter(memory);
            OnlineManager.netIO.SendP2P(broadcastPlayer, new RequestLobbyPacket(), NetIO.SendType.Reliable);
        }

        public void addLobby(LANLobbyInfo lobby) {
            RainMeadow.Debug($"Added lobby {lobby}");
            lobbyinfo = lobbyinfo.Append(lobby).ToArray();
            OnLobbyListReceived?.Invoke(true, lobbyinfo);
        }


        public void SendLobbyInfo(IPEndPoint other) {
            if (OnlineManager.lobby != null && OnlineManager.lobby.isOwner) {
                var memory = new MemoryStream(16);
                var writer = new BinaryWriter(memory);
                OnlineManager.netIO.SendP2P(broadcastPlayer, new InformLobbyPacket(
                    maxplayercount, "LAN Lobby", OnlineManager.lobby.hasPassword,
                    OnlineManager.lobby.gameModeType.value, OnlineManager.players.Count), NetIO.SendType.Reliable);
            }
        }

        public OnlinePlayer GetPlayerLAN(IPEndPoint endPoint) {
            return OnlineManager.players.FirstOrDefault(p => (p.id as LANPlayerId).endPoint == endPoint);
        }

        public int maxplayercount = 0;
        public override void CreateLobby(LobbyVisibility visibility, string gameMode, string? password, int? maxPlayerCount) {
            maxplayercount = maxPlayerCount ?? 0;
            sessionSetup(true);
            OnlineManager.lobby = new Lobby(new OnlineGameMode.OnlineGameModeType(gameMode), OnlineManager.mePlayer, password);
            OnLobbyJoined?.Invoke(true);
        }

        public void LobbyAcknoledgedUs()
        {
            OnlineManager.lobby = new Lobby(
                new OnlineGameMode.OnlineGameModeType(OnlineManager.currentlyJoiningLobby.mode, false), 
                GetLobbyOwner(), lobbyPassword);
            currentLobbyHost = (OnlineManager.currentlyJoiningLobby as LANLobbyInfo)?.endPoint;
        }


        public void AcknoledgeLANPlayer(OnlinePlayer joiningPlayer)
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

                    OnlineManager.netIO.SendP2P(player, new ModifyPlayerListPacket(ModifyPlayerListPacket.Operation.Add, new OnlinePlayer[] { joiningPlayer }), 
                        NetIO.SendType.Reliable);
                }

                // Tell joining peer to create everyone in the server
                OnlineManager.netIO.SendP2P(joiningPlayer, new ModifyPlayerListPacket(ModifyPlayerListPacket.Operation.Add, OnlineManager.players.ToArray()), 
                    NetIO.SendType.Reliable);
            }
            OnPlayerListReceived?.Invoke(playerList.ToArray());
        }

        public void RemoveLANPlayer(OnlinePlayer leavingPlayer)
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

                    OnlineManager.netIO.SendP2P(player, new ModifyPlayerListPacket(ModifyPlayerListPacket.Operation.Remove, new OnlinePlayer[] { leavingPlayer }), 
                        NetIO.SendType.Reliable);
                }
            }
            OnPlayerListReceived?.Invoke(playerList.ToArray());
        }

        string lobbyPassword = "";
        public override void RequestJoinLobby(LobbyInfo lobby, string? password) {
            if (lobby is LANLobbyInfo lobbyinfo) {
                lobbyPassword = password ?? "";
                OnlineManager.currentlyJoiningLobby = lobby;
                var lobbyInfo = (LANLobbyInfo)lobby;
                if (lobbyInfo.endPoint == null)
                {
                    RainMeadow.Debug("Failed to join local game...");
                    return;
                }
                
                OnlineManager.netIO.SendP2P(new OnlinePlayer(new LANPlayerId(lobbyInfo.endPoint, false)), 
                    new RequestJoinPacket(), NetIO.SendType.Reliable);
            } else {
                RainMeadow.Error("Invalid lobby type");
            }
        }

        public override void JoinLobby(bool success) {
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

        public override void LeaveLobby() {
            if (OnlineManager.lobby != null)
            {
                if (!OnlineManager.lobby.isOwner && currentLobbyHost != null)
                {
                    OnlineManager.netIO.SendP2P(new OnlinePlayer(new LANPlayerId(currentLobbyHost, false)), 
                        new RequestJoinPacket(), NetIO.SendType.Reliable);
                }
            }
        }

        public override OnlinePlayer GetLobbyOwner() {
            return OnlineManager.players.FirstOrDefault((p) => {
                if (p.id is LANPlayerId lanid) {
                    return lanid.endPoint == (OnlineManager.currentlyJoiningLobby as LANLobbyInfo)?.endPoint;
                }

                return false;
            });
        }   

        public override MeadowPlayerId GetEmptyId() {
            return new LANPlayerId(null, false);
        }

        public override string GetLobbyID() {
            throw new NotImplementedException();
        }
        public override void OpenInvitationOverlay() {
            throw new NotImplementedException();
        }
    }
}