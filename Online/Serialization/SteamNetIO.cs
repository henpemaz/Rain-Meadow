using Steamworks;
using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;


namespace RainMeadow
{
    class SteamNetIO : LANNetIO {
        public override void SendP2P(OnlinePlayer player, Packet packet, SendType sendType) {
            base.SendP2P(player, packet, sendType);

            if (MatchmakingManager.currentMatchMaker != MatchmakingManager.MatchMaker.Steam) {
                return;
            }

            if (player.id is SteamMatchmakingManager.SteamPlayerId playerid) {
                var steamNetId = playerid.oid;
                unsafe {
                    fixed (byte* dataPointer = OnlineManager.serializer.buffer) {
                        SteamNetworkingMessages.SendMessageToUser(ref steamNetId, (IntPtr)dataPointer, (uint)OnlineManager.serializer.Position, Constants.k_nSteamNetworkingSend_Unreliable, 0);
                    }
                }
            } else {
                RainMeadow.Error($"SendP2P failed because player.id from ({player.id.name}) is not a SteamPlayerId");
                return;
            }
        }

        override public void Update() {
            SteamAPI.RunCallbacks();
            base.Update();
        }

        override public void RecieveData()
        {

            base.RecieveData();

            if (MatchmakingManager.currentMatchMaker != MatchmakingManager.MatchMaker.Steam) {
                return;
            }

            lock (OnlineManager.serializer)
            {
                int n;
                IntPtr[] messages = new IntPtr[32];
                do // process in batches
                {
                    n = SteamNetworkingMessages.ReceiveMessagesOnChannel(0, messages, messages.Length);
                    for (int i = 0; i < n; i++)
                    {
                        var message = SteamNetworkingMessage_t.FromIntPtr(messages[i]);
                        try
                        {
                            if (OnlineManager.lobby != null)
                            {

                                if (MatchmakingManager.currentInstance is SteamMatchmakingManager manager) {
                                    var fromPlayer = (MatchmakingManager.currentInstance as SteamMatchmakingManager).GetPlayerSteam(message.m_identityPeer.GetSteamID().m_SteamID);
                                    if (fromPlayer != null) {
                                        // RainMeadow.Error("player not found: " + message.m_identityPeer + " " + message.m_identityPeer.GetSteamID());
                                        continue;
                                    }

                                    //RainMeadow.Debug($"Receiving message from {fromPlayer}");
                                    Marshal.Copy(message.m_pData, OnlineManager.serializer.buffer, 0, message.m_cbSize);
                                    OnlineManager.serializer.ReadData(fromPlayer, message.m_cbSize);
                                } else {
                                    RainMeadow.Error("MatchmakingManager is not SteamMatchmakingManager");
                                }
                                
                                

                            }
                        }
                        catch (Exception e)
                        {
                            RainMeadow.Error("Error reading packet from player : " + message.m_identityPeer.GetSteamID());
                            RainMeadow.Error(e);
                            OnlineManager.serializer.EndRead();
                            //throw;
                        }
                        finally
                        {
                            SteamNetworkingMessage_t.Release(messages[i]);
                        }
                    }
                }
                while (n > 0);
            }
        }
    }

}