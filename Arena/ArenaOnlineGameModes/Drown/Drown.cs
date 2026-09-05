using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Drown;
using Menu;
using RainMeadow.UI;
using RainMeadow.UI.Components;
using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    public class DrownMode : ExternalArenaGameMode
    {
        public const int StartingScore = 5;

        public static ArenaSetup.GameTypeID Drown = new("Drown");

        public static string Rock = "Rock";
        public static string Spear = "Spear";
        public static string ExplosiveSpear = "Explosive Spear";
        public static string ScavengerBomb = "Scavenger Bomb";
        public static string ElectricSpear = "Electric Spear";
        public static string Boomerang = "Boomerang";
        public static string Respawn = "Respawn";
        public static string OpenDens = "Open Dens";

        public override ArenaSetup.GameTypeID GetGameModeId => Drown;
        private int _timerDuration;
        public override int TimerDuration
        {
            get { return _timerDuration; }
            set { _timerDuration = value; }
        }
        public override bool ShowAddedScoreBetweenRoundsInOnlinePlayerUI { get => false; set { } }

        public int spearCost = RainMeadow.rainMeadowOptions.DrownPointsForSpear.Value;
        public int explosiveSpearCost = RainMeadow.rainMeadowOptions.DrownPointsForExplSpear.Value;
        public int bombCost = RainMeadow.rainMeadowOptions.DrownPointsForBomb.Value;
        public int electricSpearCost = RainMeadow.rainMeadowOptions.DrownPointsForElectricSpear.Value;
        public int boomerangCost = RainMeadow.rainMeadowOptions.DrownPointsForBoomerang.Value;
        public int respCost = RainMeadow.rainMeadowOptions.DrownPointsForRespawn.Value;
        public int rockCost = RainMeadow.rainMeadowOptions.DrownPointsForRock.Value;

        public int denCost = RainMeadow.rainMeadowOptions.DrownPointsForDenOpen.Value;
        public int maxCreatures = RainMeadow.rainMeadowOptions.DrownMaxCreatureCount.Value;
        public int creatureCleanupWaves = RainMeadow.rainMeadowOptions.DrownCreatureCleanup.Value;

        public bool openedDen = false;
        public int waveStart = 20;
        public int currentWaveTimer = 20;
        public int currentWave = 0;
        public int lastCleanupWave = 0;
        public bool waveNeedsUpdate = true;
        public int liveCreatureCount = 0;

        public bool WavesHeldByCreatureCap => liveCreatureCount >= maxCreatures;

        public DrownInterface? drownInterface;
        public TabContainer.Tab? myTab;

        public static bool IsDrownMode(out DrownMode drown)
        {
            drown = null!;

            if (!RainMeadow.isArenaMode(out ArenaOnlineGameMode arenaOnline))
                return false;

            if (arenaOnline.registeredGameModes.TryGetValue(Drown.value, out ExternalArenaGameMode externalArena)
                && arenaOnline.currentGameMode == Drown.value)
            {
                drown = (DrownMode)externalArena;
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool ShouldWinByScore(ArenaSetup.GameTypeSetup gameTypeSetup) => false;

        public override void InitAsCustomGameType(
            ArenaOnlineGameMode arenaOnline,
            ArenaSetup.GameTypeSetup self)
        {
            base.InitAsCustomGameType(arenaOnline, self);

            self.survivalScore = 0;
            self.EmptyDeathScore = 0;

            self.rainWhenOnePlayerLeft = false;
            self.fliesSpawn = true;
        }

        public int CalculateTeamScore(ArenaOnlineGameMode arenaOnline, ArenaSitting arenaSitting)
        {
            int score = 0;
            foreach (ArenaSitting.ArenaPlayer arenaPlayer in arenaSitting.players)
            {
                OnlinePlayer? onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arenaOnline, arenaPlayer.playerNumber);
                if (onlinePlayer is null)
                    continue;
                if (arenaPlayer.playerClass == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
                    continue;

                score += arenaPlayer.score;
            }

            return score;
        }

        public override Dialog AddGameModeInfo(ArenaOnlineGameMode arenaOnline, Menu.Menu menu)
        {
            return new DialogNotify(menu.LongTranslate("Kill & survive to buy your escape<LINE><LINE>Turn off Spear Hits for Co-Op"), new Vector2(500f, 400f), menu.manager, () => { menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed); });
        }

        public override bool On_ArenaBehaviors_ExitManager_ExitsOpen(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig,
            ArenaBehaviors.ExitManager self)
        {
            if (self.gameSession != null && self.gameSession.GameTypeSetup.wildLifeSetting == ArenaSetup.GameTypeSetup.WildLifeSetting.Off && self.gameSession.thisFrameActivePlayers == 1 && arenaOnline.setupTime > 10)
            {
                return true;
            }

            return openedDen;

        }

        public override string TimerText()
        {
            ArenaSitting arenaSitting = Custom.rainWorld.processManager.arenaSitting;

            if (arenaSitting is null)
            {
                RainMeadow.Error("Could not find arena sitting.");
                return "";
            }

            ArenaOnlineGameMode arenaOnline = (ArenaOnlineGameMode)OnlineManager.lobby.gameMode;
            ArenaSetup.GameTypeSetup gameTypeSetup = arenaSitting.gameTypeSetup;

            string scoreTypeText = gameTypeSetup.spearsHitPlayers
                ? "Current points"
                : "Team points";

            // TODO: Why can't arena player be found when exiting to the lobby manually?
            int displayScore = gameTypeSetup.spearsHitPlayers
                ? ArenaHelpers.FindArenaPlayerByOnlinePlayer(
                    arenaOnline,
                    arenaSitting,
                    OnlineManager.mePlayer
                )?.score ?? 0
                : CalculateTeamScore(arenaOnline, arenaSitting);

            string waveText = "";
            if (gameTypeSetup.wildLifeSetting != ArenaSetup.GameTypeSetup.WildLifeSetting.Off)
            {
                // The countdown keeps running at the cap, so inform why nothing is spawning.
                string capText = WavesHeldByCreatureCap
                    ? $" ({liveCreatureCount}/{maxCreatures} creatures - kill to resume waves)"
                    : $" ({liveCreatureCount}/{maxCreatures} creatures)";

                waveText =
                    $" Current Wave: {currentWave}. Next wave: {ArenaPrepTimer.FormatTime(currentWaveTimer)}{capText}";
            }

            return $": {scoreTypeText}: {displayScore}.{waveText}";
        }

        public override int SetTimer(ArenaOnlineGameMode arenaOnline)
        {
            return arenaOnline.setupTime = 1;
        }

        public override void ResetGameTimer()
        {
            _timerDuration = 1;

        }

        public override int TimerDirection(ArenaOnlineGameMode arenaOnline, int timer)
        {
            if (!openedDen)
            {
                currentWaveTimer--;
                if (currentWaveTimer == 0)
                {
                    currentWaveTimer = waveStart;
                    waveNeedsUpdate = true;
                }

                return ++arenaOnline.setupTime;
            }
            else
            {
                return arenaOnline.setupTime;
            }
        }

        public override void On_ArenaGameSession_ctor(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaGameSession.orig_ctor orig,
            ArenaGameSession self,
            RainWorldGame game)
        {
            base.On_ArenaGameSession_ctor(arenaOnline, orig, self, game);
            openedDen = false;
            currentWave = 1;
            lastCleanupWave = 0;

            ArenaSitting arenaSitting = self.arenaSitting;

            ArenaDrownClientSettings teamClientData = OnlineManager.lobby
                .clientSettings[OnlineManager.mePlayer]
                .GetData<ArenaDrownClientSettings>();

            teamClientData.iOpenedDen = false;

            foreach (ArenaSitting.ArenaPlayer arenaPlayer in arenaSitting.players)
            {
                OnlinePlayer? onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(
                    arenaOnline,
                    arenaPlayer.playerNumber
                );

                if (onlinePlayer is null)
                    continue;
                if (arenaPlayer.playerClass == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
                    continue;

                arenaPlayer.score = StartingScore;

                if (OnlineManager.lobby.isOwner)
                    arenaOnline.CopyStatsToLobbyData(arenaPlayer, onlinePlayer);
            }
        }

        public override void On_ArenaGameSession_Update(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaGameSession.orig_Update orig,
            ArenaGameSession self)
        {
            if (IsDrownMode(out DrownMode drown))
            {
                if (!self.sessionEnded)
                {
                    for (int i = 0; i < self.Players.Count; i++)
                    {
                        var onlinePlayer = OnlinePhysicalObject.map.TryGetValue(self.Players[i], out var onlineP);
                        if (onlinePlayer)
                        {
                            if (self.Players[i].state.alive)
                            {
                                bool openedDen = false;
                                OnlineManager.lobby.clientSettings.TryGetValue(onlineP.owner, out var cs);
                                if (cs != null)
                                {

                                    cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                                    if (clientSettings != null)
                                    {
                                        openedDen = clientSettings.iOpenedDen;
                                    }
                                }

                                if (drown.openedDen && !openedDen && self.Players[i] != null && self.Players[i].realizedCreature != null && self.Players[i].realizedCreature.State.alive && self.GameTypeSetup.spearsHitPlayers)
                                {
                                    self.game.cameras[0].hud.PlaySound(SoundID.UI_Slugcat_Die);
                                    self.Players[i].realizedCreature.Die();
                                }
                            }
                        }
                    }
                }

                if (!openedDen)
                {
                    if (self.playersSpawned)
                    {
                        liveCreatureCount = ThatsCap(self);
                    }

                    if (currentWaveTimer % waveStart == 0 && self.playersSpawned && waveNeedsUpdate)
                    {
                        // A wave that cannot spawn is not a wave, so hold the counter.
                        if (!WavesHeldByCreatureCap)
                        {
                            self.SpawnCreatures();
                            currentWave++;
                        }
                    }
                    if (currentWave % creatureCleanupWaves == 0 && currentWave > lastCleanupWave)
                    {
                        lastCleanupWave = currentWave;

                        CreatureCleanup(arenaOnline, self);
                    }
                    waveNeedsUpdate = false;
                }
            }
            base.On_ArenaGameSession_Update(arenaOnline, orig, self);

        }

        public override void On_HUD_HUD_InitMultiplayerHud(
            ArenaOnlineGameMode arenaOnline,
            On.HUD.HUD.orig_InitMultiplayerHud orig,
            HUD.HUD self,
            ArenaGameSession session)
        {
            base.On_HUD_HUD_InitMultiplayerHud(arenaOnline, orig, self, session);
            self.AddPart(new StoreHUD(self, session.game.cameras[0], this));
        }

        public override bool HoldFireWhileTimerIsActive(ArenaOnlineGameMode arenaOnline)
        {
            return arenaOnline.countdownInitiatedHoldFire = false;
        }

        public override string AddIcon(
            ArenaOnlineGameMode arenaOnline,
            OnlinePlayerDisplay display,
            PlayerSpecificOnlineHud owner,
            SlugcatCustomization customization,
            OnlinePlayer player)
        {
            if (player != null)
            {
                OnlineManager.lobby.clientSettings.TryGetValue(player, out var cs);
                if (cs != null)
                {

                    cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                    if (clientSettings != null && clientSettings.isInStore)
                        return "spearSymbol";
                    else
                        return "";
                }
            }


            return base.AddIcon(arenaOnline, display, owner, customization, player);
        }

        public override Color IconColor(
            ArenaOnlineGameMode arenaOnline,
            OnlinePlayerDisplay display,
            PlayerSpecificOnlineHud owner,
            SlugcatCustomization customization,
            OnlinePlayer player)
        {
            if (owner.PlayerConsideredDead)
            {
                return Color.grey;
            }

            return base.IconColor(arenaOnline, display, owner, customization, player);
        }

        public override void OnUIEnabled(ArenaOnlineLobbyMenu menu)
        {
            base.OnUIEnabled(menu);
            myTab = menu.arenaMainLobbyPage.tabContainer.AddTab(menu.Translate("Drown Settings"));
            myTab.AddObjects(drownInterface = new DrownInterface((ArenaOnlineGameMode)OnlineManager.lobby.gameMode, this, myTab.menu, myTab, new(0, 0), menu.arenaMainLobbyPage.tabContainer.size));
        }

        public override void OnUIDisabled(ArenaOnlineLobbyMenu menu)
        {
            base.OnUIDisabled(menu);
            drownInterface?.OnShutdown();
            if (myTab != null) menu.arenaMainLobbyPage.tabContainer.RemoveTab(myTab);
            myTab = null;
        }

        /// <inheritdoc/>
        public override List<ArenaSitting.ArenaPlayer> DetermineArenaSessionWinners(
            ArenaOnlineGameMode arenaOnline,
            ArenaGameSession arenaSession)
        {
            ArenaSitting arenaSitting = arenaSession.arenaSitting;
            List<ArenaSitting.ArenaPlayer> winners = [];

            foreach (ArenaSitting.ArenaPlayer arenaPlayer in arenaSitting.players)
            {
                OnlinePlayer? onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(
                    arenaOnline,
                    arenaPlayer.playerNumber
                );

                if (onlinePlayer is null)
                    continue;
                if (arenaPlayer.playerClass == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
                    continue;

                if (!OnlineManager.lobby.clientSettings[onlinePlayer]
                    .TryGetData(out ArenaDrownClientSettings clientData))
                {
                    RainMeadow.Error($"Unable to find {onlinePlayer}'s drown client data.");
                    continue;
                }

                if (arenaSession.GameTypeSetup.spearsHitPlayers)
                {
                    if (clientData.iOpenedDen)
                        winners.Add(arenaPlayer);
                }
                else
                {
                    if (arenaPlayer.alive)
                        winners.Add(arenaPlayer);
                }
            }

            return winners;
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
                : resultMenu.Translate("GAME OVER");

            if (resultMenu is MultiplayerResults)
            {
                if (resultMenu.ArenaSitting.gameTypeSetup.spearsHitPlayers)
                {
                    return base.GetResultText(arenaOnline, resultMenu, out isSpecific);
                }
                else
                {
                    isSpecific = false;
                    return nonSpecificText;
                }
            }

            List<ArenaSitting.ArenaPlayer> activeArenaPlayers = resultMenu.result
                .Where(arenaPlayer => arenaPlayer.playerClass != RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
                .ToList();

            if (activeArenaPlayers.Count < 2)
            {
                isSpecific = false;
                return nonSpecificText;
            }

            List<ArenaSitting.ArenaPlayer> winners = activeArenaPlayers
                .Where(arenaPlayer => arenaPlayer.winner)
                .ToList();


            if (resultMenu.ArenaSitting.gameTypeSetup.spearsHitPlayers)
            {
                switch (winners.Count)
                {
                    case 1:
                        ArenaSitting.ArenaPlayer winner = winners[0];
                        OnlinePlayer? onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(
                            arenaOnline,
                            winner.playerNumber
                        );

                        if (onlinePlayer is null)
                        {
                            RainMeadow.Error(
                                $"Unable to find the online player corresponding to the winner. "
                                + $"Player number: {winner.playerNumber}."
                            );

                            isSpecific = false;
                            return nonSpecificText;
                        }

                        string displayName = MatchmakingManager.currentInstance.FilterTeamName(
                            onlinePlayer.id.DisplayName
                        );

                        return resultMenu.Translate(winner.alive ? "<USERNAME> ESCAPED!" : "<USERNAME> WINS!")
                            .Replace("<USERNAME>", displayName);

                    case 0:
                        return resultMenu.Translate("EVERYONE DROWNED!");

                    case > 1:
                        RainMeadow.Error(
                            $"When spear hits are on there can only be 1 or 0 winners (there are {winners.Count})"
                        );
                        isSpecific = false;
                        return nonSpecificText;

                    case < 1: // Not possible, only included because the compiler treats .Count as a regular int which can be negative.
                        throw new Exception();
                }
            }
            else
            {
                if (winners.Count == activeArenaPlayers.Count)
                    return resultMenu.Translate("EVERYONE ESCAPED!");

                return winners.Count == 0
                    ? resultMenu.Translate("EVERYONE DROWNED!")
                    : resultMenu.Translate("SOME PLAYERS ESCAPED!");
            }
        }

        private static int ThatsCap(ArenaGameSession session)
        {
            List<AbstractCreature>? creatures = session.room?.abstractRoom?.creatures;

            if (creatures == null)
                return 0;

            int count = 0;

            for (int i = 0; i < creatures.Count; i++)
            {
                AbstractCreature creature = creatures[i];

                if (creature?.realizedCreature == null || !creature.state.alive)
                    continue;
                if (creature.creatureTemplate.type == CreatureTemplate.Type.Slugcat)
                    continue;
                if (creature.creatureTemplate.type == CreatureTemplate.Type.Fly)
                    continue;

                count++;
            }

            return count;
        }

        private void CreatureCleanup(ArenaOnlineGameMode arenaOnline, ArenaGameSession session)
        {
            if (RoomSession.map.TryGetValue(session.room.abstractRoom, out var roomSession))
            {
                var entities = session.room.abstractRoom.entities;
                for (int i = entities.Count - 1; i >= 0; i--)
                {
                    if (entities[i] is AbstractPhysicalObject apo && apo is AbstractCreature ac && ac.state.dead && ac.realizedCreature.grabbedBy.Count <= 0 && OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
                    {
                        for (int num = ac.stuckObjects.Count - 1; num >= 0; num--)
                        {
                            if (ac.stuckObjects[num] is AbstractPhysicalObject.AbstractSpearStick && ac.stuckObjects[num].A.type == AbstractPhysicalObject.AbstractObjectType.Spear && ac.stuckObjects[num].A.realizedObject != null)
                            {
                                (ac.stuckObjects[num].A.realizedObject as Spear).ChangeMode(Weapon.Mode.Free);
                            }
                        }
                        oe.RemoveEntityFromRoom();
                        oe.RemoveEntityFromGame();
                    }
                }
            }
        }

        public override string ExportLocalSettings(ArenaOnlineGameMode arenaOnline)
        {
            string baseExport = base.ExportLocalSettings(arenaOnline);
            string decodedBase = string.IsNullOrEmpty(baseExport) ? "" : Encoding.UTF8.GetString(Convert.FromBase64String(baseExport));

            var pairs = new List<string>
            {
                $"bombCost={bombCost}",
                $"boomerangCost={boomerangCost}",
                $"creatureCleanupWaves={creatureCleanupWaves}",
                $"denCost={denCost}",
                $"electricSpearCost={electricSpearCost}",
                $"explosiveSpearCost={explosiveSpearCost}",
                $"maxCreatures={maxCreatures}",
                $"respCost={respCost}",
                $"rockCost={rockCost}",
                $"spearCost={spearCost}",
            };

            string combined = string.Join("|", pairs);

            if (!string.IsNullOrEmpty(decodedBase))
            {
                combined = decodedBase + "|" + combined;
            }

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
        }

        public override bool ImportLocalSettings(ArenaOnlineGameMode arenaOnline, string base64Data)
        {
            if (string.IsNullOrEmpty(base64Data)) return false;
            bool success = base.ImportLocalSettings(arenaOnline, base64Data);
            if (!success) return false;

            try
            {
                string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64Data));
                string[] pairs = decoded.Split('|');

                foreach (string pair in pairs)
                {
                    string[] kvp = pair.Split('=');
                    if (kvp.Length != 2) continue;

                    string key = kvp[0];
                    string val = kvp[1];

                    // Sorted alphanumerically
                    switch (key)
                    {
                        case "bombCost":
                            if (int.TryParse(val, out int i1)) bombCost = i1;
                            break;
                        case "boomerangCost":
                            if (int.TryParse(val, out int i2)) boomerangCost = i2;
                            break;
                        case "creatureCleanupWaves":
                            if (int.TryParse(val, out int i3)) creatureCleanupWaves = i3;
                            break;
                        case "denCost":
                            if (int.TryParse(val, out int i4)) denCost = i4;
                            break;
                        case "electricSpearCost":
                            if (int.TryParse(val, out int i5)) electricSpearCost = i5;
                            break;
                        case "explosiveSpearCost":
                            if (int.TryParse(val, out int i6)) explosiveSpearCost = i6;
                            break;
                        case "maxCreatures":
                            if (int.TryParse(val, out int i7)) maxCreatures = i7;
                            break;
                        case "respCost":
                            if (int.TryParse(val, out int i8)) respCost = i8;
                            break;
                        case "rockCost":
                            if (int.TryParse(val, out int i9)) rockCost = i9;
                            break;
                        case "spearCost":
                            if (int.TryParse(val, out int i10)) spearCost = i10;
                            break;
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                return false;
            }
        }
    }
}
