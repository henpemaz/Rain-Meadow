using Menu;
using Menu.Remix;
using RainMeadow;
using RainMeadow.UI.Components;
using System.Linq;
using UnityEngine;
using Menu.Remix.MixedUI;
using System.Collections.Generic;
using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using RainMeadow.UI;
using Sony.PS4.SaveData;

namespace RainMeadow.Arena.ArenaOnlineGameModes.Drown
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


        public static ArenaSetup.GameTypeID Drown = new ArenaSetup.GameTypeID("Drown", register: true);
        public override ArenaSetup.GameTypeID GetGameModeId
        {
            get
            {
                return Drown;
            }
            set { GetGameModeId = value; }

        }

        public override Dialog AddGameModeInfo(ArenaOnlineGameMode arena, Menu.Menu menu)
        {
            return new DialogNotify(menu.LongTranslate("Will you drown in the deep, or rise from it?"), new Vector2(500f, 400f), menu.manager, () => { menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed); });
        }

        public static bool isDrownMode(ArenaOnlineGameMode arena, out DrownMode mode)
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
        public int spearCost = RainMeadow.rainMeadowOptions.PointsForSpear.Value;
        public int spearExplCost = RainMeadow.rainMeadowOptions.PointsForExplSpear.Value;
        public int bombCost = RainMeadow.rainMeadowOptions.PointsForBomb.Value;
        public int electricSpearCost = RainMeadow.rainMeadowOptions.PointsForElectricSpear.Value;
        public int boomerangeCost = RainMeadow.rainMeadowOptions.PointsForBoomerang.Value;
        public int respCost = RainMeadow.rainMeadowOptions.PointsForRespawn.Value;
        public int rockCost = RainMeadow.rainMeadowOptions.PointsForRock.Value;

        public int denCost = RainMeadow.rainMeadowOptions.PointsForDenOpen.Value;
        public int maxCreatures = RainMeadow.rainMeadowOptions.MaxCreatureCount.Value;
        public int creatureCleanupWaves = RainMeadow.rainMeadowOptions.CreatureCleanup.Value;

        private int _timerDuration;
        public bool openedDen = false;
        public int waveStart = 20;
        public int currentWaveTimer = 20;
        public int currentWave = 0;
        public int lastCleanupWave = 0;
        public bool waveNeedsUpdate = true;
        public DrownInterface? drownInterface;
        public TabContainer.Tab? myTab;


        public override bool IsExitsOpen(ArenaOnlineGameMode arena, On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ArenaBehaviors.ExitManager self)
        {
            return openedDen;

        }

    

        public override bool SpawnBatflies(FliesWorldAI self, int spawnRoom)
        {
            return false;
        }

        public override bool ShouldSessionEnd(ArenaMode arena, On.ArenaGameSession.orig_ShouldSessionEnd orig, ArenaGameSession self)
        {   
            bool teamWork = !self.GameTypeSetup.spearsHitPlayers;
            int count = 0;
            if (DrownMode.isDrownMode(arena, out var drown)) {
            if (self.Players.FindAll(x => x.realizedCreature != null && x.realizedCreature.State.alive).Count == 0) {
            foreach (var p in arena.arenaSittingOnlineOrder)
            {
                OnlinePlayer? pl = ArenaHelpers.FindOnlinePlayerByLobbyId(p);
                if (pl != null)
                {
                    OnlineManager.lobby.clientSettings.TryGetValue(pl, out var cs);
                    if (cs != null)
                    {

                        cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                        if (clientSettings != null)
                        {

                            if ((teamWork ? clientSettings.teamScore : clientSettings.score) < drown.respCost && !drown.openedDen)
                            {
                               count++;
                            }
                        }
                        }
                    }
                }
                if (count == self.arenaSitting.players.Count) // everyone's dead with no money
                {
                    return true;
                }
            }
          }
         return self.thisFrameActivePlayers == 0;
    }

        public override int PlayersStillActive(ArenaMode arena, On.ArenaGameSession.orig_PlayersStillActive orig, ArenaGameSession self, bool addToAliveTime, bool dontCountSandboxLosers)
        {
            if (!DrownMode.isDrownMode(arena, out var drown))
            {
                return orig(self, addToAliveTime, dontCountSandboxLosers);
            }

            bool teamWork = !self.GameTypeSetup.spearsHitPlayers;
            foreach (var p in arena.arenaSittingOnlineOrder)
            {
                OnlinePlayer? pl = ArenaHelpers.FindOnlinePlayerByLobbyId(p);
                if (pl != null)
                {
                    OnlineManager.lobby.clientSettings.TryGetValue(pl, out var cs);
                    if (cs != null)
                    {

                        cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                        if (clientSettings != null)
                        {

                            if ((teamWork ? clientSettings.teamScore : clientSettings.score) >= drown.respCost && !drown.openedDen) // We can still respawn
                            {
                                return self.arenaSitting.players.Count;
                            }
                        }
                    }
                }
            }

            if (teamWork && !drown.openedDen && (self.Players.FindAll(x => x.realizedCreature != null && x.realizedCreature.State.alive).Count > 0))
            {
                return self.arenaSitting.players.Count;
            }

             return orig(self, addToAliveTime, dontCountSandboxLosers);
        }

        public override void ArenaSessionCtor(ArenaOnlineGameMode arena, On.ArenaGameSession.orig_ctor orig, ArenaGameSession self, RainWorldGame game)
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
                        clientSettings.score = 5;
                    }
                }
            }

        }
        public override void Killing(ArenaOnlineGameMode arena, On.ArenaGameSession.orig_Killing orig, ArenaGameSession self, Player player, Creature killedCrit, int playerIndex)
        {
            base.Killing(arena, orig, self, player, killedCrit, playerIndex);
            if (DrownMode.isDrownMode(arena, out _))
            {
                OnlinePhysicalObject? onlineP = player.abstractCreature.GetOnlineObject();
                OnlinePhysicalObject? onlineC = killedCrit.abstractCreature.GetOnlineObject();

                if (onlineP == null || onlineC == null)
                {
                    RainMeadow.Error($"Error in ArenaGameSession_Killing: onlineP :{onlineP} or onlineC : {onlineC}is null");
                    return;
                }

                if (killedCrit.IsLocal())
                {
                    onlineC.BroadcastRPCInRoom(DrownModeRPCs.Drown_Killing, onlineP, onlineC);
                }

                if (player.abstractCreature == killedCrit.killTag && onlineP.owner == OnlineManager.mePlayer) //  Me. I killed them.
                {
                    OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs);
                    if (cs != null)
                    {

                        cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                        if (clientSettings != null)
                        {
                            int arenaPlayer = ArenaHelpers.FindOnlinePlayerNumber(arena, OnlineManager.mePlayer);
                            IconSymbol.IconSymbolData iconSymbolData = CreatureSymbol.SymbolDataFromCreature(killedCrit.abstractCreature);
                            int index = MultiplayerUnlocks.SandboxUnlockForSymbolData(iconSymbolData).Index;
                            if (index >= 0)
                            {
                                self.arenaSitting.players[arenaPlayer].AddSandboxScore(self.arenaSitting.gameTypeSetup.killScores[index]);
                            }
                            else
                            {
                                self.arenaSitting.players[arenaPlayer].AddSandboxScore(0);
                            }
                            clientSettings.score += self.arenaSitting.gameTypeSetup.killScores[index];
                        }
                    }

                }


            }
        }

        public override void InitAsCustomGameType(ArenaSetup.GameTypeSetup self)
        {

            self.foodScore = 1;
            self.survivalScore = 0;
            self.spearHitScore = 1;
            self.repeatSingleLevelForever = false;
            self.denEntryRule = ArenaSetup.GameTypeSetup.DenEntryRule.Standard;
            self.rainWhenOnePlayerLeft = false;
            self.levelItems = true;
            self.fliesSpawn = true;
            self.saveCreatures = false;
            self.spearsHitPlayers = ArenaHelpers.GetOptionFromArena("SPEARSHIT", self.spearsHitPlayers);
            spearHits = self.spearsHitPlayers;
            SandboxSettingsInterface.DefaultKillScores(ref self.killScores);

        }

        public override string TimerText()
        {
            var waveTimer = ArenaPrepTimer.FormatTime(currentWaveTimer);
            OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs);
            var points = 0;
            if (cs != null)
            {

                cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                if (clientSettings != null)
                {
                    points = !spearHits ? clientSettings.teamScore : clientSettings.score;
                }
            }
            var text = !spearHits ? "Team points" : "Current points";
            return $": {text}: {points}. Current Wave: {currentWave}. Next wave: {waveTimer}";
        }

        public override int SetTimer(ArenaOnlineGameMode arena)
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

        public override int TimerDirection(ArenaOnlineGameMode arena, int timer)
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

        public override void LandSpear(ArenaOnlineGameMode arena, ArenaGameSession self, Player player, Creature target, ArenaSitting.ArenaPlayer aPlayer)
        {

        }

        public override void HUD_InitMultiplayerHud(ArenaOnlineGameMode arena, HUD.HUD self, ArenaGameSession session)
        {
            base.HUD_InitMultiplayerHud(arena, self, session);
            self.AddPart(new StoreHUD(self, session.game.cameras[0], this));
        }

        public override bool HoldFireWhileTimerIsActive(ArenaOnlineGameMode arena)
        {
            return arena.countdownInitiatedHoldFire = false;
        }

        public override string AddIcon(ArenaOnlineGameMode arena, PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player)
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


            return base.AddIcon(arena, owner, customization, player);
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

        public override void ArenaSessionEnded(ArenaMode arena, On.ArenaSitting.orig_SessionEnded orig, ArenaSitting self, ArenaGameSession session, List<ArenaSitting.ArenaPlayer> list)
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
                                player.score = clientSettings.score;
                            }
                        }
                    }
                }

                if (list.Count == 1)
                {
                    list[0].winner = list[0].alive;
                }
                else if (list.Count > 1)
                {
                    if (list[0].alive && list[1].alive)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {

                            var onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, list[i].playerNumber);
                            if (onlinePlayer != null)
                            {
                                OnlineManager.lobby.clientSettings.TryGetValue(onlinePlayer, out var cs);
                                if (cs != null)
                                {

                                    cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                                    if (clientSettings != null)
                                    {
                                        list[i].winner = clientSettings.iOpenedDen;
                                    }
                                }
                            }

                        }
                    }
                    if (list[0].alive && !list[1].alive)
                    {
                        list[0].winner = true;
                    }

                }
            }
        }

        public override void ArenaSessionUpdate(ArenaOnlineGameMode arena, ArenaGameSession session)
        {
            if (isDrownMode(arena, out var drown))
            {
                if (!session.sessionEnded)
                {
                    for (int i = 0; i < session.Players.Count; i++)
                    {
                        var onlinePlayer = OnlinePhysicalObject.map.TryGetValue(session.Players[i], out var onlineP);
                        if (onlinePlayer)
                        {
                            if (session.Players[i].state.alive)
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

                                if (drown.openedDen && !openedDen && session.Players[i] != null && session.Players[i].realizedCreature != null && session.Players[i].realizedCreature.State.alive && session.GameTypeSetup.spearsHitPlayers)
                                {
                                    session.game.cameras[0].hud.PlaySound(SoundID.UI_Slugcat_Die);
                                    session.Players[i].realizedCreature.Die();
                                }
                            }
                        }

                    }
                    if (!session.GameTypeSetup.spearsHitPlayers) // team work makes the dream work
                    {
                        var points = 0;

                        arena.arenaSittingOnlineOrder.ForEach(x =>
                        {

                            OnlinePlayer? p = ArenaHelpers.FindOnlinePlayerByLobbyId(x);
                            if (p != null)
                            {
                                OnlineManager.lobby.clientSettings.TryGetValue(p, out var cs);
                                if (cs != null)
                                {

                                    cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                                    if (clientSettings != null)
                                    {

                                        points += clientSettings.score;
                                    }
                                }
                            }
                        });


                        arena.arenaSittingOnlineOrder.ForEach(x =>
                      {

                          OnlinePlayer? p = ArenaHelpers.FindOnlinePlayerByLobbyId(x);
                          if (p != null)
                          {
                              OnlineManager.lobby.clientSettings.TryGetValue(p, out var cs);
                              if (cs != null)
                              {

                                  cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                                  if (clientSettings != null)
                                  {
                                      clientSettings.teamScore = points;
                                  }
                              }
                          }

                      });
                    }

                }

                if (!openedDen)
                {
                    if (currentWaveTimer % waveStart == 0 && session.playersSpawned && waveNeedsUpdate)
                    {
                        var creatureAlive = 0;
                        for (int i = 0; i < session.room.abstractRoom.creatures.Count; i++)
                        {
                            if (session.room.abstractRoom.creatures[i].state.alive)
                            {
                                creatureAlive++;
                            }

                        }
                        if (creatureAlive < maxCreatures)
                        {
                            session.SpawnCreatures();
                        }
                        currentWave++;
                    }
                    if (currentWave % creatureCleanupWaves == 0 && currentWave > lastCleanupWave)
                    {
                        lastCleanupWave = currentWave;

                        CreatureCleanup(arena, session);
                    }
                    waveNeedsUpdate = false;
                }
            }
            base.ArenaSessionUpdate(arena, session);

        }

        

        private void CreatureCleanup(ArenaOnlineGameMode arena, ArenaGameSession session)
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
                        ac.LoseAllStuckObjects();
                        if (!oe.isMine)
                        {
                            oe.beingMoved = true;

                            if (oe.apo.realizedObject is Creature c && c.inShortcut)
                            {
                                if (c.RemoveFromShortcuts()) c.inShortcut = false;
                            }

                            entities.Remove(oe.apo);

                            session.room.abstractRoom.creatures.Remove(oe.apo as AbstractCreature);

                            session.room.RemoveObject(oe.apo.realizedObject);
                            session.room.CleanOutObjectNotInThisRoom(oe.apo.realizedObject);
                            oe.beingMoved = false;
                        }
                        else
                        {
                            oe.ExitResource(roomSession);
                            oe.ExitResource(roomSession.worldSession);
                            oe.apo.realizedObject.RemoveFromRoom();

                        }


                    }
                }
            }
        }

    }
}