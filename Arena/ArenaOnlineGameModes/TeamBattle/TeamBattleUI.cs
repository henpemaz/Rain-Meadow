using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Menu;
using RainMeadow.UI;
using RainMeadow.UI.Components;
using UnityEngine;

namespace RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle
{
    public partial class TeamBattleMode
    {
        public TabContainer.Tab? myTab;
        public OnlineTeamBattleSettingsInterface? myTeamBattleSettingInterface;
        public ConditionalWeakTable<ArenaPlayerBox, TeamBattlePlayerBox> playerBoxes = new();

        public int martyrsSpawn;
        public int outlawsSpawn;
        public int dragonslayersSpawn;
        public int chieftainsSpawn;
        public int roundSpawnPointCycler;

        public string martyrsTeamName = RainMeadow.rainMeadowOptions.MartyrTeamName.Value;
        public string outlawTeamNames = RainMeadow.rainMeadowOptions.OutlawsTeamName.Value;
        public string dragonSlayersTeamNames = RainMeadow.rainMeadowOptions.DragonSlayersTeamName.Value;
        public string chieftainsTeamNames = RainMeadow.rainMeadowOptions.ChieftainTeamName.Value;

        public float lerp = RainMeadow.rainMeadowOptions.TeamColorLerp.Value;

        public enum TeamSpawnPoints
        {
            martyrsTeamName,
            outlawTeamName,
            dragonslayersTeamName,
            chieftainsTeamName
        }

        public Dictionary<int, string> teamNames = new()
        {
            { 0, RainMeadow.rainMeadowOptions.MartyrTeamName.Value },
            { 1, RainMeadow.rainMeadowOptions.OutlawsTeamName.Value },
            { 2, RainMeadow.rainMeadowOptions.DragonSlayersTeamName.Value },
            { 3, RainMeadow.rainMeadowOptions.ChieftainTeamName.Value }
        };
        public Dictionary<int, string> teamIcons = new()
        {
            { 0, "SaintA" },
            { 1, "OutlawA" },
            { 2, "DragonSlayerA" },
            { 3, "ChieftainA" }
        };
        public Dictionary<int, Color> teamColors = new()
        {
            { 0, RainMeadow.rainMeadowOptions.MartyrTeamColor.Value },
            { 1, RainMeadow.rainMeadowOptions.OutlawsTeamColor.Value },
            { 2, RainMeadow.rainMeadowOptions.DragonSlayersTeamColor.Value },
            { 3, RainMeadow.rainMeadowOptions.ChieftainTeamColor.Value }
        };

        public void ArenaSettingsInit()
        {
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
            //myTab = menu.arenaMainLobbyPage.tabContainer.AddTab(menu.Translate("Team Settings"));
            myTab = new(menu, menu.arenaMainLobbyPage.tabContainer);
            myTab.AddObjects(myTeamBattleSettingInterface = new OnlineTeamBattleSettingsInterface((ArenaOnlineGameMode)OnlineManager.lobby.gameMode, this, myTab.menu, myTab, new(0, 0), menu.arenaMainLobbyPage.tabContainer.size));
            menu.arenaMainLobbyPage.tabContainer.AddTab(myTab, menu.Translate("Team Settings"));
        }

        public override void OnUIDisabled(ArenaOnlineLobbyMenu menu)
        {
            base.OnUIDisabled(menu);
            myTeamBattleSettingInterface?.OnShutdown();
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
            foreach (ButtonScroller.IPartOfButtonScroller button in menu.arenaMainLobbyPage.playerDisplayer?.buttons ?? [])
            {
                if (button is ArenaPlayerBox playerBox)
                {
                    ArenaTeamClientSettings? teamSettings = ArenaHelpers.GetDataSettings<ArenaTeamClientSettings>(playerBox.profileIdentifier);
                    playerBox.showRainbow = BestTeamIndexes.Count == 1 && teamSettings?.team == BestTeamIndexes[0];
                    string symbolName = teamSettings != null ? teamIcons[teamSettings.team] : "pixel";
                    if (!playerBoxes.TryGetValue(playerBox, out TeamBattlePlayerBox teamBox) && playerBox.profileIdentifier != OnlineManager.lobby.owner)
                    {
                        teamBox = new(playerBox.menu, playerBox, new(0, 0), symbolName);
                        playerBox.subObjects.Add(teamBox);
                        playerBoxes.Add(playerBox, teamBox);
                    }
                    else 
                    if (playerBox.profileIdentifier != OnlineManager.lobby.owner) {
                    teamBox.teamSymbol.SetElementByName(symbolName);
                    teamBox.teamColor = teamSettings != null ? teamColors[teamSettings.team] : Color.black;
                    }
                }
                if (button is ArenaPlayerSmallBox smallBox)
                {
                    ArenaTeamClientSettings? teamSettings = ArenaHelpers.GetDataSettings<ArenaTeamClientSettings>(smallBox.profileIdentifier);
                    smallBox.baseColor = teamSettings != null ? teamColors[teamSettings.team].ToHSL() : null;
                }
            }
        }

        public override void OnUIShutDown(ArenaOnlineLobbyMenu menu)
        {
            base.OnUIShutDown(menu);
            myTeamBattleSettingInterface?.OnShutdown();
        }

        public override Color GetPortraitColor(
            ArenaOnlineGameMode arenaOnline,
            OnlinePlayer? player,
            Color origColor)
        {
            Color col = base.GetPortraitColor(arenaOnline, player, origColor);
            ArenaTeamClientSettings? teamClientSettings = ArenaHelpers.GetDataSettings<ArenaTeamClientSettings>(player);
            if (teamClientSettings != null && teamColors.ContainsKey(teamClientSettings.team))
                col = Color.Lerp(col, teamColors[teamClientSettings.team], lerp);
            return col;
        }

        public override bool DidPlayerWinRainbow(ArenaOnlineGameMode arenaOnline, OnlinePlayer player)
        {
            ArenaTeamClientSettings? teamSettings = ArenaHelpers.GetDataSettings<ArenaTeamClientSettings>(player);
            return base.DidPlayerWinRainbow(arenaOnline, player)
                || BestTeamIndexes.Count == 1 && teamSettings?.team == BestTeamIndexes[0];
        }

        public override Dialog AddGameModeInfo(ArenaOnlineGameMode arenaOnline, Menu.Menu menu)
        {
            return new DialogNotify(menu.LongTranslate("Choose a faction. Last team standing wins."), new Vector2(500f, 400f), menu.manager, () => { menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed); });
        }

        /// <inheritdoc/>
        public override string GetResultText(
            ArenaOnlineGameMode arenaOnline,
            PlayerResultMenu resultMenu,
            out bool isSpecific)
        {
            isSpecific = true;
            string nonSpecificText = resultMenu is MultiplayerResults
                ? resultMenu.Translate("SESSION ENDED!")
                : resultMenu.Translate("GAME OVER");;

            if (BestTeamIndexes.Count == 0)
            {
                isSpecific = false;
                return nonSpecificText;
            }
            if (BestTeamIndexes.Count > 1)
                return resultMenu.Translate("IT'S A DRAW!");

            string filteredTeamName = MatchmakingManager.currentInstance.FilterTeamName(
                teamNames[0].ToUpper()
            );

            return resultMenu.Translate("<TEAMNAME> WINS!")
                .Replace("<TEAMNAME>", filteredTeamName);
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
