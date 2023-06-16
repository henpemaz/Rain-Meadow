using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace RainMeadow
{
    public static class PlayersManager
    {
        private static Callback<SteamNetworkingMessagesSessionRequest_t> m_SessionRequest;
        public static OnlinePlayer mePlayer;
        public static List<OnlinePlayer> players;
        public static int nextPlayerId; // Everyone keeps track of this

        public enum PlayerSetup : byte {
            RequestJoin,
            UpdatePlayer,
            CreatePlayer,
            RemovePlayer,
            Ready,
            Leave,
        }
        public static void InitPlayersManager()
        {
            m_SessionRequest = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(SessionRequest);
            LocalPeer.PeerTerminated += (ep) => PlayerLeft(PlayerFromIp(ep));
        }

        public static void Reset()
        {
            if (players != null) {
                foreach (OnlinePlayer player in players) {
                    if (player.isUsingSteam)
                        SteamNetworking.CloseP2PSessionWithUser(player.steamId);
                }
            }
            mePlayer = new OnlinePlayer(1, SteamUser.GetSteamID()) { name = SteamFriends.GetPersonaName() };
            players = new List<OnlinePlayer>() { mePlayer };
            nextPlayerId = 2;
        }

        public static void UpdatePlayersList(CSteamID cSteamID)
        {
            if (!mePlayer.isUsingSteam)
                return;

            try
            {
                RainMeadow.DebugMe();
                var n = SteamMatchmaking.GetNumLobbyMembers(cSteamID);
                var oldplayers = players.FindAll(p => p.isUsingSteam).Select(p => p.steamId).ToArray();
                var newplayers = new CSteamID[n];
                for (int i = 0; i < n; i++)
                {
                    newplayers[i] = SteamMatchmaking.GetLobbyMemberByIndex(cSteamID, i);
                }
                foreach (var p in oldplayers)
                {
                    if (!newplayers.Contains(p)) PlayerLeft(PlayerFromId(p));
                }
                foreach (var p in newplayers)
                {
                    if (!oldplayers.Contains(p)) PlayerJoined(p);
                }
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        // Steam version - everyone using steam gets this event automaticly
        private static void PlayerJoined(CSteamID joiningSteamId)
        { 
            if (players.Exists(p => p.steamId == joiningSteamId))
                // Ignore me! I am already here!
                return;
            
            RainMeadow.Debug($"Steam {SteamFriends.GetFriendPersonaName(joiningSteamId)} ({joiningSteamId})");
            
            // Lobby owner
            if (OnlineManager.lobby != null && OnlineManager.lobby.owner.isMe) {
                byte[] buffer = new byte[13];
                MemoryStream stream = new MemoryStream(buffer);
                BinaryWriter writer = new BinaryWriter(stream);

                // Tell the joining steam player to update their network id
                writer.Write((byte)PlayerSetup.UpdatePlayer);
                writer.Write(nextPlayerId);
                writer.Write((ulong)joiningSteamId);

                SteamNetworking.SendP2PPacket(joiningSteamId, buffer, 13, EP2PSend.k_EP2PSendReliable, 1);

                // Tell other non-steam peers to create this new player
                stream.Seek(0, SeekOrigin.Begin);
                writer.Write((byte)PlayerSetup.CreatePlayer);

                // TODO: Redo this later for when it is no longer for debugging
                foreach (OnlinePlayer player in players) {
                    if (player.isUsingSteam)
                        continue;

                    LocalPeer.Send(buffer, 13, LocalPeer.PacketType.Reliable, LocalPeer.PacketDataType.PlayerInfo); // Only local peer, but an endpoint should be passed here
                }

                // Tell joining player to create everyone in the server
                foreach (OnlinePlayer p in players) {
                    stream.Seek(0, SeekOrigin.Begin);
                    writer.Write((byte)PlayerSetup.CreatePlayer);
                    writer.Write(p.netId);
                    writer.Write((ulong)p.steamId);

                    SteamNetworking.SendP2PPacket(joiningSteamId, buffer, 13, EP2PSend.k_EP2PSendReliable, 1);
                }

                players.Add(new OnlinePlayer(nextPlayerId, joiningSteamId));
                nextPlayerId ++;
            }
        }

        // Non steam version - IF THIS IS NOT USED FOR DEBUG TODO: lobby owner needs to broadcast this
        private static void PlayerJoined(IPEndPoint joiningEndpoint)
        {
            if (players.Exists(p => p.endpoint != null && p.endpoint.Equals(joiningEndpoint)))
                // Ignore me! I am already here!
                return;

            RainMeadow.Debug($"Non-Steam {joiningEndpoint}");
            
            // Lobby owner
            if (OnlineManager.lobby != null && OnlineManager.lobby.owner.isMe) {
                byte[] buffer = new byte[13];
                MemoryStream stream = new MemoryStream(buffer);
                BinaryWriter writer = new BinaryWriter(stream);

                // Tell the joining player to update their network id
                writer.Write((byte)PlayerSetup.UpdatePlayer);
                writer.Write(nextPlayerId);
                writer.Write((ulong)CSteamID.Nil);

                LocalPeer.Send(buffer, 13, LocalPeer.PacketType.Reliable, LocalPeer.PacketDataType.PlayerInfo);

                // Tell the other players to create this player
                stream.Seek(0, SeekOrigin.Begin);
                writer.Write((byte)PlayerSetup.CreatePlayer);
                
                foreach (OnlinePlayer player in players) {
                    if (player.isUsingSteam) {
                        SteamNetworking.SendP2PPacket(player.steamId, buffer, 13, EP2PSend.k_EP2PSendReliable, 1);
                    } else {
                        LocalPeer.Send(buffer, 13, LocalPeer.PacketType.Reliable, LocalPeer.PacketDataType.PlayerInfo);
                    }
                }
            }
            
            // Add player yourself
            players.Add(new OnlinePlayer(nextPlayerId, joiningEndpoint));
            nextPlayerId ++;
        }

        private static void PlayerLeft(OnlinePlayer leavingPlayer)
        {
            if (leavingPlayer == null)
                return;

            RainMeadow.Debug($"Handling player disconnect:{leavingPlayer}");
            leavingPlayer.hasLeft = true;
            OnlineManager.lobby?.OnPlayerDisconnect(leavingPlayer);
            while (leavingPlayer.HasUnacknoledgedEvents())
            {
                leavingPlayer.AbortUnacknoledgedEvents();
                OnlineManager.lobby?.OnPlayerDisconnect(leavingPlayer);
            }

            RainMeadow.Debug($"Actually removing player:{leavingPlayer}");
            players.Remove(leavingPlayer);

            // Relay to everyone if both them and the leaving player is not using steam
            // If both ends are using steam, the above is already done
            if (OnlineManager.lobby.owner.isMe) {
                byte[] buffer = new byte[13];
                MemoryStream stream = new MemoryStream(buffer);
                BinaryWriter writer = new BinaryWriter(stream);

                writer.Write((byte)PlayerSetup.RemovePlayer); // Remove
                writer.Write(leavingPlayer.netId);
                writer.Write((ulong)leavingPlayer.steamId);

                foreach (OnlinePlayer player in players) {
                    if (!(leavingPlayer.isUsingSteam && player.isUsingSteam))
                        continue;

                    LocalPeer.Send(buffer, 13, LocalPeer.PacketType.Reliable);
                }

            }
        }

        private static void SessionRequest(SteamNetworkingMessagesSessionRequest_t param)
        {
            try
            {
                var id = new CSteamID(param.m_identityRemote.GetSteamID64());
                RainMeadow.Debug("session request from " + id);
                if (OnlineManager.lobby != null)
                {
                    if (players.FirstOrDefault(op => op.steamId == id) is OnlinePlayer p)
                    {
                        RainMeadow.Debug("accepted session from " + p.name);
                        SteamNetworkingMessages.AcceptSessionWithUser(ref param.m_identityRemote);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        public static OnlinePlayer BestTransferCandidate(OnlineResource onlineResource, Dictionary<OnlinePlayer, ResourceMembership> subscribers)
        {
            if (subscribers.Keys.Contains(mePlayer)) return mePlayer;
            // todo pick by ping?
            if (subscribers.Count < 1) return null;
            return subscribers.First().Key;
        }

        public static OnlinePlayer PlayerFromId(CSteamID steamId)
        {
            return players.FirstOrDefault(p => p.steamId == steamId);
        }

        public static OnlinePlayer PlayerFromId(ulong steamId)
        {
            return players.FirstOrDefault(p => p.steamId.m_SteamID == steamId);
        }

        public static OnlinePlayer PlayerFromId(int netId)
        {
            return players.FirstOrDefault(p => p.netId == netId);
        }

        public static OnlinePlayer PlayerFromIp(IPEndPoint endpoint)
        {
            return players.FirstOrDefault(p => p.endpoint != null && p.endpoint.Equals(endpoint));
        }

        public static void ReceiveData() {
            while (SteamNetworking.IsP2PPacketAvailable(out uint size, 1)) {
                byte[] buffer = new byte[size];
                uint bytesRead;
                CSteamID remoteId;

                if (!SteamNetworking.ReadP2PPacket(buffer, size, out bytesRead, out remoteId, 1))
                    continue;
                
                MemoryStream stream = new MemoryStream(buffer);
                BinaryReader reader = new BinaryReader(stream);

                OnReceiveData(reader, new IPEndPoint(IPAddress.Any, 0), remoteId);
            }
        }

        public static void OnReceiveData(BinaryReader reader, IPEndPoint fromEndpoint, CSteamID fromSteamID) {
            bool isUsingSteam = fromSteamID.IsValid() && fromSteamID.BIndividualAccount();
            // For now, there is no need to distinguish the type of data
            // Only the assigned network id is sent over, so we expect set size
            
            long dataSize = reader.BaseStream.Length - reader.BaseStream.Position;
            if (dataSize != 13)
                throw new Exception($"Received data of invalid size for player info! Got {dataSize} bytes instead of 13");


            PlayerSetup type = (PlayerSetup)reader.ReadByte();
            int netId = reader.ReadInt32();
            CSteamID steamId = new CSteamID(reader.ReadUInt64());

            OnlinePlayer player = PlayerFromId(netId);
            switch (type) {
                case PlayerSetup.RequestJoin:
                    // Hello packet from joining peer

                    if (isUsingSteam)
                        PlayerJoined(fromSteamID);
                    else
                        PlayerJoined(fromEndpoint);
                    
                    byte[] buffer = new byte[13];
                    MemoryStream stream = new MemoryStream(buffer);
                    BinaryWriter writer = new BinaryWriter(stream);
                    
                    // Tell joining peer to create everyone in the server
                    foreach (OnlinePlayer p in players) {
                        stream.Seek(0, SeekOrigin.Begin);
                        writer.Write((byte)PlayerSetup.CreatePlayer);
                        writer.Write(p.netId);
                        writer.Write((ulong)p.steamId);

                        if (isUsingSteam)
                            SteamNetworking.SendP2PPacket(fromSteamID, buffer, 13, EP2PSend.k_EP2PSendReliable, 1);
                        else
                            LocalPeer.Send(buffer, 13, LocalPeer.PacketType.Reliable, LocalPeer.PacketDataType.PlayerInfo);
                    }

                    // Tell them they are now ready
                    stream.Seek(0, SeekOrigin.Begin);
                    writer.Write((byte)PlayerSetup.Ready); 
                    
                    if (isUsingSteam)
                        SteamNetworking.SendP2PPacket(fromSteamID, buffer, 13, EP2PSend.k_EP2PSendReliable, 1);
                    else
                        LocalPeer.Send(buffer, 13, LocalPeer.PacketType.Reliable, LocalPeer.PacketDataType.PlayerInfo);

                    nextPlayerId = nextPlayerId + 1;
                    break;
                
                case PlayerSetup.UpdatePlayer:
                    if (steamId.IsValid()) {
                        if (mePlayer.steamId != steamId) {
                            RainMeadow.Error($"Tried to update self with mismatching Steam ID");
                            return;
                        }
                    } else {
                        RainMeadow.Debug("Cleared steam id");
                        mePlayer.steamId.Clear();
                    }

                    mePlayer.netId = netId;
                    RainMeadow.Debug($"Your new network id is {netId}");

                    nextPlayerId = netId + 1;
                    break;
                
                case PlayerSetup.CreatePlayer:
                    if (player != null) {
                        RainMeadow.Error($"Player with ID {netId} '{player.name}' already exists!");
                        return;
                    }

                    if (steamId.IsValid() && steamId.BIndividualAccount()) {
                        player = new OnlinePlayer(netId, steamId);
                        RainMeadow.Debug($"New player {netId} from {steamId}");
                    } else {
                        // endpoint should be the joining player, not who sent this data
                        // placeholder, not need to fix immediately but still a TODO
                        player = new OnlinePlayer(netId, fromEndpoint);
                        RainMeadow.Debug($"New player {netId} from {fromEndpoint}");
                    }

                    players.Add(player);
                    nextPlayerId = netId + 1;
                    break;
                
                case PlayerSetup.RemovePlayer:
                    if (player == null) {
                        RainMeadow.Error($"Player with ID {netId} does not already exist!");
                        return;
                    }

                    PlayerLeft(player);
                    break;
                
                case PlayerSetup.Ready:
                    // Final message
                    OnlineManager.lobby = new Lobby(LobbyManager.joiningLobbyId, SteamMatchmaking.GetLobbyData(LobbyManager.joiningLobbyId, OnlineManager.MODE_KEY));
                    LobbyManager.manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbyMenu); // We may change to the main lobby screen
                    break;

                case PlayerSetup.Leave:
                    // Local peer is leaving
                    PlayerLeft(player);
                    break;
            }
        }

		public static void RequestPlayerInfo(CSteamID steamLobbyOwner) {
			// Send empty hello packet
            byte[] buffer = new byte[13];
            if (steamLobbyOwner.IsValid() && steamLobbyOwner.BIndividualAccount()) {
                SteamNetworking.SendP2PPacket(steamLobbyOwner, buffer, 13, EP2PSend.k_EP2PSendReliable, 1);
            } else {
                LocalPeer.Send(buffer, 13, LocalPeer.PacketType.Reliable, LocalPeer.PacketDataType.PlayerInfo);
            }
		}
	}
}