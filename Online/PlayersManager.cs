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
                    if (!oldplayers.Contains(p)) SteamPlayerJoined(p);
                }
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        // Steam version - everyone using steam gets this event automaticly
        static OnlinePlayer SteamPlayerJoined(CSteamID joiningSteamId)
        {
            OnlinePlayer joiningPlayer = players.Find(p => p.steamId == joiningSteamId);
            if (joiningPlayer != null)
                // Ignore me! I am already here!
                return joiningPlayer;
            
            RainMeadow.Debug($"Steam {SteamFriends.GetFriendPersonaName(joiningSteamId)} ({joiningSteamId})");
            
            // Lobby owner
            if (OnlineManager.lobby != null && OnlineManager.lobby.owner.isMe) {
                joiningPlayer = new OnlinePlayer(PlayersManager.nextPlayerId, joiningSteamId);
				PlayerJoined(joiningPlayer);
                return joiningPlayer;
            }

            return null;
        }

        // Non steam version - IF THIS IS NOT USED FOR DEBUG TODO: lobby owner needs to broadcast this
        static OnlinePlayer IpPlayerJoined(IPEndPoint joiningEndpoint)
        {
            OnlinePlayer joiningPlayer = PlayerFromIp(joiningEndpoint);
            if (joiningPlayer != null)
                // Ignore me! I am already here!
                return joiningPlayer;
            

            RainMeadow.Debug($"Non-Steam {joiningEndpoint}");
            
            // Lobby owner
            if (OnlineManager.lobby != null && OnlineManager.lobby.owner.isMe)
			{
                joiningPlayer = new OnlinePlayer(PlayersManager.nextPlayerId, joiningEndpoint);
				PlayerJoined(joiningPlayer);
                return joiningPlayer;
			}

            return null;
		}

		static void PlayerJoined(OnlinePlayer joiningPlayer) {
			byte[] buffer = new byte[13];
			MemoryStream stream = new MemoryStream(buffer);
			BinaryWriter writer = new BinaryWriter(stream);

			// Tell the joining player to update their network id
			writer.Write((byte)PlayerSetup.UpdatePlayer);
			writer.Write(joiningPlayer.netId);
			writer.Write((ulong)joiningPlayer.steamId);

			NetIO.SendP2P(joiningPlayer, buffer, 13, SendType.Reliable, PacketDataType.PlayerInfo);

			// Tell the other players to create this player
			stream.Seek(0, SeekOrigin.Begin);
			writer.Write((byte)PlayerSetup.CreatePlayer);

			foreach (OnlinePlayer player in players) {
                if (player.isMe)
                    continue;

				NetIO.SendP2P(player, buffer, 13, SendType.Reliable, PacketDataType.PlayerInfo);
			}

			// Tell joining peer to create everyone in the server
			foreach (OnlinePlayer player in players) {
				stream.Seek(0, SeekOrigin.Begin);
				writer.Write((byte)PlayerSetup.CreatePlayer);
				writer.Write(player.netId);
				writer.Write((ulong)player.steamId);

				NetIO.SendP2P(joiningPlayer, buffer, 13, SendType.Reliable, PacketDataType.PlayerInfo);

				RainMeadow.Debug($"Sending Create Player {player}");
			}

			// Add player yourself
			players.Add(joiningPlayer);
			nextPlayerId++;
		}

		private static void PlayerLeft(OnlinePlayer leavingPlayer)
        {
            if (leavingPlayer == null || leavingPlayer.hasLeft)
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

            // Relay to everyone if the leaving player is not using steam
            // If both ends are using steam, the above is already done
            if (OnlineManager.lobby.owner.isMe && leavingPlayer.isUsingSteam) {
                byte[] buffer = new byte[13];
                MemoryStream stream = new MemoryStream(buffer);
                BinaryWriter writer = new BinaryWriter(stream);

                writer.Write((byte)PlayerSetup.RemovePlayer);
                writer.Write(leavingPlayer.netId);
                writer.Write((ulong)leavingPlayer.steamId);

                foreach (OnlinePlayer player in players) {
                    if (player.isMe)
                        continue;
                    NetIO.SendP2P(player, buffer, 13, SendType.Reliable, PacketDataType.PlayerInfo);
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

        // Use when testing for a player
        public static OnlinePlayer TryGetPlayer(int netId)
        {
            return players.FirstOrDefault(p => p.netId == netId);
        }

        // Use when expecting a player in return
        public static OnlinePlayer PlayerFromId(int netId)
        {
            OnlinePlayer player = TryGetPlayer(netId);
            if (netId > 0 && player is null)
                throw new Exception("Could not find player from given network id: " + netId);
            return player;
        }

        public static OnlinePlayer PlayerFromId(CSteamID steamId)
        {
            return players.FirstOrDefault(p => p.steamId == steamId);
        }

        public static OnlinePlayer PlayerFromId(ulong steamId)
        {
            return players.FirstOrDefault(p => p.steamId.m_SteamID == steamId);
        }

        public static OnlinePlayer PlayerFromIp(IPEndPoint endpoint)
        {
            return players.FirstOrDefault(p => p.endpoint != null && p.endpoint.Equals(endpoint));
        }

        public static void OnReceiveData(BinaryReader reader, IPEndPoint fromEndpoint, CSteamID fromSteamID) {
            bool isUsingSteam = fromSteamID.IsValid() && fromSteamID.BIndividualAccount();
            // For now, there is no need to distinguish the type of data
            // Only the assigned network id is sent over, so we expect set size
            
            long dataSize = reader.BaseStream.Length - reader.BaseStream.Position;
            if (dataSize != 13)
                throw new Exception($"Received data of invalid size for player info! Got {dataSize} bytes instead of 13");


            PlayerSetup type = (PlayerSetup)reader.ReadByte();
            RainMeadow.Debug(type);
            int netId = reader.ReadInt32();
            CSteamID steamId = new CSteamID(reader.ReadUInt64());

            OnlinePlayer player;
            switch (type) {
                case PlayerSetup.RequestJoin:
                    // Hello packet from joining peer

                    OnlinePlayer joiningPlayer = isUsingSteam ? SteamPlayerJoined(fromSteamID) : IpPlayerJoined(fromEndpoint);
                    
                    byte[] buffer = new byte[13];
                    MemoryStream stream = new MemoryStream(buffer);
                    BinaryWriter writer = new BinaryWriter(stream);

                    // Tell them they are now ready
                    stream.Seek(0, SeekOrigin.Begin);
                    writer.Write((byte)PlayerSetup.Ready); 
                    
                    NetIO.SendP2P(joiningPlayer, buffer, 13, SendType.Reliable, PacketDataType.PlayerInfo);

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
                    player = TryGetPlayer(netId);
                    if (player != null) {
                        RainMeadow.Error($"Player with ID {netId} '{player.name}' already exists!");
                        return;
                    }

                    if (steamId.IsValid() && steamId.BIndividualAccount()) {
                        player = new OnlinePlayer(netId, steamId, fromEndpoint);
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
                    player = PlayerFromId(netId);
                    PlayerLeft(player);
                    break;
                
                case PlayerSetup.Ready:
                    // Final message
                    OnlineManager.lobby = new Lobby(LobbyManager.joiningLobbyId, SteamMatchmaking.GetLobbyData(LobbyManager.joiningLobbyId, OnlineManager.MODE_KEY));
                    LobbyManager.GoToMenu(); // We may change to the main lobby screen
                    break;

                case PlayerSetup.Leave:
                    // Local peer is leaving
                    player = PlayerFromId(netId);
                    PlayerLeft(player);
                    break;
            }
        }

		public static void RequestPlayerInfo(CSteamID steamLobbyOwnerId) {
			// Send empty hello packet
            byte[] buffer = new byte[13];
            if (steamLobbyOwnerId.IsValid() && steamLobbyOwnerId.BIndividualAccount()) {
                SteamNetworking.SendP2PPacket(steamLobbyOwnerId, buffer, 13, EP2PSend.k_EP2PSendReliable, 1);
            } else {
                LocalPeer.Send(buffer, 13, LocalPeer.PacketType.Reliable, PacketDataType.PlayerInfo);
            }
		}
	}
}