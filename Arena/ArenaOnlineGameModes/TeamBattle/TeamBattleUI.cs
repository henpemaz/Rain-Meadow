using Menu;
using Menu.Remix;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components;
using HarmonyLib;
using RainMeadow.UI;
using RainMeadow.UI.Pages;
using System.Runtime.CompilerServices;

namespace RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle
{
    public partial class TeamBattleMode : ExternalArenaGameMode
    {
        public TabContainer.Tab? myTab;
        public ConditionalWeakTable<ArenaPlayerBox, TeamBattlePlayerBox> playerBoxes = new();
        public int winningTeam, martyrsSpawn, outlawsSpawn, dragonslayersSpawn, chieftainsSpawn, roundSpawnPointCycler;

        public string martyrsTeamName = RainMeadow.rainMeadowOptions.MartyrTeamName.Value;
        public string outlawTeamNames = RainMeadow.rainMeadowOptions.OutlawsTeamName.Value;
        public string dragonSlayersTeamNames = RainMeadow.rainMeadowOptions.DragonSlayersTeamName.Value;
        public string chieftainsTeamNames = RainMeadow.rainMeadowOptions.ChieftainTeamName.Value;

        public float lerp = RainMeadow.rainMeadowOptions.TeamColorLerp.Value;
        public bool teamComboBoxLastHeld;
        public Dictionary<int, string> teamNameDictionary = new Dictionary<int, string>
        {
            { 0, RainMeadow.rainMeadowOptions.MartyrTeamName.Value },
            { 1, RainMeadow.rainMeadowOptions.OutlawsTeamName.Value },
            { 2, RainMeadow.rainMeadowOptions.DragonSlayersTeamName.Value },
            { 3, RainMeadow.rainMeadowOptions.ChieftainTeamName.Value }
        };
        public enum TeamMappings
        {
            martyrsTeamName,
            outlawTeamName,
            dragonslayersTeamName,
            chieftainsTeamName
        }

        public Dictionary<int, string> TeamMappingsDictionary = new Dictionary<int, string>
        {
            { 0, "SaintA" },
            { 1, "OutlawA" },
            { 2, "DragonSlayerA" },
            { 3, "ChieftainA" }
    };

        public static Dictionary<int, Color> TeamColors = new Dictionary<int, Color>
        {
    { 0, GetColorFromHex("#FF7F7F") },
    { 1, GetColorFromHex("#FFFF7F") },
    { 2, GetColorFromHex("#7FFF7F") },
    { 3,  GetColorFromHex("#7F7FFF") }
    };
        public void ArenaSettingsInit()
        {
            winningTeam = -1;
            martyrsSpawn = 0;
            outlawsSpawn = 0;
            dragonslayersSpawn = 0;
            chieftainsSpawn = 0;
            roundSpawnPointCycler = 0;
        }
        public override void OnUIEnabled(ArenaOnlineLobbyMenu menu)
        {
            base.OnUIEnabled(menu);
            ArenaSettingsInit();
            myTab = menu.arenaMainLobbyPage.tabContainer.AddTab("Team Settings");
            myTab.AddObjects(new OnlineTeamBattleSettingsInterface((ArenaMode)OnlineManager.lobby.gameMode, this, myTab.menu, myTab, new(0, 0)));
            menu.arenaMainLobbyPage.tabContainer.tabButtonContainer.GoPrevPage();
        }
        public override void OnUIDisabled(ArenaOnlineLobbyMenu menu)
        {
            base.OnUIDisabled(menu);
            if (myTab != null) menu.arenaMainLobbyPage.tabContainer.RemoveTab(myTab);
            myTab = null;
            foreach (ArenaPlayerBox playerBox in menu.arenaMainLobbyPage.playerDisplayer?.GetSpecificButtons<ArenaPlayerBox>() ?? [])
            {
                if (!playerBoxes.TryGetValue(playerBox, out TeamBattlePlayerBox teamBox)) continue;
                playerBox.ClearMenuObject(teamBox);
                playerBoxes.Remove(playerBox);
            }
        }
        public override void OnUIUpdate(ArenaOnlineLobbyMenu menu)
        {
            base.OnUIUpdate(menu);
            foreach (ArenaPlayerBox playerBox in menu.arenaMainLobbyPage.playerDisplayer?.GetSpecificButtons<ArenaPlayerBox>() ?? [])
            {
                ArenaTeamClientSettings? teamSettings = ArenaHelpers.GetDataSettings<ArenaTeamClientSettings>(playerBox.profileIdentifier);
                playerBox.showRainbow = teamSettings?.team == winningTeam && winningTeam != -1;
                string symbolName = teamSettings != null? TeamMappingsDictionary[teamSettings.team] : "pixel";
                if (!playerBoxes.TryGetValue(playerBox, out TeamBattlePlayerBox teamBox))
                {
                    teamBox = new(playerBox.menu, playerBox, new(0, 0), symbolName);
                    playerBox.subObjects.Add(teamBox);
                    playerBoxes.Add(playerBox, teamBox);
                }
                else teamBox.teamSymbol.SetElementByName(symbolName);
                teamBox.teamColor = teamSettings != null ? TeamColors[teamSettings.team] : Color.black;

            }
        }
        public override DialogNotify AddGameModeInfo(Menu.Menu menu)
        {
            return new DialogNotify(menu.LongTranslate("Choose a faction. Last team standing wins."), new Vector2(500f, 400f), menu.manager, () => { menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed); });
        }
        public static Color GetColorFromHex(string hexCode)
        {
            Color color;
            // TryParseHtmlString returns true if the conversion was successful
            if (ColorUtility.TryParseHtmlString(hexCode, out color))
            {
                return color;
            }
            else
            {
                Debug.LogError("Invalid hex code: " + hexCode + ". Returning default color.");
                return Color.magenta; // Or any default/error color you prefer
            }
        }
    }
}
