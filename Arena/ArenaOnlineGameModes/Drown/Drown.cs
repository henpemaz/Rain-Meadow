using Menu;
using RainMeadow.UI.Components;
using System.Linq;
using UnityEngine;
using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using RainMeadow.UI;
using Drown;
using System;
using System.Text;
using System.Collections.Generic;
namespace RainMeadow
{
    public partial class DrownMode : ExternalArenaGameMode
    {

        public static string Rock = "Rock";
        public static string Spear = "Spear";
        public static string ExplosiveSpear = "Explosive Spear";
        public static string ScavengerBomb = "Scavenger Bomb";
        public static string ElectricSpear = "Electric Spear";
        public static string Boomerang = "Boomerang";
        public static string Respawn = "Respawn";
        public static string OpenDens = "Open Dens";


        public static ArenaSetup.GameTypeID Drown = new ArenaSetup.GameTypeID("Drown", register: false);
        public override ArenaSetup.GameTypeID GetGameModeId
        {
            get
            {
                return Drown;
            }

        }

        public override Dialog AddGameModeInfo(ArenaMode arena, Menu.Menu menu)
        {
            return new DialogNotify(menu.LongTranslate("Kill & survive to buy your escape<LINE><LINE>Toggle Spear Hits for teams or FFA"), new Vector2(500f, 400f), menu.manager, () => { menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed); });
        }

        public static bool isDrownMode(ArenaMode arena, out DrownMode mode)
        {
            mode = null;
            if (arena.currentGameMode == Drown.value)
            {
                mode = (arena.registeredGameModes.FirstOrDefault(x => x.Key == Drown.value).Value as DrownMode);
                return true;
            }
            return false;
        }

        private bool spearHits;
        public bool isInStore = false;
        public int spearCost = RainMeadow.rainMeadowOptions.DrownPointsForSpear.Value;
        public int spearExplCost = RainMeadow.rainMeadowOptions.DrownPointsForExplSpear.Value;
        public int bombCost = RainMeadow.rainMeadowOptions.DrownPointsForBomb.Value;
        public int electricSpearCost = RainMeadow.rainMeadowOptions.DrownPointsForElectricSpear.Value;
        public int boomerangeCost = RainMeadow.rainMeadowOptions.DrownPointsForBoomerang.Value;
        public int respCost = RainMeadow.rainMeadowOptions.DrownPointsForRespawn.Value;
        public int rockCost = RainMeadow.rainMeadowOptions.DrownPointsForRock.Value;

        public int denCost = RainMeadow.rainMeadowOptions.DrownPointsForDenOpen.Value;
        public int maxCreatures = RainMeadow.rainMeadowOptions.DrownMaxCreatureCount.Value;
        public int creatureCleanupWaves = RainMeadow.rainMeadowOptions.DrownCreatureCleanup.Value;

        private int _timerDuration;
        public bool openedDen = false;
        public int waveStart = 20;
        public int currentWaveTimer = 20;
        public int currentWave = 0;
        public int lastCleanupWave = 0;
        public bool waveNeedsUpdate = true;

        public int timerPoints = 0; // no way to get this from ArenaGameSession without breaking API

        public int teamPoints;
        public DrownInterface? drownInterface;
        public TabContainer.Tab? myTab;

        public override bool IsExitsOpen(ArenaMode arena, On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ArenaBehaviors.ExitManager self)
        {
            if (self.gameSession != null && self.gameSession.GameTypeSetup.wildLifeSetting == ArenaSetup.GameTypeSetup.WildLifeSetting.Off && self.gameSession.thisFrameActivePlayers == 1 && arena.setupTime > 10)
            {
                return true;
            }

            return openedDen;

        }


        public override bool SpawnBatflies(FliesWorldAI self, int spawnRoom)
        {
            return false;
        }

        public override void ArenaSessionCtor(ArenaMode arena, On.ArenaGameSession.orig_ctor orig, ArenaGameSession self, RainWorldGame game)
        {
            base.ArenaSessionCtor(arena, orig, self, game);
            openedDen = false;
            currentWave = 1;
            lastCleanupWave = 0;

            foreach (var player in self.arenaSitting.players)
            {
                player.score = 5;
                OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs);
                if (cs != null)
                {

                    cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                    if (clientSettings != null)
                    {
                        clientSettings.iOpenedDen = false;
                    }
                }
            }

        }

        public override void InitAsCustomGameType(ArenaMode arena, ArenaSetup.GameTypeSetup self)
        {
            self.foodScore = 1;
            self.survivalScore = arena.aliveScore;
            self.spearHitScore = arena.spearHitScore;
            self.repeatSingleLevelForever = false;
            self.denEntryRule = arena.denEntryRule;
            self.rainWhenOnePlayerLeft = false;
            self.levelItems = true;
            self.fliesSpawn = true;
            self.saveCreatures = false;
            self.spearsHitPlayers = ArenaHelpers.GetOptionFromArena("SPEARSHIT", self.spearsHitPlayers);
            spearHits = self.spearsHitPlayers;
            if (arena.killScore == 0)
            {
                SandboxSettingsInterface.DefaultKillScores(ref self.killScores);
            }

        }

        public override string TimerText()
        {

            RainMeadow.isArenaMode(out var arena);
            var text = !spearHits ? "Team points" : "Current points";
            var waveText = "";

            var waveTimer = ArenaPrepTimer.FormatTime(currentWaveTimer);
            OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs);

            if (cs != null)
            {
                cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                if (clientSettings != null)
                {
                    if (!spearHits)
                    {
                        timerPoints = teamPoints;
                    }
                    else
                    {
                        timerPoints = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame).GetArenaGameSession.arenaSitting.players[ArenaHelpers.FindOnlinePlayerNumber(arena, OnlineManager.mePlayer)].score;
                    }
                }
            }
            if (arena.session != null && arena.session.GameTypeSetup.wildLifeSetting != ArenaSetup.GameTypeSetup.WildLifeSetting.Off)
            {
                waveText = $"Current Wave: {currentWave}. Next wave: {waveTimer}";
            }
            return $": {text}: {timerPoints}. {waveText}";
        }

        public override int SetTimer(ArenaMode arena)
        {
            return arena.setupTime = 1;
        }

        public override void ResetGameTimer()
        {
            _timerDuration = 1;

        }

        public override int TimerDuration
        {
            get { return _timerDuration; }
            set { _timerDuration = value; }
        }

        public override int TimerDirection(ArenaMode arena, int timer)
        {
            if (!openedDen)
            {

                currentWaveTimer--;
                if (currentWaveTimer == 0)
                {
                    currentWaveTimer = waveStart;
                    waveNeedsUpdate = true;
                }

                return ++arena.setupTime;
            }
            else
            {
                return arena.setupTime;
            }
        }


        public override void HUD_InitMultiplayerHud(ArenaMode arena, HUD.HUD self, ArenaGameSession session)
        {
            base.HUD_InitMultiplayerHud(arena, self, session);
            self.AddPart(new StoreHUD(self, session.game.cameras[0], this));
        }

        public override bool HoldFireWhileTimerIsActive(ArenaMode arena)
        {
            return arena.countdownInitiatedHoldFire = false;
        }

        public override string AddIcon(ArenaMode arena, OnlinePlayerDisplay display, PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player)
        {
            if (player != null)
            {
                OnlineManager.lobby.clientSettings.TryGetValue(player, out var cs);
                if (cs != null)
                {

                    cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                    if (clientSettings != null && clientSettings.isInStore)
                    {
                        return "spearSymbol";
                    }
                    else
                    {
                        return "Kill_Slugcat";

                    }
                }
            }


            return base.AddIcon(arena, display, owner, customization, player);
        }

        public override Color IconColor(ArenaMode arena, OnlinePlayerDisplay display, PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player)
        {
            if (owner.PlayerConsideredDead)
            {
                return Color.grey;
            }

            return base.IconColor(arena, display, owner, customization, player);
        }



        public override void OnUIEnabled(ArenaOnlineLobbyMenu menu)
        {
            base.OnUIEnabled(menu);
            myTab = menu.arenaMainLobbyPage.tabContainer.AddTab(menu.Translate("Drown Settings"));
            myTab.AddObjects(drownInterface = new DrownInterface((ArenaMode)OnlineManager.lobby.gameMode, this, myTab.menu, myTab, new(0, 0), menu.arenaMainLobbyPage.tabContainer.size));
        }
        public override void OnUIDisabled(ArenaOnlineLobbyMenu menu)
        {
            base.OnUIDisabled(menu);
            drownInterface?.OnShutdown();
            if (myTab != null) menu.arenaMainLobbyPage.tabContainer.RemoveTab(myTab);
            myTab = null;
        }
        public override void ArenaSessionEnded(ArenaMode arena, On.ArenaSitting.orig_SessionEnded orig, ArenaSitting self, ArenaGameSession session)
        {
            if (isDrownMode(arena, out var drown))
            {
                foreach (var player in self.players)
                {
                    var onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, player.playerNumber);
                    if (onlinePlayer != null)
                    {
                        OnlineManager.lobby.clientSettings.TryGetValue(onlinePlayer, out var cs);
                        if (cs != null)
                        {
                            if (cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings))
                            {
                                player.winner = clientSettings.iOpenedDen;
                            }
                        }
                    }
                }
            }
            base.ArenaSessionEnded(arena, orig, self, session);
        }

        public override void ArenaSessionUpdate(On.ArenaGameSession.orig_Update orig, ArenaGameSession self, ArenaMode arena)
        {

            if (isDrownMode(arena, out var drown))
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
                    if (!self.GameTypeSetup.spearsHitPlayers) // team work makes the dream work
                    {
                        teamPoints = self.arenaSitting.players.Sum(x => x.score);
                    }

                }

                if (!openedDen)
                {
                    if (currentWaveTimer % waveStart == 0 && self.playersSpawned && waveNeedsUpdate)
                    {
                        var creatureAlive = 0;
                        for (int i = 0; i < self.room.abstractRoom.creatures.Count; i++)
                        {
                            if (self.room.abstractRoom.creatures[i].state.alive && self.room.abstractRoom.creatures[i].creatureTemplate.type != CreatureTemplate.Type.Slugcat)
                            {
                                creatureAlive++;
                            }

                        }
                        if (creatureAlive < maxCreatures)
                        {
                            self.SpawnCreatures();
                        }
                        currentWave++;
                    }
                    if (currentWave % creatureCleanupWaves == 0 && currentWave > lastCleanupWave)
                    {
                        lastCleanupWave = currentWave;

                        CreatureCleanup(arena, self);
                    }
                    waveNeedsUpdate = false;
                }
            }
            base.ArenaSessionUpdate(orig, self, arena);

        }



        private void CreatureCleanup(ArenaMode arena, ArenaGameSession session)
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

        public override string ExportLocalSettings(ArenaMode arena)
        {
            string baseExport = base.ExportLocalSettings(arena);
            string decodedBase = string.IsNullOrEmpty(baseExport) ? "" : Encoding.UTF8.GetString(Convert.FromBase64String(baseExport));

            var pairs = new List<string>
            {
                $"bombCost={bombCost}",
                $"boomerangeCost={boomerangeCost}",
                $"creatureCleanupWaves={creatureCleanupWaves}",
                $"denCost={denCost}",
                $"electricSpearCost={electricSpearCost}",
                $"maxCreatures={maxCreatures}",
                $"respCost={respCost}",
                $"rockCost={rockCost}",
                $"spearCost={spearCost}",
                $"spearExplCost={spearExplCost}",
            };

            string combined = string.Join("|", pairs);

            if (!string.IsNullOrEmpty(decodedBase))
            {
                combined = decodedBase + "|" + combined;
            }

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
        }

        public override bool ImportLocalSettings(ArenaMode arena, string base64Data)
        {
            if (string.IsNullOrEmpty(base64Data)) return false;
            bool success = base.ImportLocalSettings(arena, base64Data);
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
                        case "boomerangeCost":
                            if (int.TryParse(val, out int i2)) boomerangeCost = i2;
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
                        case "maxCreatures":
                            if (int.TryParse(val, out int i6)) maxCreatures = i6;
                            break;
                        case "respCost":
                            if (int.TryParse(val, out int i7)) respCost = i7;
                            break;
                        case "rockCost":
                            if (int.TryParse(val, out int i8)) rockCost = i8;
                            break;
                        case "spearCost":
                            if (int.TryParse(val, out int i9)) spearCost = i9;
                            break;
                        case "spearExplCost":
                            if (int.TryParse(val, out int i10)) spearExplCost = i10;
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
