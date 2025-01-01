

namespace RainMeadow {
    using LANPlayerId = LocalMatchmakingManager.LocalPlayerId;
    using LANLobbyInfo = LocalMatchmakingManager.LocalLobbyInfo;
    using System;
    using System.Net;
    using System.Linq;
    using System.Diagnostics.Design;
    using System.IO;

    class LANMatchmakingManager : MatchmakingManager {
        public LANMatchmakingManager() {
            OnlineManager.mePlayer = new OnlinePlayer(new LANPlayerId(0, UdpPeer.ownEndPoint, false)) { isMe = true };
        }
        public void sessionSetup(bool isHost)
        {
            RainMeadow.DebugMe();
            UdpPeer.Startup();
            var thisPlayer = (LANPlayerId)OnlineManager.mePlayer.id;
            thisPlayer.name = $"local:{UdpPeer.port}";
            thisPlayer.endPoint = UdpPeer.ownEndPoint;
            thisPlayer.isHost = isHost;
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

        public override void RequestLobbyList() {
            // To create a proper list, we need to send a message to the broadcast endpoint.
            // and wait for responces from possible hosts.
            return;
        }

        public OnlinePlayer GetPlayerLAN(IPEndPoint endPoint) {
            return OnlineManager.players.FirstOrDefault(p => (p.id as LANPlayerId).endPoint == endPoint);
        }

        string? lobbyPassword = "";
        public override void CreateLobby(LobbyVisibility visibility, string gameMode, string? password, int? maxPlayerCount) {
            this.lobbyPassword = password;
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

        public override void RequestJoinLobby(LobbyInfo lobby, string? password) {
            if (lobby is LANLobbyInfo lobbyinfo) {
                lobbyPassword = password;
                OnlineManager.currentlyJoiningLobby = lobby;
                if (((LANPlayerId)OnlineManager.mePlayer.id).isHost)
                {
                    OnLobbyJoined?.Invoke(false, "use the client");
                    return;
                }

                var lobbyInfo = (LANLobbyInfo)lobby;
                if (lobbyInfo.endPoint == null)
                {
                    RainMeadow.Debug("Failed to join local game...");
                    return;
                }
                
                var memory = new MemoryStream(16);
                var writer = new BinaryWriter(memory);
                Packet.Encode(new RequestJoinPacket(), writer, null);
                UdpPeer.Send(lobbyInfo.endPoint, memory.GetBuffer(), (int)memory.Position, UdpPeer.PacketType.Reliable);
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
                    var memory = new MemoryStream(16);
                    var writer = new BinaryWriter(memory);
                    Packet.Encode(new RequestLeavePacket(), writer, null);
                    UdpPeer.Send(currentLobbyHost, memory.GetBuffer(), (int)memory.Position, UdpPeer.PacketType.Reliable);
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
            return new LANPlayerId(0, null, false);
        }

        public override string GetLobbyID() {
            throw new NotImplementedException();
        }
        public override void OpenInvitationOverlay() {
            throw new NotImplementedException();
        }
    }
}