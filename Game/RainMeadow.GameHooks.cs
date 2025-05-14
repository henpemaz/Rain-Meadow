using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Linq;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        static bool doneCutscene; // HACK: should keep track of local firstTimeRealized for RoomSpecificScripts

        // setup things
        // prevent creature spawns
        private void GameHooks()
        {
            On.Futile.OnApplicationQuit += Futile_OnApplicationQuit;
            On.RainWorldGame.ctor += RainWorldGame_ctor;
            IL.RainWorldGame.ctor += RainWorldGame_ctor2;
            On.StoryGameSession.ctor += StoryGameSession_ctor;
            On.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
            On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;
            IL.ShortcutHandler.SuckInCreature += ShortcutHandler_SuckInCreature;

            On.Options.GetSaveFileName_SavOrExp += Options_GetSaveFileName_SavOrExp;
            On.PlayerProgression.CopySaveFile += PlayerProgression_CopySaveFile;
            On.Menu.BackupManager.RestoreSaveFile += BackupManager_RestoreSaveFile;

            On.RegionState.AdaptWorldToRegionState += RegionState_AdaptWorldToRegionState;

            On.Room.ctor += Room_ctor;
            IL.Room.LoadFromDataString += Room_LoadFromDataString;
            IL.Room.Loaded += Room_Loaded;
            On.Room.Loaded += Room_LoadedCheck;
            On.Room.PlaceQuantifiedCreaturesInRoom += Room_PlaceQuantifiedCreaturesInRoom;

            On.RoomSettings.ctor_Room_string_Region_bool_bool_Timeline_RainWorldGame += RoomSettings_ctor_Room_string_Region_bool_bool_Timeline_RainWorldGame;

            On.RoomSpecificScript.AddRoomSpecificScript += RoomSpecificScript_AddRoomSpecificScript;

            IL.RoomSpecificScript.SS_E08GradientGravity.Update += RoomSpecificScript_SS_E08GradientGravity_Update;
            On.AntiGravity.BrokenAntiGravity.Update += AntiGravity_BrokenAntiGravity_Update;

            On.FliesWorldAI.AddFlyToSwarmRoom += FliesWorldAI_AddFlyToSwarmRoom;

            // can't pause it's online mom
            new Hook(typeof(RainWorldGame).GetProperty("GamePaused").GetGetMethod(), this.RainWorldGame_GamePaused);

            IL.RainWorldGame.Update += RainWorldGame_Update;
            On.RainWorldGame.Update += RainWorldGame_Update1;

            // Arena specific
            On.GameSession.AddPlayer += GameSession_AddPlayer;
        }

        private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            if (OnlineManager.lobby != null)
            {
                OnlineManager.lobby.gameMode.PreGameStart();
            }
            orig(self, manager);
            if (OnlineManager.lobby != null)
            {
                OnlineManager.lobby.gameMode.PostGameStart(self);
            }
        }

        private void RainWorldGame_ctor2(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                // pole mimics are the last AbstractCreature to be created, whereas pink lizards are the first
                ILLabel pmLoop = null;
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchCallOrCallvirt<RainWorldGame>("get_setupValues"),
                    i => i.MatchLdfld<RainWorldGame.SetupValues>("poleMimics"),
                    i => i.MatchBlt(out pmLoop)
                );
                c.MoveAfterLabels();
                c.MarkLabel(skip);
                c.GotoPrev(moveType: MoveType.Before,
                    i => i.MatchLdarg(0),
                    i => i.MatchCallOrCallvirt<RainWorldGame>("get_world"),
                    i => i.MatchLdloc(0),
                    i => i.MatchCallOrCallvirt<World>("GetAbstractRoom"),
                    i => i.MatchLdarg(0),
                    i => i.MatchCallOrCallvirt<RainWorldGame>("get_world"),
                    i => i.MatchLdstr("Pink Lizard")
                );
                // eligibility criteria; if we are not eligibile to create objects, we skip over the entire AbstractCreature creation process
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((RainWorldGame self) => OnlineManager.lobby == null || (WorldSession.map.TryGetValue(self.world, out var ws) && ws.isOwner));
                c.Emit(OpCodes.Brfalse, skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void RainWorldGame_Update(ILContext il)
        {
            try
            {
                // if chat is open, moves pausing logic to RawUpdate for consistent input detection
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                c.GotoNext(
                    i => i.MatchLdloc(0),
                    i => i.MatchBrfalse(out var _),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<RainWorldGame>("lastPauseButton"),
                    i => i.MatchBrfalse(out var _),
                    i => i.MatchCall<Kittehface.Framework20.Platform>("get_systemMenuShowing"),
                    i => i.MatchBrfalse(out skip)
                );
                c.MoveAfterLabels();
                c.EmitDelegate(() => OnlineManager.lobby != null && OnlineManager.lobby.gameMode is not MeadowGameMode && ChatTextBox.blockInput);
                c.Emit(OpCodes.Brtrue_S, skip);

                // no construct pause menu if pause menu already there!
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchStfld<RainWorldGame>("pauseMenu")
                    );
                c.GotoPrev(moveType: MoveType.After,
                    i => i.MatchBrfalse(out skip)
                    );
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((RainWorldGame self) =>
                {
                    return OnlineManager.lobby == null || self.pauseMenu == null;
                }
                );
                c.Emit(OpCodes.Brfalse, skip);

                // special pausemenu for special needs
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((RainWorldGame self) =>
                {
                    if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode.CustomPauseMenu(self.manager, self) is Menu.PauseMenu pauseMenu)
                    {
                        self.pauseMenu = pauseMenu;
                        return true;
                    }
                    return false;
                }
                );
                c.Emit(OpCodes.Brtrue, skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void RainWorldGame_Update1(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            if (OnlineManager.lobby?.gameMode is MeadowGameMode)
            {
                // fast travel init means save-and-restart on load, which uses player[0]
                if (self.manager.menuSetup.FastTravelInitCondition)
                {
                    self.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;
                    self.manager.blackDelay = 0;
                }
            }

            orig(self);

            if (OnlineManager.lobby?.gameMode is MeadowGameMode mgm)
            {
                MeadowProgression.progressionData.currentCharacterProgress.timePlayed += 1000 / self.framesPerSecond;
                // every 5 minutes
                if (self.manager.upcomingProcess == null && self.clock % (5 * 60 * 40) == 0)
                {
                    MeadowProgression.progressionData.currentCharacterProgress.saveLocation = mgm.avatars[0].apo.pos;
                    MeadowProgression.AutosaveProgression();
                }
            }
        }

        public bool RainWorldGame_GamePaused(Func<RainWorldGame, bool> orig, RainWorldGame self)
        {
            if (OnlineManager.lobby != null)
            {
                // todo we could do very fancy things with the (story) lobby owner being able to pause etc
                return false; // it's online mom
            }
            return orig(self);
        }

        private void Futile_OnApplicationQuit(On.Futile.orig_OnApplicationQuit orig, Futile self)
        {
            //TODO: Impliment graceful exist
            orig(self);
        }

        private string Options_GetSaveFileName_SavOrExp(On.Options.orig_GetSaveFileName_SavOrExp orig, Options self)
        {
            if (OnlineManager.lobby != null)
            {
                return "online_" + orig(self);
            }
            return orig(self);
        }

        private void PlayerProgression_CopySaveFile(On.PlayerProgression.orig_CopySaveFile orig, PlayerProgression self, string sourceName, string destinationDirectory)
        {
            orig(self, sourceName, destinationDirectory);
            orig(self, "online_" + sourceName, destinationDirectory);
        }

        private void BackupManager_RestoreSaveFile(On.Menu.BackupManager.orig_RestoreSaveFile orig, Menu.BackupManager self, string sourceName)
        {
            orig(self, sourceName);
            orig(self, "online_" + sourceName);
        }

        private void Room_PlaceQuantifiedCreaturesInRoom(On.Room.orig_PlaceQuantifiedCreaturesInRoom orig, Room self, CreatureTemplate.Type critType)
        {
            // don't place if not roomsession owner
            if (OnlineManager.lobby != null && RoomSession.map.TryGetValue(self.abstractRoom, out var rs) && !rs.isOwner) return;
            orig(self, critType);
        }

        private void Room_LoadedCheck(On.Room.orig_Loaded orig, Room self)
        {
            var isFirstTimeRealized = self.abstractRoom.firstTimeRealized;
            orig(self);

            if (OnlineManager.lobby != null)
            {
                if (!RoomSession.map.TryGetValue(self.abstractRoom, out var rs)) return;
                if (!WorldSession.map.TryGetValue(self.world, out var ws)) return;

                if (!ws.isOwner)
                {

                    if (self.abstractRoom.firstTimeRealized != isFirstTimeRealized)
                    {
                        ws.owner.InvokeRPC(rs.AbstractRoomFirstTimeRealized);
                    }
                }
            }
        }

        private void RoomSettings_ctor_Room_string_Region_bool_bool_Timeline_RainWorldGame(On.RoomSettings.orig_ctor_Room_string_Region_bool_bool_Timeline_RainWorldGame orig, RoomSettings self, Room room, string name, Region region, bool template, bool firstTemplate, SlugcatStats.Timeline timelinePoint, RainWorldGame game)
        {
            if (isStoryMode(out var storyGameMode))
            {
                timelinePoint = SlugcatStats.SlugcatToTimeline(storyGameMode.currentCampaign);
            }

            orig(self, room, name, region, template, firstTemplate, timelinePoint, game);
        }

        private void RoomSpecificScript_AddRoomSpecificScript(On.RoomSpecificScript.orig_AddRoomSpecificScript orig, Room room)
        {
            if (isStoryMode(out var storyGameMode))
            {
                var firstTimeRealized = room.abstractRoom.firstTimeRealized;
                if (!doneCutscene && room.abstractRoom.name == SaveState.GetStoryDenPosition(storyGameMode.currentCampaign, out _))
                {
                    room.abstractRoom.firstTimeRealized = !doneCutscene;
                    doneCutscene = true;
                }
                orig(room);
                room.abstractRoom.firstTimeRealized = firstTimeRealized;
            }
            else
            {
                orig(room);
            }
        }

        private void RoomSpecificScript_SS_E08GradientGravity_Update(ILContext il)
        {
            try
            {
                // if (room.physicalObjects[i][j] is Player)
                //becomes
                // if (room.physicalObjects[i][j] is local Player)
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchIsinst<Player>()
                    );
                c.EmitDelegate((Player? player) => player?.IsLocal() is true ? player : null);  // null if remote player
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void AntiGravity_BrokenAntiGravity_Update(On.AntiGravity.BrokenAntiGravity.orig_Update orig, AntiGravity.BrokenAntiGravity self)
        {
            if (OnlineManager.lobby != null && self.game.world.GetResource() is WorldSession ws && !ws.isOwner)
            {
                if (ws.state != null)
                {
                    self.counter = (self.on == (ws.state as WorldSession.WorldState).rainCycleData.antiGravity) ? self.cycleMin * 40 : 0;
                }
            }
            orig(self);
        }

        private void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
        {
            if (OnlineManager.lobby != null)
            {
                doneCutscene = false;
                saveStateNumber = OnlineManager.lobby.gameMode.GetStorySessionPlayer(game);
            }
            orig(self, saveStateNumber, game);
        }

        private void RainWorldGame_RawUpdate(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            var closeChat = false;
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is not MeadowGameMode && !self.lastPauseButton && ChatTextBox.blockInput)
            {
                ChatTextBox.blockInput = false;
                if (RWInput.CheckPauseButton(0) || UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Escape))
                {
                    closeChat = true;
                    self.lastPauseButton = true;
                }
                ChatTextBox.blockInput = true;
            }
            orig(self, dt);
            // riskier chat stuff is run after orig, to minimize chances of orig not being run if things go wrong
            if(closeChat)
            {
                self.cameras[0]?.hud.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                ChatTextBox.InvokeShutDownChat();
                if ((self.cameras[0].hud.parts.Find(x => x is ChatHud) is ChatHud hud) && !hud.showChatLog)
                {
                    hud.ShutDownChatLog();
                }
            }
            if (OnlineManager.lobby != null)
            {
                DebugOverlay.Update(self, dt);
                MeadowMusic.RawUpdate(self, dt);
            }
        }

        private void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
        {
            orig(self);
            if (OnlineManager.lobby != null)
            {
                DebugOverlay.RemoveOverlay(self);

                OnlineManager.lobby.gameMode.GameShutDown(self);

                if (!WorldSession.map.TryGetValue(self.world, out var ws)) return;

                if (ws.isActive) ws.Deactivate();
                ws.NotNeeded();
                if (self.manager.upcomingProcess != ProcessManager.ProcessID.MainMenu) // quit directly, otherwise wait release
                {
                    while (ws.isAvailable)
                    {
                        OnlineManager.ForceLoadUpdate();
                    }
                }
            }
        }

        // Don't activate rooms on other slugs moving around, dumbass
        private void ShortcutHandler_SuckInCreature(ILContext il)
        {
            try
            {
                // if (creature is Player && shortCut.shortCutType == ShortcutData.Type.RoomExit)
                //becomes
                // if (creature is Player && creature.IsLocal() && shortCut.shortCutType == ShortcutData.Type.RoomExit)
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdarg(1),
                    i => i.MatchIsinst<Player>(),
                    i => i.MatchBrfalse(out skip)
                    );
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate((Creature creature) => creature.IsLocal());
                c.Emit(OpCodes.Brfalse, skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        // World loading items/creatures
        private void RegionState_AdaptWorldToRegionState(On.RegionState.orig_AdaptWorldToRegionState orig, RegionState self)
        {
            if (OnlineManager.lobby != null && WorldSession.map.TryGetValue(self.world, out var ws))
            {
                if (!OnlineManager.lobby.gameMode.ShouldLoadCreatures(self.world.game, ws) || !ws.isOwner)
                {
                    self.savedPopulation.Clear();
                    self.saveState.pendingFriendCreatures.Clear(); // maybe these should be let through, but we remove them on leaving the world to sleepscreen?
                }
                if (!ws.isOwner)
                {
                    self.savedObjects.Clear();
                    self.saveState.pendingObjects.Clear();
                }
            }


            orig(self);
        }

        // Prevent gameplay items
        private void Room_ctor(On.Room.orig_ctor orig, Room self, RainWorldGame game, World world, AbstractRoom abstractRoom, bool devUI)
        {
            orig(self, game, world, abstractRoom, devUI);
            if (game != null && OnlineManager.lobby != null)
            {
                OnlineManager.lobby.gameMode.FilterItems(self);
            }
        }

        // Prevent geo item spawn
        private void Room_LoadFromDataString(ILContext il)
        {
            try
            {
                // if (this.world != null && this.game != null && this.abstractRoom.firstTimeRealized && (!this.game.IsArenaSession || this.game.GetArenaGameSession.GameTypeSetup.levelItems))
                //becomes
                // if (this.world != null && this.game != null && this.abstractRoom.firstTimeRealized && (!this.game.IsOnlineSession || session.ShouldSpawnItems()) && (!this.game.IsArenaSession || this.game.GetArenaGameSession.GameTypeSetup.levelItems))
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchCallOrCallvirt<Room>("get_abstractRoom"),
                    i => i.MatchLdfld<AbstractRoom>("firstTimeRealized"),
                    i => i.MatchBrfalse(out skip)
                    );
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((Room self) =>
                {
                    return OnlineManager.lobby != null && RoomSession.map.TryGetValue(self.abstractRoom, out var roomSession) && !OnlineManager.lobby.gameMode.ShouldSpawnRoomItems(self.game, roomSession);
                }
                );
                c.Emit(OpCodes.Brtrue, skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        // Prevent random item spawn
        private void Room_Loaded(ILContext il)
        {
            try
            {
                // if (this.world != null && this.game != null && this.abstractRoom.firstTimeRealized && (!this.game.IsArenaSession || this.game.GetArenaGameSession.GameTypeSetup.levelItems))
                //becomes
                // if (this.world != null && this.game != null && this.abstractRoom.firstTimeRealized && (OnlineManager.lobby == null || gameMode.ShouldSpawnRoomItems()) && (!this.game.IsArenaSession || this.game.GetArenaGameSession.GameTypeSetup.levelItems))
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<Room>("roomSettings"),
                    i => i.MatchCallOrCallvirt<RoomSettings>("get_RandomItemDensity"),
                    i => i.MatchLdcR4(0f),
                    i => i.MatchBleUn(out skip)
                    );
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((Room self) =>
                {
                    // during room.loaded the RoomSession isn't available yet so no point in passing self?
                    return OnlineManager.lobby != null && RoomSession.map.TryGetValue(self.abstractRoom, out var roomSession) && !OnlineManager.lobby.gameMode.ShouldSpawnRoomItems(self.game, roomSession);
                });
                c.Emit(OpCodes.Brtrue, skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        // please dont spawn flies
        private void FliesWorldAI_AddFlyToSwarmRoom(On.FliesWorldAI.orig_AddFlyToSwarmRoom orig, FliesWorldAI self, int spawnRoom)
        {
            if (OnlineManager.lobby != null && !OnlineManager.lobby.gameMode.ShouldSpawnFly(self, spawnRoom))
            {
                // Add to quantified creature to trick ourselves into thinking we spawned them
                // this does NOT affect host 
                AbstractRoom swarmRoom = self.world.GetSwarmRoom(spawnRoom);
                int relevantNode = swarmRoom.NodesRelevantToCreature(StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Fly));
                for (int num = 0; num <= relevantNode; ++num)
                {
                    int node = swarmRoom.CreatureSpecificToCommonNodeIndex(num, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Fly));
                    if (node != -1)
                    {
                        swarmRoom.AddQuantifiedCreature(node, CreatureTemplate.Type.Fly);
                    }
                }
                return;
            }
            orig(self, spawnRoom);
        }

        private void GameSession_AddPlayer(On.GameSession.orig_AddPlayer orig, GameSession self, AbstractCreature player)
        {
            orig(self, player);

            if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not ArenaOnlineGameMode)
            {
                return;
            }


            if (WorldSession.map.TryGetValue(self.game.world, out var ws) && OnlineManager.lobby.gameMode.ShouldSyncAPOInWorld(ws, player))
            {
                ws.ApoEnteringWorld(player);
                ws.roomSessions.First().Value.ApoEnteringRoom(player, player.pos);
            }
        }
    }
}
