using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public static class PlayersManager
    {
        private static Callback<SteamNetworkingMessagesSessionRequest_t> m_SessionRequest;
        public static void InitPlayersManager()
        {
            m_SessionRequest = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(SessionRequest);
        }


        public static void UpdatePlayersList(CSteamID cSteamID)
        {
            try
            {
                RainMeadow.DebugMe();
                var n = SteamMatchmaking.GetNumLobbyMembers(cSteamID);
                var oldplayers = OnlineManager.players.Select(p => p.id).ToArray();
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
            if (p == OnlineManager.me) return;
            SteamFriends.RequestUserInformation(p, true);
            OnlineManager.players.Add(new OnlinePlayer(p));
        }

        private static void PlayerLeft(CSteamID p)
        {
            // todo if lobby owner leaves, update lobby resources accordingly

            // todo if resource owner leaves and I'm super, coordinate transfer

            RainMeadow.Debug($"PlayerLeft:{p} - {SteamFriends.GetFriendPersonaName(p)}");
            OnlineManager.players.RemoveAll(op => op.id == p);
        }

        private static void SessionRequest(SteamNetworkingMessagesSessionRequest_t param)
        {
            try
            {
                var id = new CSteamID(param.m_identityRemote.GetSteamID64());
                RainMeadow.Debug("session request from " + id);
                if (OnlineManager.lobby is { })
                {
                    foreach (var p in OnlineManager.players)
                    {
                        if (id == p.id)
                        {
                            RainMeadow.Debug("accepted session from " + p.name);
                            SteamNetworkingMessages.AcceptSessionWithUser(ref param.m_identityRemote);
                            return;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        internal static OnlinePlayer BestTransferCandidate(OnlineResource onlineResource, List<OnlinePlayer> subscribers)
        {
            if (subscribers.Contains(OnlineManager.mePlayer)) return OnlineManager.mePlayer;
            if (subscribers.Count < 1) return null;
            return subscribers[0];
        }
    }
}