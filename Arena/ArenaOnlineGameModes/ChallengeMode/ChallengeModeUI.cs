using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI;
using RainMeadow.UI.Components;
using RainMeadow.UI.Pages;
using UnityEngine;
using ArenaMode = RainMeadow.ArenaOnlineGameMode;

namespace RainMeadow.Arena.ArenaOnlineGameModes.ArenaChallengeModeNS
{
    public partial class ArenaChallengeMode : ExternalArenaGameMode
    {
        public TabContainer.Tab? myTab;
        public OnlineArenaChallengeSettingsInterface? arenaChallengeSettingsInterface;

        public override void OnUIEnabled(ArenaOnlineLobbyMenu menu)
        {
            base.OnUIEnabled(menu);
            myTab = new(menu, menu.arenaMainLobbyPage.tabContainer);
            myTab.AddObjects(
                arenaChallengeSettingsInterface = new OnlineArenaChallengeSettingsInterface(
                    (ArenaMode)OnlineManager.lobby.gameMode,
                    this,
                    myTab.menu,
                    myTab,
                    new(0, 0),
                    menu.arenaMainLobbyPage.tabContainer.size
                )
            );
            menu.arenaMainLobbyPage.tabContainer.AddTab(
                myTab,
                menu.Translate("Challenge Settings")
            );
        }

        public override void OnUIDisabled(ArenaOnlineLobbyMenu menu)
        {
            base.OnUIDisabled(menu);
            arenaChallengeSettingsInterface?.OnShutdown();
            if (myTab != null)
                menu.arenaMainLobbyPage.tabContainer.RemoveTab(myTab);
            myTab = null;
        }

        public override void OnUIUpdate(ArenaOnlineLobbyMenu menu)
        {
            base.OnUIUpdate(menu);
        }

        public override void OnUIShutDown(ArenaOnlineLobbyMenu menu)
        {
            base.OnUIShutDown(menu);
            arenaChallengeSettingsInterface?.OnShutdown();
        }
    }
}
