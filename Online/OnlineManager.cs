using Steamworks;
using System;
using System.Collections.Generic;
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
                foreach (var subscription in subscriptions)
                {
                    subscription.Update(0);
                }

                foreach (var player in lobby.players)
                {
                    player.SendData();
                }
            }
        }

        internal static OnlinePlayer PlayerFromId(ulong v)
        {
            var id = new CSteamID(v);
            return lobby?.players.FirstOrDefault(p => p.id == id);
        }
    }
}
