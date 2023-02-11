using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    // Static/singleton class for online features and callbacks
    // is a mainloopprocess so update bound to game update? worth it? idk
    public class OnlineManager : MainLoopProcess {

        public static string CLIENT_KEY = "client";
        public static string CLIENT_VAL = "Meadow_" + RainMeadow.MeadowVersionStr;
        public static string NAME_KEY = "name";

        public static CSteamID me;
        public static OnlinePlayer mePlayer;
        public static Lobby lobby;
        internal static Serializer serializer = new Serializer(16000);

        public static LobbyManager lobbyManager;
        internal static List<Subscription> subscriptions;

        public OnlineManager(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.OnlineManager)
        {
            me = SteamUser.GetSteamID();
            mePlayer = new OnlinePlayer(me);
            lobbyManager = new LobbyManager();

            framesPerSecond = 20;

            RainMeadow.Debug("OnlineManager Created");
        }

        public override void Update()
        {
            base.Update();
            if(lobby != null)
            {
                ReceiveData();
                foreach (var subscription in subscriptions)
                {
                    subscription.Update(ts);
                }

                foreach (var player in lobby.players)
                {
                    SendData(player);
                }
            }
        }

        internal static OnlinePlayer PlayerFromId(ulong v)
        {
            var id = new CSteamID(v);
            return lobby?.players.FirstOrDefault(p => p.id == id);
        }

        internal static OnlinePlayer PlayerFromId(CSteamID id)
        {
            return lobby?.players.FirstOrDefault(p => p.id == id);
        }

        internal void ReceiveData()
        {
            lock (serializer)
            {
                int n = 1;
                IntPtr[] messages = new IntPtr[32];

                while (n > 0)
                {
                    n = SteamNetworkingMessages.ReceiveMessagesOnChannel(0, messages, messages.Length);
                    for (int i = 0; i < n; i++)
                    {
                        var message = SteamNetworkingMessage_t.FromIntPtr(messages[i]);
                        var fromPlayer = PlayerFromId(message.m_identityPeer.GetSteamID());
                        if (fromPlayer == null)
                        {
                            RainMeadow.Error("player not found: " + message.m_identityPeer + " " + message.m_identityPeer.GetSteamID());
                            SteamNetworkingMessage_t.Release(messages[i]);
                            continue;
                        }
                        serializer.BeginRead();

                        serializer.ReadHeaders(fromPlayer);

                        int ne = serializer.BeginReadEvents();
                        for (int ie = 0; ie < ne; ie++)
                        {
                            ProcessIncomingEvent(serializer.ReadEvent(), fromPlayer);
                        }

                        int ns = serializer.BeginReadStates();
                        for (int ist = 0; ist < ns; ist++)
                        {
                            ProcessIncomingState(serializer.ReadState(), fromPlayer);
                        }

                        serializer.EndRead();
                        SteamNetworkingMessage_t.Release(messages[i]);
                    }
                }
                
                serializer.Free();
            }
        }

        private void ProcessIncomingEvent(PlayerEvent playerEvent, OnlinePlayer fromPlayer)
        {
            if(IsNewer(playerEvent.eventId, fromPlayer.lastIncomingEvent))
            {
                fromPlayer.lastIncomingEvent = playerEvent.eventId;
                fromPlayer.needsAck = true;
                playerEvent.Process();
            }
        }

        private bool IsNewer(ulong eventId, ulong lastIncomingEvent)
        {
            var delta = eventId - lastIncomingEvent;
            return delta != 0 && delta < ulong.MaxValue / 2; // closer through one side than the other
        }

        private void ProcessIncomingState(ResourceState resourceState, OnlinePlayer fromPlayer)
        {
            
        }

        internal void SendData(OnlinePlayer toPlayer)
        {
            lock (serializer)
            {
                serializer.BeginWrite();

                serializer.WriteHeaders(toPlayer);

                serializer.BeginWriteEvents();
                foreach (var e in toPlayer.OutgoingEvents)
                {
                    if (!serializer.CanFit(e)) throw new IOException("no buffer space for events");
                    serializer.WriteEvent(e);
                }
                serializer.EndWriteEvents();

                serializer.BeginWriteStates();
                while (toPlayer.OutgoingStates.Count > 1 && serializer.CanFit(toPlayer.OutgoingStates.Peek()))
                {
                    var s = toPlayer.OutgoingStates.Dequeue();
                    serializer.WriteState(s);
                }
                // todo handle states overflow, planing a packet for maximum size and least stale states
                serializer.EndWriteStates();

                serializer.EndWrite();

                unsafe
                {
                    fixed (byte* ptr = serializer.buffer)
                    {
                        SteamNetworkingMessages.SendMessageToUser(ref toPlayer.oid, (IntPtr)ptr, (uint)serializer.Position, 0, 0);
                    }
                }

                serializer.Free();
            }
        }
    }
}
