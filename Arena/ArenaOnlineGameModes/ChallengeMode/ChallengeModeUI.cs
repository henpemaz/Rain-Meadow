using RainMeadow.UI;
using RainMeadow.UI.Components;

namespace RainMeadow.Arena.ArenaOnlineGameModes.ArenaChallengeModeNS
{
    public partial class ArenaChallengeMode
    {
        public TabContainer.Tab? myTab;
        public OnlineArenaChallengeSettingsInterface? arenaChallengeSettingsInterface;

        public override void OnUIEnabled(ArenaOnlineLobbyMenu menu)
        {
            base.OnUIEnabled(menu);
            myTab = new(menu, menu.arenaMainLobbyPage.tabContainer);
            myTab.AddObjects(
                arenaChallengeSettingsInterface = new OnlineArenaChallengeSettingsInterface(
                    (ArenaOnlineGameMode)OnlineManager.lobby.gameMode,
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

        public override void OnUIShutDown(ArenaOnlineLobbyMenu menu)
        {
            base.OnUIShutDown(menu);
            arenaChallengeSettingsInterface?.OnShutdown();
        }
    }
}
