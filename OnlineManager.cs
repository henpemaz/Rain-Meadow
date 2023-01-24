using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    public partial class OnlineManager : MainLoopProcess {

        public static string CLIENT_KEY = "client";
        public static string CLIENT_VAL = "Meadow";
        public static string NAME_KEY = "name";

        public static OnlineManager instance;
        public OnlinePlayer me;
        public Lobby lobby;
        public OnlineSession session;

        public bool isOnlineSession;

        public OnlineManager(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.OnlineManager)
        {
            instance = this;
            SetupLobbyCallbacks();

            me = new OnlinePlayer(SteamUser.GetSteamID());
            RainMeadow.Debug("OnlineManager Created");
        }


        public override void Update()
        {
            base.Update();

        }


        internal void BroadcastEvent(LobbyEvent lobbyEvent)
        {
            //throw new NotImplementedException();
        }
    }
}
