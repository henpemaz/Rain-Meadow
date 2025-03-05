using Steamworks;
using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;


namespace RainMeadow
{

    partial class NetIOPlatform {
        static partial void PlatformSteamAvailable(ref bool val) {
            val = SteamManager.Instance.m_bInitialized && SteamUser.BLoggedOn();
        }
    }

    class SteamNetIO : NetIO {
        // public override void SendP2P(OnlinePlayer player, Packet packet, SendType sendType, bool start_conversation = false) {
        //     // base.SendP2P(player, packet, sendType, start_conversation);
            
        //     // RainMeadow.Error("UNIMPLEMENTED");
        // }



        public override void SendSessionData(OnlinePlayer toPlayer)
        {
            try
            {
                OnlineManager.serializer.WriteData(toPlayer);
                var steamNetId = (toPlayer.id as SteamMatchmakingManager.SteamPlayerId).oid;
                unsafe
                {
                    fixed (byte* dataPointer = OnlineManager.serializer.buffer)
                    {
                        SteamNetworkingMessages.SendMessageToUser(ref steamNetId, (IntPtr)dataPointer, (uint)OnlineManager.serializer.Position, Constants.k_nSteamNetworkingSend_Unreliable, 0);
                    }
                }
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                OnlineManager.serializer.EndWrite();
                throw;
            }
                
        }

        override public void RecieveData() {
            // base.RecieveData();
            SteamAPI.RunCallbacks();
            SteamRecieveData();
        }

        public void SteamRecieveData()
        {
            if (MatchmakingManager.currentDomain != MatchmakingManager.MatchMakingDomain.Steam) {
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

                                var fromPlayer = (MatchmakingManager.instances[MatchmakingManager.MatchMakingDomain.Steam] as SteamMatchmakingManager).GetPlayerSteam(message.m_identityPeer.GetSteamID().m_SteamID);
                                if (fromPlayer == null)
                                {
                                    RainMeadow.Error("player not found: " + message.m_identityPeer + " " + message.m_identityPeer.GetSteamID());
                                    continue;
                                }
                                //RainMeadow.Debug($"Receiving message from {fromPlayer}");
                                Marshal.Copy(message.m_pData, OnlineManager.serializer.buffer, 0, message.m_cbSize);
                                OnlineManager.serializer.ReadData(fromPlayer, message.m_cbSize);
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