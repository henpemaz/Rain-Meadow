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

        public static void InitPlayersManager()
        {
            m_SessionRequest = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(SessionRequest);
            UdpPeer.PeerTerminated += (ep) => PlayerLeft(PlayerFromIp(ep));
        }

        public static void Reset()
        {
            if (players != null) {
                foreach (OnlinePlayer player in players) {
                    if (player.isUsingSteam)
                        SteamNetworking.CloseP2PSessionWithUser(player.steamId);
                }
            }
            mePlayer = new OnlinePlayer(1, SteamUser.GetSteamID(), new IPEndPoint(IPAddress.Loopback, 0)) { name = SteamFriends.GetPersonaName() };
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
        public static OnlinePlayer SteamPlayerJoined(CSteamID joiningSteamId)
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

        // Non steam version
        public static OnlinePlayer IpPlayerJoined(IPEndPoint joiningEndpoint)
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
            // Tell the joining player to update their network id
            NetIO.SendP2P(joiningPlayer, new ModifyPlayerPacket(joiningPlayer), SendType.Reliable);

			// Tell the other players to create this player
			foreach (OnlinePlayer player in players) {
                if (player.isMe)
                    continue;
                    
				NetIO.SendP2P(player, new ModifyPlayerListPacket(ModifyPlayerListPacket.Operation.Add, new OnlinePlayer[] {joiningPlayer}), SendType.Reliable);
			}
            
			// Tell joining peer to create everyone in the server
            NetIO.SendP2P(joiningPlayer, new ModifyPlayerListPacket(ModifyPlayerListPacket.Operation.Add, PlayersManager.players.ToArray()), SendType.Reliable);

			// Add player yourself
			players.Add(joiningPlayer);
			nextPlayerId = nextPlayerId + 1;
		}

		private static void PlayerLeft(OnlinePlayer leavingPlayer)
        {
            if (leavingPlayer == null || leavingPlayer.hasLeft)
                return;

            RainMeadow.Debug($"Handling player disconnect:{leavingPlayer}");
            leavingPlayer.hasLeft = true;
            OnlineManager.lobby?.OnPlayerDisconnect(leavingPlayer);
            while (leavingPlayer.HasUnacknowledgedEvents())
            {
                leavingPlayer.AbortUnacknowledgedEvents();
                OnlineManager.lobby?.OnPlayerDisconnect(leavingPlayer);
            }

            RainMeadow.Debug($"Actually removing player:{leavingPlayer}");
            players.Remove(leavingPlayer);

            // Relay to everyone if the leaving player is not using steam
            // If both ends are using steam, the above is already done
            if (OnlineManager.lobby.owner.isMe && !leavingPlayer.isUsingSteam) {
                foreach (OnlinePlayer player in players) {
                    if (player.isMe)
                        continue;
                    
                    NetIO.SendP2P(player, new ModifyPlayerListPacket(ModifyPlayerListPacket.Operation.Remove, new OnlinePlayer[] {leavingPlayer}), SendType.Reliable);
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

        public static OnlinePlayer BestTransferCandidate(OnlineResource onlineResource, Dictionary<OnlinePlayer, PlayerMemebership> subscribers)
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

		public static void RequestPlayerInfo(CSteamID steamLobbyOwnerId) {
            var memory = new MemoryStream(16);
            var writer = new BinaryWriter(memory);
            Packet.Encode(new RequestJoinPacket(), writer, steamLobbyOwnerId, new IPEndPoint(IPAddress.Loopback, UdpPeer.STARTING_PORT));

            if (steamLobbyOwnerId.IsValid() && steamLobbyOwnerId.BIndividualAccount()) {
                SteamNetworking.SendP2PPacket(steamLobbyOwnerId, memory.GetBuffer(), (uint)memory.Position, EP2PSend.k_EP2PSendReliable, 1);
            } else {
                // The IP endpoint of who you are joining with would go here
                UdpPeer.Send(new IPEndPoint(IPAddress.Loopback, UdpPeer.STARTING_PORT), memory.GetBuffer(), (int)memory.Position, UdpPeer.PacketType.Reliable);
            }
		}
	}
}