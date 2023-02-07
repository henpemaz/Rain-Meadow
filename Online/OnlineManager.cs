using Steamworks;
using System;
using System.Collections.Generic;
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


                /// if lobby owner
                ///     if lobby tickrate ticks this turn
                ///         send lobby state to subscribers
                ///

                foreach (var subscription in subscriptions)
                {
                    subscription.Update();
                }

                ///
                /// for each player in the lobby
                ///     fire outgoing playerevents
                ///     do actually send things to players
                foreach (var player in lobby.players)
                {
                    player.SendFrame();
                }
            }
        }
    }
}
