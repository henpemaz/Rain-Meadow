using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;
using HUD;
using Menu;
using MoreSlugcats;
using RainMeadow;
using UnityEngine;
using static RainMeadow.OnlineEntity;
namespace RainMeadow
{
    public abstract class ExternalArenaGameMode
    {
        private int _timerDuration;

        public abstract bool IsExitsOpen(ArenaOnlineGameMode arena, On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ArenaBehaviors.ExitManager self);
        public abstract bool SpawnBatflies(FliesWorldAI self, int spawnRoom);

        public abstract int TimerDuration { get; set; }

        public virtual void ArenaSessionCtor(ArenaOnlineGameMode arena, On.ArenaGameSession.orig_ctor orig, ArenaGameSession self, RainWorldGame game)
        {
            arena.ResetAtSession_ctor();
        }

        public virtual void ArenaSessionNextLevel(ArenaOnlineGameMode arena, On.ArenaSitting.orig_NextLevel orig, ArenaSitting self, ProcessManager process)
        {
            arena.ResetAtNextLevel();
        }

        public virtual void InitAsCustomGameType(ArenaSetup.GameTypeSetup self)
        {
            self.foodScore = 1;
            self.survivalScore = 0;
            self.spearHitScore = 0;
            self.repeatSingleLevelForever = false;
            self.savingAndLoadingSession = true;
            self.denEntryRule = ArenaSetup.GameTypeSetup.DenEntryRule.Standard;
            self.rainWhenOnePlayerLeft = true;
            self.levelItems = true;
            self.fliesSpawn = true;
            self.saveCreatures = false;
        }

        public virtual string TimerText()
        {
            return "";
        }

        public virtual int SetTimer(ArenaOnlineGameMode arena)
        {
            return arena.setupTime = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
        }

        public virtual void ResetGameTimer()
        {
            _timerDuration = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;

        }
        public virtual int TimerDirection(ArenaOnlineGameMode arena, int timer)
        {
            return --timer;
        }
        public virtual void Killing(ArenaOnlineGameMode arena, On.ArenaGameSession.orig_Killing orig, ArenaGameSession self, Player player, Creature killedCrit, int playerIndex)
        {
        }
        public virtual void LandSpear(ArenaOnlineGameMode arena, ArenaGameSession self, Player player, Creature target, ArenaSitting.ArenaPlayer aPlayer)
        {

        }
        public virtual void HUD_InitMultiplayerHud(ArenaOnlineGameMode arena, HUD.HUD self, ArenaGameSession session)
        {
            self.AddPart(new HUD.TextPrompt(self));

            if (MatchmakingManager.currentInstance.canSendChatMessages)
                self.AddPart(new ChatHud(self, session.game.cameras[0]));

            self.AddPart(new SpectatorHud(self, session.game.cameras[0]));
            self.AddPart(new ArenaPrepTimer(self, self.fContainers[0], arena, session));
            self.AddPart(new OnlineHUD(self, session.game.cameras[0], arena));
            self.AddPart(new Pointing(self));
            self.AddPart(new ArenaSpawnLocationIndicator(self, session.game.cameras[0]));
            self.AddPart(new Watcher.CamoMeter(self, self.fContainers[1]));
            if (ModManager.Watcher && OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaClientSettings>().playingAs == Watcher.WatcherEnums.SlugcatStatsName.Watcher)
            {
                RainMeadow.Debug("Adding Watcher Camo Meter");
                self.AddPart(new Watcher.CamoMeter(self, self.fContainers[1]));
            }
        }
        public virtual void ArenaCreatureSpawner_SpawnCreatures(ArenaOnlineGameMode arena, On.ArenaCreatureSpawner.orig_SpawnArenaCreatures orig, RainWorldGame game, ArenaSetup.GameTypeSetup.WildLifeSetting wildLifeSetting, ref List<AbstractCreature> availableCreatures, ref MultiplayerUnlocks unlocks)
        {

        }

        public virtual bool HoldFireWhileTimerIsActive(ArenaOnlineGameMode arena)
        {
            return arena.countdownInitiatedHoldFire = false;
        }

        public virtual string AddCustomIcon(ArenaOnlineGameMode arena, PlayerSpecificOnlineHud onlineHud)
        {
            return "";
        }

        public virtual void SpawnPlayer(ArenaOnlineGameMode arena, ArenaGameSession self, Room room, List<int> suggestedDens)
        {
            List<OnlinePlayer> list = new List<OnlinePlayer>();

            List<OnlinePlayer> list2 = new List<OnlinePlayer>();

            for (int j = 0; j < OnlineManager.players.Count; j++)
            {
                if (arena.arenaSittingOnlineOrder.Contains(OnlineManager.players[j].inLobbyId))
                {
                    list2.Add(OnlineManager.players[j]);
                }
            }

            while (list2.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, list2.Count);
                list.Add(list2[index]);
                list2.RemoveAt(index);
            }

            int totalExits = self.game.world.GetAbstractRoom(0).exits;
            int[] exitScores = new int[totalExits];
            if (suggestedDens != null)
            {
                for (int k = 0; k < suggestedDens.Count; k++)
                {
                    if (suggestedDens[k] >= 0 && suggestedDens[k] < exitScores.Length)
                    {
                        exitScores[suggestedDens[k]] -= 1000;
                    }
                }
            }

            int randomExitIndex = UnityEngine.Random.Range(0, totalExits);
            float highestScore = float.MinValue;


            for (int currentExitIndex = 0; currentExitIndex < totalExits; currentExitIndex++)
            {
                float score = UnityEngine.Random.value - (float)exitScores[currentExitIndex] * 1000f;
                RWCustom.IntVector2 startTilePosition = room.ShortcutLeadingToNode(currentExitIndex).StartTile;

                for (int otherExitIndex = 0; otherExitIndex < totalExits; otherExitIndex++)
                {
                    if (otherExitIndex != currentExitIndex && exitScores[otherExitIndex] > 0)
                    {
                        float distanceAdjustment = Mathf.Clamp(startTilePosition.FloatDist(room.ShortcutLeadingToNode(otherExitIndex).StartTile), 8f, 17f) * UnityEngine.Random.value;
                        score += distanceAdjustment;
                    }
                }

                if (score > highestScore)
                {
                    randomExitIndex = currentExitIndex;
                    highestScore = score;
                }
            }

            RainMeadow.Debug("Trying to create an abstract creature");
            RainMeadow.Debug($"RANDOM EXIT INDEX: {randomExitIndex}");
            RainMeadow.Debug($"RANDOM START TILE INDEX: {room.ShortcutLeadingToNode(randomExitIndex).StartTile}");
            RainMeadow.sSpawningAvatar = true;
            AbstractCreature abstractCreature = new AbstractCreature(self.game.world, StaticWorld.GetCreatureTemplate("Slugcat"), null, new WorldCoordinate(0, -1, -1, -1), new EntityID(-1, 0));
            abstractCreature.pos.room = self.game.world.GetAbstractRoom(0).index;
            abstractCreature.pos.abstractNode = room.ShortcutLeadingToNode(randomExitIndex).destNode;


            RainMeadow.Debug("assigned ac, registering");

            self.game.world.GetResource().ApoEnteringWorld(abstractCreature);
            RainMeadow.sSpawningAvatar = false;

            self.game.cameras[0].followAbstractCreature = abstractCreature;

            if (abstractCreature.GetOnlineObject(out var oe) && oe.TryGetData<SlugcatCustomization>(out var customization))
            {
                abstractCreature.state = new PlayerState(abstractCreature, 0, customization.playingAs, isGhost: false);

            }
            else
            {
                RainMeadow.Error("Could not get online owner for spawned player!");
                abstractCreature.state = new PlayerState(abstractCreature, 0, self.arenaSitting.players[ArenaHelpers.FindOnlinePlayerNumber(arena, OnlineManager.mePlayer)].playerClass, isGhost: false);
            }

            RainMeadow.Debug("Arena: Realize Creature!");
            abstractCreature.Realize();
            var shortCutVessel = new ShortcutHandler.ShortCutVessel(room.ShortcutLeadingToNode(randomExitIndex).DestTile, abstractCreature.realizedCreature, self.game.world.GetAbstractRoom(0), 0);

            shortCutVessel.entranceNode = abstractCreature.pos.abstractNode;
            shortCutVessel.room = self.game.world.GetAbstractRoom(abstractCreature.Room.name);

            self.game.shortcuts.betweenRoomsWaitingLobby.Add(shortCutVessel);
            self.AddPlayer(abstractCreature);
            if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Night)
            {
                (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 1;
            }
            if (ModManager.MSC)
            {
                if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Red)
                {
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, -0.75f);
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, 0.5f);
                }

                if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Yellow)
                {
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, 0.75f);
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, 0.3f);
                }

                if ((abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer)
                {
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, -0.5f);
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, -1f);
                }

                if ((abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup)
                {
                    (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 1;
                }

                if ((abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
                {
                    (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = arena.painCatThrowingSkill;
                    RainMeadow.Debug("ENOT THROWING SKILL " + (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill);
                    if ((abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill == 0 && arena.painCatEgg)
                    {
                        AbstractPhysicalObject bringThePain = new AbstractPhysicalObject(room.world, DLCSharedEnums.AbstractObjectType.SingularityBomb, null, abstractCreature.pos, shortCutVessel.room.world.game.GetNewID());
                        room.abstractRoom.AddEntity(bringThePain);
                        bringThePain.RealizeInRoom();

                        self.room.world.GetResource().ApoEnteringWorld(bringThePain);
                        self.room.abstractRoom.GetResource()?.ApoEnteringRoom(bringThePain, bringThePain.pos);
                    }

                    if (arena.lizardEvent == 99 && arena.painCatLizard)
                    {
                        self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Lizards, -1, 0, 1f);
                        AbstractCreature bringTheTrain = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate("Red Lizard"), null, room.GetWorldCoordinate(shortCutVessel.pos), shortCutVessel.room.world.game.GetNewID()); // Train too big :( 
                        room.abstractRoom.AddEntity(bringTheTrain);
                        bringTheTrain.RealizeInRoom();

                        self.room.world.GetResource().ApoEnteringWorld(bringTheTrain);
                        self.room.abstractRoom.GetResource()?.ApoEnteringRoom(bringTheTrain, bringTheTrain.pos);
                    }
                }

                if ((abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint)
                {
                    if (!arena.sainot) // ascendance saint
                    {
                        (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 0;
                    }
                    else
                    {
                        (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 1;

                    }
                }
            }

            if (ModManager.Watcher && (abstractCreature.realizedCreature as Player).SlugCatClass == Watcher.WatcherEnums.SlugcatStatsName.Watcher)
            {
                (abstractCreature.realizedCreature as Player).enterIntoCamoDuration = 40;
            }



            self.playersSpawned = true;
            arena.playerEnteredGame++;
            foreach (var player in arena.arenaSittingOnlineOrder)
            {
                OnlinePlayer? getPlayer = ArenaHelpers.FindOnlinePlayerByLobbyId(player);
                if (getPlayer != null)
                {
                    if (!getPlayer.isMe)
                    {
                        getPlayer.InvokeOnceRPC(ArenaRPCs.Arena_IncrementPlayersJoined);
                    }
                }
            }
            if (OnlineManager.lobby.isOwner)
            {
                arena.isInGame = true; // used for readied players at the beginning
                arena.leaveForNextLevel = false;
                if (arena.playersLateWaitingInLobbyForNextRound.Count > 0)
                {
                    foreach (var p in arena.playersLateWaitingInLobbyForNextRound)
                    {
                        OnlinePlayer? onlineP = ArenaHelpers.FindOnlinePlayerByLobbyId(p);
                        if (onlineP != null)
                        {
                            onlineP.InvokeOnceRPC(ArenaRPCs.Arena_NotifyRejoinAllowed, true);
                        }
                    }
                }
                foreach (var arenaPlayer in self.arenaSitting.players)
                {
                    if (!arena.playerNumberWithKills.ContainsKey(arenaPlayer.playerNumber))
                    {
                        arena.playerNumberWithKills.Add(arenaPlayer.playerNumber, 0);
                    }
                    if (!arena.playerNumberWithDeaths.ContainsKey(arenaPlayer.playerNumber))
                    {
                        arena.playerNumberWithDeaths.Add(arenaPlayer.playerNumber, 0);
                    }
                    if (!arena.playerNumberWithWins.ContainsKey(arenaPlayer.playerNumber))
                    {
                        arena.playerNumberWithWins.Add(arenaPlayer.playerNumber, 0);
                    }
                }
                arena.playersLateWaitingInLobbyForNextRound.Clear();


            }
            arena.hasPermissionToRejoin = false;




        }

        public virtual void ArenaSessionUpdate(ArenaOnlineGameMode arena, ArenaGameSession session)
        {

        }

    }
}
