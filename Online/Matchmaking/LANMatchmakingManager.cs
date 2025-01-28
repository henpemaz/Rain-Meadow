using System;
using System.Net;
using System.Linq;
using System.IO;
using Menu;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Diagnostics;
using UnityEngine;


namespace RainMeadow {
    

    public class LANMatchmakingManager : MatchmakingManager {
        public string GenerateRandomUsername()
        {
            return "";
        }
        public class LANLobbyInfo : LobbyInfo {
            public IPEndPoint endPoint;
            public LANLobbyInfo(IPEndPoint endPoint, string name, string mode, int playerCount, bool hasPassword, int maxPlayerCount) : 
                base(name, mode, playerCount, hasPassword, maxPlayerCount) {
                this.endPoint = endPoint;
            }
        }   

        public class LANPlayerId : MeadowPlayerId
        {
            // Blackhole Endpoint
            // https://superuser.com/questions/698244/ip-address-that-is-the-equivalent-of-dev-null
            static readonly IPEndPoint BlackHole = new IPEndPoint(IPAddress.Parse("253.253.253.253"), 999); 
            public IPEndPoint endPoint;

            public LANPlayerId() { }
            public LANPlayerId(IPEndPoint? endPoint) : base(
                    UsernameGenerator.GenerateRandomUsername(endPoint?.GetHashCode() ?? 0))
            {
                this.endPoint = endPoint ?? BlackHole;
            }

            public override void OpenProfileLink() {
                string dialogue = name + " comes from " + endPoint.ToString();
                if (OnlineManager.lobby?.owner?.id?.Equals(this) ?? false) {
                    dialogue += Environment.NewLine + "This player is the owner of the lobby.";
                    dialogue += Environment.NewLine + "Players can Direct Connect to this lobby through their IP Address.";
                }
                OnlineManager.instance.manager.ShowDialog(new DialogNotify(dialogue, OnlineManager.instance.manager, null));
            }

            public void reset()
            {
                this.endPoint = BlackHole;
            }

            public override int GetHashCode() {
                return this.endPoint?.GetHashCode() ?? 0;
            }

            public override void CustomSerialize(Serializer serializer)
            {
                if (serializer.IsWriting) {
                    if (this.isLoopback()) {
                        serializer.writer.Write(true);
                    } else {
                        serializer.writer.Write(false);
                        serializer.writer.Write((int)endPoint.Port);
                        serializer.writer.Write((int)endPoint.Address.GetAddressBytes().Length);
                        serializer.writer.Write(endPoint.Address.GetAddressBytes());
                    }
                } else if (serializer.IsReading) {
                    bool issender = serializer.reader.ReadBoolean();
                    if (issender) {
                        this.endPoint = (serializer.currPlayer.id as LANPlayerId)?.endPoint ?? BlackHole;
                    } else {
                        int port = serializer.reader.ReadInt32();
                        byte[] endpointbytes = serializer.reader.ReadBytes(serializer.reader.ReadInt32());
                        this.endPoint = new IPEndPoint(new IPAddress(endpointbytes), port);
                    }
                }
            }

            public bool isLoopback() {
                if (OnlineManager.netIO is LANNetIO netio) {
                    if (netio.manager.port != endPoint?.Port) return false;
                }

                return UDPPeerManager.isLoopback(endPoint.Address);
            }

            public override bool Equals(MeadowPlayerId other)
            {
                
                if (other is LANPlayerId lanid) {
                    return UDPPeerManager.CompareIPEndpoints(endPoint, lanid.endPoint); 
                }
                return false;
            }
        }
        public override void initializeMePlayer() {
            if (OnlineManager.netIO is LANNetIO netio) {
                OnlineManager.mePlayer = new OnlinePlayer(new LANPlayerId(new IPEndPoint(
                    UDPPeerManager.getInterfaceAddresses()[0], netio.manager.port))) { isMe = true };
                if (RainMeadow.rainMeadowOptions.LanUserName.Value.Length > 0) {
                    OnlineManager.mePlayer.id.name = RainMeadow.rainMeadowOptions.LanUserName.Value;
                }
            } 
        }

        
        static List<LANLobbyInfo> lobbyinfo = new();
        public override void RequestLobbyList() {
            lobbyinfo.Clear();
            
            // To create a proper list, we need to send a message to the broadcast endpoint.
            // and wait for responces from possible hosts.
            for (int i = 0; i < 8; i++) {
                if (OnlineManager.netIO is LANNetIO lanentio) {
                    using (MemoryStream memoryStream = new())
                    using (BinaryWriter writer = new(memoryStream)) {
                        lanentio.SendBroadcast(new RequestLobbyPacket());
                    }
                }

            }
        }

        public void addLobby(LANLobbyInfo lobby) {
            var updating_lobby = lobbyinfo.FirstOrDefault(x => UDPPeerManager.CompareIPEndpoints(x.endPoint, lobby.endPoint));
            if (updating_lobby is null) {
                RainMeadow.Debug($"Added lobby {lobby}");
                lobbyinfo.Add(lobby);
            } else {
                updating_lobby.hasPassword = lobby.hasPassword;
                updating_lobby.name = lobby.name;
                updating_lobby.mode = lobby.mode;
                updating_lobby.playerCount = lobby.playerCount;
                updating_lobby.maxPlayerCount = lobby.maxPlayerCount;
            }

            
            
            OnLobbyListReceivedEvent(true,  lobbyinfo.ToArray());
        }


        public void SendLobbyInfo(OnlinePlayer other) {
            if (OnlineManager.lobby != null && OnlineManager.lobby.isOwner) {
                if (OnlineManager.netIO is LANNetIO lannetio) {
                    var packet = new InformLobbyPacket(
                        maxplayercount, "LAN Lobby", OnlineManager.lobby.hasPassword,
                        OnlineManager.lobby.gameModeType.value, OnlineManager.players.Count);
                    OnlineManager.netIO.SendP2P(other, packet, NetIO.SendType.Unreliable, true);
                }
            }
        }

        public OnlinePlayer GetPlayerLAN(IPEndPoint other) {
            return OnlineManager.players.FirstOrDefault(p => {
                if (p.id is LANPlayerId lanid)
                    if (lanid.endPoint != null)
                        return UDPPeerManager.CompareIPEndpoints(lanid.endPoint, other);
                return false;
            });
        }

        public override bool canSendChatMessages => true;
        public override void SendChatMessage(string message) {
            foreach (OnlinePlayer player in OnlineManager.players) {
                if (player.isMe) continue;
                OnlineManager.netIO.SendP2P(player, new ChatMessagePacket(message), NetIO.SendType.Reliable);
            }

            RecieveChatMessage(OnlineManager.mePlayer, message);
        }

        public int maxplayercount = 0;
        public override void CreateLobby(LobbyVisibility visibility, string gameMode, string? password, int? maxPlayerCount) {
            maxplayercount = maxPlayerCount ?? 0;
            OnlineManager.lobby = new Lobby(new OnlineGameMode.OnlineGameModeType(gameMode), OnlineManager.mePlayer, password);
            MatchmakingManager.OnLobbyJoinedEvent(true, "");
        }

        public void LobbyAcknoledgedUs(OnlinePlayer owner)
        {
            RainMeadow.DebugMe();
            if (OnlineManager.lobby is null) {
                OnlineManager.lobby = new Lobby(
                    new OnlineGameMode.OnlineGameModeType(OnlineManager.currentlyJoiningLobby.mode, false), 
                    owner, lobbyPassword);
            }
                
        }


        public void AcknoledgeLANPlayer(OnlinePlayer joiningPlayer)
        {
            var lanid = joiningPlayer.id as LANPlayerId;
            if (lanid is null) return;
            if (lanid.isLoopback()) return; 


            RainMeadow.DebugMe();
            if (OnlineManager.players.Contains(joiningPlayer)) { return; }
            OnlineManager.players.Add(joiningPlayer);
            (OnlineManager.netIO as LANNetIO)?.SendAcknoledgement(joiningPlayer);
            RainMeadow.Debug($"Added {joiningPlayer} to the lobby matchmaking player list");

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
                OnlineManager.netIO.SendP2P(joiningPlayer, new ModifyPlayerListPacket(ModifyPlayerListPacket.Operation.Add, 
                    OnlineManager.players.Append(OnlineManager.mePlayer).ToArray()), 
                    NetIO.SendType.Reliable);
            }
            OnPlayerListReceivedEvent(playerList.ToArray());
        }

        public void RemoveLANPlayer(OnlinePlayer leavingPlayer)
        {
            StackTrace stackTrace = new();
            RainMeadow.Debug(stackTrace.ToString());


            if (leavingPlayer.isMe) return; 
            if (!OnlineManager.players.Contains(leavingPlayer)) { return; }
            HandleDisconnect(leavingPlayer);
            if (OnlineManager.lobby is not null)
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
            OnlineManager.netIO.ForgetPlayer(leavingPlayer);
            OnPlayerListReceivedEvent(playerList.ToArray());
        }
        string lobbyPassword = "";
        public override void RequestJoinLobby(LobbyInfo lobby, string? password) {
            RainMeadow.DebugMe();
            if (lobby is LANLobbyInfo lobbyinfo) {
                lobbyPassword = password ?? "";
                OnlineManager.currentlyJoiningLobby = lobby;
                var lobbyInfo = (LANLobbyInfo)lobby;
                if (lobbyInfo.endPoint == null)
                {
                    RainMeadow.Debug("Failed to join local game...");
                    return;
                }
                
                RainMeadow.Debug("Sending Request to join lobby...");
                OnlineManager.netIO.SendP2P(new OnlinePlayer(new LANPlayerId(lobbyInfo.endPoint)), 
                    new RequestJoinPacket(OnlineManager.mePlayer.id.name), NetIO.SendType.Reliable, true);
            } else {
                RainMeadow.Error("Invalid lobby type");
            }
        }

        public override void JoinLobby(bool success) {
            if (success)
            {
                RainMeadow.Debug("Joining lobby");
                OnLobbyJoinedEvent(true);
            }
            else
            {
                OnlineManager.LeaveLobby();
                RainMeadow.Debug("Failed to join local game. Wrong Password");
                OnLobbyJoinedEvent(false, "Wrong password!");
            }
        }

        public override void LeaveLobby() {
            if (OnlineManager.players is not null)
            foreach (OnlinePlayer p in  OnlineManager.players) {
                OnlineManager.netIO.SendP2P(p, 
                    new SessionEndPacket(), 
                        NetIO.SendType.Reliable);
            }
            OnlineManager.netIO.ForgetEverything();
        }

        public override OnlinePlayer GetLobbyOwner() {
            if (OnlineManager.lobby == null) return null;

            if (OnlineManager.lobby.owner.hasLeft == true || OnlineManager.lobby == null) {
                // select a new owner. 
                // The order of players should be 
                for (int i = 0; i < OnlineManager.players.Count; i++) {
                    OnlinePlayer onlinePlayer = OnlineManager.players[i];
                    if (onlinePlayer.hasLeft) continue;
                    return onlinePlayer;
                }
            }

            return OnlineManager.lobby.owner;
        }   

        public override MeadowPlayerId GetEmptyId() {
            return new LANPlayerId(null);
        }

        public override string GetLobbyID() {
            if (OnlineManager.lobby != null) {
                return (OnlineManager.lobby.owner.id as LANPlayerId)?.GetPersonaName() ?? "Nobody" + "'s Lobby";
            }

            return "Unknown Lan Lobby";
        }
        public override void OpenInvitationOverlay() {
            OnlineManager.instance.manager.ShowDialog(new DialogNotify("You cannot use this feature here.", OnlineManager.instance.manager, null));
        }
    }
}