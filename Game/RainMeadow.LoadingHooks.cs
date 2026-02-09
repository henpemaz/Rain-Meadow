using System;
using System.Collections.Generic;
using System.Linq;
using MonoMod.RuntimeDetour;
namespace RainMeadow
{

    public partial class RainMeadow
    {
        // World/room load unload wait
        private void LoadingHooks()
        {
            On.WorldLoader.ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues += WorldLoader_ctor;

            On.WorldLoader.Update += WorldLoader_Update;
            On.RoomPreparer.Update += RoomPreparer_Update;
            On.RoomPreparer.ctor += RoomPreparer_ctor;
            On.AbstractRoom.Abstractize += AbstractRoom_Abstractize;
            On.ArenaSitting.NextLevel += ArenaSitting_NextLevel;

            new Hook(typeof(RainWorldGame).GetProperty(nameof(RainWorldGame.StoryCharacter)).GetGetMethod(), RainWorldGame_StoryCharacter);
            new Hook(typeof(RainWorldGame).GetProperty(nameof(RainWorldGame.TimelinePoint)).GetGetMethod(), RainWorldGame_TimelinePoint);
        }

        /// <summary>
        /// A centralized coroutine helper that waits for a WorldSession's participants to clear before proceeding.
        /// It enforces a 5-second safety timeout to prevent softlocks (deadlocks) if entities fail to remove.
        /// </summary>
        /// <param name="session">The WorldSession currently transitioning.</param>
        /// <param name="extraWaitCondition">Optional: An additional condition that must remain true to keep waiting (e.g., !newWorldSession.isAvailable). Pass null if not needed.</param>
        /// <param name="onComplete">The action/method to execute once the wait finishes or times out (e.g., orig(self, ...)).</param>
        private System.Collections.IEnumerator WaitAndExecuteSession(
            WorldSession session, 
            System.Func<bool> extraWaitCondition, 
            System.Action onComplete)
        {
            float startTime = UnityEngine.Time.time;
            float timeoutSeconds = 5f;
            session.transitionInProgress = true;

            if (OnlineManager.lobby.gameMode is not MeadowGameMode) {
                while (session.participants.Count > 0 && 
                    (extraWaitCondition == null || extraWaitCondition()) && 
                    (UnityEngine.Time.time - startTime < timeoutSeconds))
                {
                    RainMeadow.Debug($"Waiting for {session.participants.Count} to leave...");
                    yield return null;
                }
            }

            if (UnityEngine.Time.time - startTime >= timeoutSeconds)
            {
                Debug("WaitLoop timed out after 5 seconds. Proceeding anyway to prevent deadlock.");
            }
            else
            {
                Debug("Entities removed. Proceeding...");
            }

            session.transitionInProgress = false;
            onComplete?.Invoke();
        }
        SlugcatStats.Name RainWorldGame_StoryCharacter(Func<RainWorldGame, SlugcatStats.Name> orig, RainWorldGame self)
        {
            if (OnlineManager.lobby != null) return OnlineManager.lobby.gameMode.LoadWorldAs(self);
            return orig(self);
        }

        SlugcatStats.Timeline RainWorldGame_TimelinePoint(Func<RainWorldGame, SlugcatStats.Timeline> orig, RainWorldGame self)
        {
            if (OnlineManager.lobby != null) return OnlineManager.lobby.gameMode.LoadWorldIn(self);
            return orig(self);
        }
        private System.Collections.IEnumerator ArenaNextLevel_WaitLoop(On.ArenaSitting.orig_NextLevel orig, ArenaSitting self, ProcessManager manager, WorldSession oldWorldSession)
        {
            return WaitAndExecuteSession(
                oldWorldSession, 
                null, 
                () => self.NextLevel(manager) 
            );
        }
        private void ArenaSitting_NextLevel(On.ArenaSitting.orig_NextLevel orig, ArenaSitting self, ProcessManager manager)
        {
            if (isArenaMode(out var arena))
            {
                if (OnlineManager.lobby.isOwner)
                {
                    arena.leaveForNextLevel = true;
                }


                arena.externalArenaGameMode.ArenaSessionNextLevel(arena, orig, self, manager);

                ArenaGameSession getArenaGameSession = (manager.currentMainLoop as RainWorldGame).GetArenaGameSession;
                AbstractRoom absRoom = getArenaGameSession.game.world.abstractRooms[0];
                Room room = absRoom.realizedRoom;
                WorldSession worldSession = WorldSession.map.TryGetValue(absRoom.world, out var ws)  ?  ws :  OnlineManager.lobby.overworld.worldSessions.TryGetValue("arena", out var ws2) ? ws2 : null;
                if (worldSession.transitionInProgress)
                {
                    return;
                }

                for (int i = arena.arenaSittingOnlineOrder.Count - 1; i >= 0; i--)
                {
                    OnlinePlayer? missingPlayer = ArenaHelpers.FindOnlinePlayerByLobbyId(arena.arenaSittingOnlineOrder[i]);
                    if (missingPlayer == null)
                    {
                        arena.arenaSittingOnlineOrder.RemoveAt(i);
                    }
                }

                foreach (var player in self.players)
                {
                    OnlinePlayer? currentName = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, player.playerNumber);
                    if (currentName != null)
                    {
                        arena.ReadFromStats(player, currentName);
                    }
                }


                if (RoomSession.map.TryGetValue(absRoom, out var roomSession))
                {
                    // we go over all APOs in the room
                    Debug("Next level switching");
                        RainMeadow.Debug("Unsubscribing from old world");
                        if (worldSession.isActive) {
                        worldSession.Deactivate();
                        worldSession.NotNeeded(); 
                        }

                    if (worldSession.participants.Count > 0)
                    {
                        if (OnlineManager.lobby.isOwner) {
                        Debug($"Waiting for {worldSession.participants.Count} players to leave...");
                        } else
                        {
                         Debug($"Waiting for host  players to join new world...");
                        }
                        manager.rainWorld.StartCoroutine(ArenaNextLevel_WaitLoop(orig, self, manager, worldSession));
                        worldSession.transitionInProgress = true;
                        return; 
                    }

                    if (manager.currentMainLoop is RainWorldGame)
                    {

                        self.creatures.Clear();
                        self.savCommunities = null;

                        self.firstGameAfterMenu = false;

                        if (ModManager.MSC && getArenaGameSession.challengeCompleted)
                        {
                            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MultiplayerMenu);
                            self.players.Clear();
                            return;
                        }
                    }

                    RainMeadow.Debug("Arena: Moving to next level");
                    self.currentLevel++;
                    if (OnlineManager.lobby.isOwner)
                    {
                        arena.currentLevel = self.currentLevel;
                    }

                    if (self.currentLevel >= arena.playList.Count && !self.gameTypeSetup.repeatSingleLevelForever)
                    {
                        manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MultiplayerResults);
                        return;
                    }

                    List<OnlinePlayer> waitingPlayers = [.. OnlineManager.players.Where(x => ArenaHelpers.GetArenaClientSettings(x)?.ready == true && !x.isMe)];

                    self.players.Clear();
                    for (int i = 0; i < arena.arenaSittingOnlineOrder.Count; i++)
                    {
                        OnlinePlayer? pl = ArenaHelpers.FindOnlinePlayerByLobbyId(arena.arenaSittingOnlineOrder[i]);
                        if (pl != null)
                        {
                            ArenaSitting.ArenaPlayer newArenaPlayer = new(i)
                            {
                                playerNumber = i,
                                playerClass = ArenaHelpers.GetArenaClientSettings(pl)!.playingAs,
                                hasEnteredGameArea = true
                            };

                            Debug($"Arena: Local Sitting Data: {newArenaPlayer.playerNumber}: {newArenaPlayer.playerClass}");
                            arena.AddOrInsertPlayerStats(arena, newArenaPlayer, pl);

                            self.players.Add(newArenaPlayer);
                        }
                    }

                    // Add waiting players
                    if (arena.allowJoiningMidRound)
                    {
                        foreach (OnlinePlayer player in waitingPlayers)
                        {
                            if (player != null) // always gotta check in case something happened to them
                            {
                                if (!arena.arenaSittingOnlineOrder.Contains(player.inLobbyId) && OnlineManager.lobby.isOwner)
                                {
                                    arena.arenaSittingOnlineOrder.Add(player.inLobbyId);
                                }
                                ArenaSitting.ArenaPlayer newArenaPlayer = new(arena.arenaSittingOnlineOrder.Count - 1)
                                {
                                    playerNumber = arena.arenaSittingOnlineOrder.Count - 1,
                                    playerClass = ArenaHelpers.GetArenaClientSettings(player)!.playingAs,
                                    hasEnteredGameArea = true
                                };
                                Debug($"Arena: Local Sitting Data: {newArenaPlayer.playerNumber}: {newArenaPlayer.playerClass}");
                                arena.AddOrInsertPlayerStats(arena, newArenaPlayer, player);
                                self.players.Add(newArenaPlayer);
                            }
                        }
                    }


                    manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);

                }
            }
            else
            {
                orig(self, manager);
            }
        }

        // Room unload
        private void AbstractRoom_Abstractize(On.AbstractRoom.orig_Abstractize orig, AbstractRoom self)
        {
            if (OnlineManager.lobby != null)
            {
                if (RoomSession.map.TryGetValue(self, out RoomSession rs))
                {
                    if (rs.isActive) rs.Deactivate();
                    rs.NotNeeded();
                    Debug("Room released: " + self.name);
                    // room release needs to be instant, because the game just checks room != null in realizer logic
                    foreach (AbstractWorldEntity? item in self.entities.Concat(self.entitiesInDens))
                    {
                        if (item is AbstractPhysicalObject apo && OnlinePhysicalObject.map.TryGetValue(apo, out var ent))
                        {
                            ent.beingMoved = true;
                        }
                    }
                    self.world.loadingRooms.RemoveAll(rl => rl.room == self.realizedRoom);
                    orig(self);
                    foreach (AbstractWorldEntity? item in self.entities.Concat(self.entitiesInDens))
                    {
                        if (item is AbstractPhysicalObject apo && OnlinePhysicalObject.map.TryGetValue(apo, out var ent))
                        {
                            ent.beingMoved = false;
                        }
                    }
                    return;
                }
            }
            orig(self);
        }

        // Room wait and activate
        private void RoomPreparer_Update(On.RoomPreparer.orig_Update orig, RoomPreparer self)
        {
            if (OnlineManager.lobby != null)
            {
                if (RoomSession.map.TryGetValue(self.room.abstractRoom, out RoomSession rs))
                {

                    if (!self.shortcutsOnly && self.room.game != null)
                    {
                        RainMeadow.Trace($"{rs} : {rs.isPending} {rs.isAvailable} {rs.isActive}");
                        rs.Needed();
                        if (!rs.isAvailable || rs.isPending) return;
                        if ((self.requestShortcutsReady || self.room.shortCutsReady) && !rs.isActive) rs.Activate();
                    }
                }

            }
            orig(self);
        }

        // Room request
        private void RoomPreparer_ctor(On.RoomPreparer.orig_ctor orig, RoomPreparer self, Room room, bool loadAiHeatMaps, bool falseBake, bool shortcutsOnly)
        {
            orig(self, room, loadAiHeatMaps, falseBake, shortcutsOnly);
            if (!shortcutsOnly && room.game != null && OnlineManager.lobby != null && RoomSession.map.TryGetValue(room.abstractRoom, out var rs))
            {
                rs.Needed();
            }
        }

        // World wait, activate
        private void WorldLoader_Update(On.WorldLoader.orig_Update orig, WorldLoader self)
        {
            if (OnlineManager.lobby != null && self.game != null && WorldSession.map.TryGetValue(self.world, out var ws0))
            {
                RainMeadow.Trace($"{ws0} : {ws0.isPending} {ws0.isAvailable} {ws0.isActive}");
                ws0.Needed();
                if (!ws0.isAvailable || ws0.isPending)
                {
                    if (self.game.overWorld.activeWorld == null)
                    {
                        OnlineManager.ForceLoadUpdate();
                    }
                    // no processing while not available
                    return;
                }
            }
            orig(self);
            if (OnlineManager.lobby != null && self.game != null && WorldSession.map.TryGetValue(self.world, out var ws))
            {
                // activate the new world
                if (self.Finished && !ws.isActive)
                {
                    Debug("world loading activating new world");
                    ws.Activate();
                }

                // if there is a gate, the gate's room will be reused, it needs to be made available
                if (ws.isActive && self.game.overWorld?.reportBackToGate is RegionGate gate)
                {
                    var newRoom = ws.roomSessions[gate.room.abstractRoom.name];
                    newRoom.Needed();
                    if (!newRoom.isAvailable)
                    {
                        self.Finished = false;
                        return;
                    }
                }
            }
        }

        // World request/release
        private void WorldLoader_ctor(On.WorldLoader.orig_ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues orig, WorldLoader self, RainWorldGame game, SlugcatStats.Name playerCharacter, SlugcatStats.Timeline timeline, bool singleRoomWorld, string worldName, Region region, RainWorldGame.SetupValues setupValues)
        {
            if (OnlineManager.lobby != null)
            {
                playerCharacter = OnlineManager.lobby.gameMode.LoadWorldAs(game);
                timeline = OnlineManager.lobby.gameMode.LoadWorldIn(game);
            }
            orig(self, game, playerCharacter, timeline, singleRoomWorld, worldName, region, setupValues);
            if (OnlineManager.lobby != null && self.game != null)
            {
                try
                {
                    WorldSession ws = OnlineManager.lobby.gameMode.LinkWorld(self.world);
                    ws.BindWorld(self, self.world);
                }
                // TODO: Why?
                catch (System.NullReferenceException e) // happens in riv ending
                {
                    RainMeadow.Error(e);
                    RainMeadow.Error("rivulet hackfix null ref exception is bad!");
                }
            }
        }
    }
}