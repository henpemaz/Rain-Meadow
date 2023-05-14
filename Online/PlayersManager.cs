using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public static class PlayersManager
    {
        private static Callback<SteamNetworkingMessagesSessionRequest_t> m_SessionRequest;

        public static CSteamID me;
        public static OnlinePlayer mePlayer;
        public static List<OnlinePlayer> players;
        public static void InitPlayersManager()
        {
            m_SessionRequest = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(SessionRequest);
        }

        public static void Reset()
        {
            me = SteamUser.GetSteamID();
            mePlayer = new OnlinePlayer(me) { isMe = true, name = SteamFriends.GetPersonaName() };
            players = new List<OnlinePlayer>() { mePlayer };
        }

        public static void UpdatePlayersList(CSteamID cSteamID)
        {
            try
            {
                RainMeadow.DebugMe();
                var n = SteamMatchmaking.GetNumLobbyMembers(cSteamID);
                var oldplayers = players.Select(p => p.id).ToArray();
                var newplayers = new CSteamID[n];
                for (int i = 0; i < n; i++)
                {
                    newplayers[i] = SteamMatchmaking.GetLobbyMemberByIndex(cSteamID, i);
                }
                foreach (var p in oldplayers)
                {
                    if (!newplayers.Contains(p)) PlayerLeft(p);
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

        private static void PlayerJoined(CSteamID p)
        {
            RainMeadow.Debug($"PlayerJoined:{p} - {SteamFriends.GetFriendPersonaName(p)}");
            if (p == me) return;
            SteamFriends.RequestUserInformation(p, true);
            players.Add(new OnlinePlayer(p));
        }

        private static void PlayerLeft(CSteamID p)
        {
            RainMeadow.Debug($"{p} - {SteamFriends.GetFriendPersonaName(p)}");

            if(players.FirstOrDefault(op => op.id == p) is OnlinePlayer player)
            {
                RainMeadow.Debug($"Handling player disconnect:{player}");
                player.hasLeft = true;
                OnlineManager.lobby?.OnPlayerDisconnect(player);
                while (player.HasUnacknoledgedEvents())
                {
                    player.AbortUnacknoledgedEvents();
                    OnlineManager.lobby?.OnPlayerDisconnect(player);
                }
                RainMeadow.Debug($"Actually removing player:{player}");
                players.Remove(player);
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
                    if (players.FirstOrDefault(op => op.id == id) is OnlinePlayer p)
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

        public static OnlinePlayer BestTransferCandidate(OnlineResource onlineResource, Dictionary<OnlinePlayer, ParticipantInResource> subscribers)
        {
            if (subscribers.Keys.Contains(mePlayer)) return mePlayer;
            // todo pick by ping?
            if (subscribers.Count < 1) return null;
            return subscribers.First().Key;
        }

        public static OnlinePlayer PlayerFromId(CSteamID id)
        {
            return players.FirstOrDefault(p => p.id == id);
        }

        public static OnlinePlayer PlayerFromId(ulong id)
        {
            return players.FirstOrDefault(p => p.id.m_SteamID == id);
        }
    }
}