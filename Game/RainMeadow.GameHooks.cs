using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        // setup things
        // prevent creature spawns
        private void GameHooks()
        {
            On.Futile.OnApplicationQuit += Futile_OnApplicationQuit;
            On.RainWorldGame.ctor += RainWorldGame_ctor;
            On.StoryGameSession.ctor += StoryGameSession_ctor;
            On.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
            On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;
            IL.ShortcutHandler.SuckInCreature += ShortcutHandler_SuckInCreature;

            On.RegionState.AdaptWorldToRegionState += RegionState_AdaptWorldToRegionState;

            On.World.LoadWorld += World_LoadWorld;

            On.Room.ctor += Room_ctor;
            IL.Room.LoadFromDataString += Room_LoadFromDataString;
            IL.Room.Loaded += Room_Loaded;
            On.Room.Loaded += Room_LoadedCheck;
            On.Room.PlaceQuantifiedCreaturesInRoom += Room_PlaceQuantifiedCreaturesInRoom;

            On.RoomSettings.ctor += RoomSettings_ctor;

            IL.RoomSpecificScript.SS_E08GradientGravity.Update += RoomSpecificScript_SS_E08GradientGravity_Update;

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
                OnlineManager.lobby.gameMode.clientSettings.inGame = true;
            }
            orig(self, manager);
        }

        private void World_LoadWorld(On.World.orig_LoadWorld orig, World self, SlugcatStats.Name slugcatNumber, System.Collections.Generic.List<AbstractRoom> abstractRoomsList, int[] swarmRooms, int[] shelters, int[] gates)
        {
            orig(self, slugcatNumber, abstractRoomsList, swarmRooms, shelters, gates);

            if (OnlineManager.lobby != null)
                OnlineManager.lobby.gameMode.LobbyReadyCheck();
        }

        private void RainWorldGame_Update(ILContext il)
        {
            try
            {
                // no construct pause menu if pause menu already there!
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
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
                    if(OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode mgm)
                    {
                        self.pauseMenu = new MeadowPauseMenu(self.manager, self, mgm);
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
                    MeadowProgression.progressionData.currentCharacterProgress.saveLocation = mgm.avatar.apo.pos;
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


        private void Room_PlaceQuantifiedCreaturesInRoom(On.Room.orig_PlaceQuantifiedCreaturesInRoom orig, Room self, CreatureTemplate.Type critType)
        {
            if (OnlineManager.lobby != null)
            {
                if (RoomSession.map.TryGetValue(self.abstractRoom, out var rs))
                {
                    if (!rs.isOwner && critType == CreatureTemplate.Type.Fly)
                    {
                        return; // don't place fly in room if not owner
                    }
                }
            }
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

        private void RoomSettings_ctor(On.RoomSettings.orig_ctor orig, RoomSettings self, string name, Region region, bool template, bool firstTemplate, SlugcatStats.Name playerChar)
        {
            if (isStoryMode(out var storyGameMode))
            {
                playerChar = storyGameMode.currentCampaign;
            }

            orig(self, name, region, template, firstTemplate, playerChar);
        }

        private void RoomSpecificScript_SS_E08GradientGravity_Update(ILContext il)
        {
            try
            {
                // if (room.physicalObjects[i][j] is Player)
                //becomes
                // if (room.physicalObjects[i][j] is Player && !(OnlineManager.lobby != null && !(OnlinePhysicalObject.map.TryGetValue(self.room.physicalObjects[i][j].abstractPhysicalObject, out var oe) && oe.isMine)))
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchIsinst<Player>(),
                    i => i.MatchBrfalse(out skip)
                    );
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_0);
                c.Emit(OpCodes.Ldloc_1);
                c.EmitDelegate((RoomSpecificScript.SS_E08GradientGravity self, int i, int j) => {
                    return OnlineManager.lobby != null && !(OnlinePhysicalObject.map.TryGetValue(self.room.physicalObjects[i][j].abstractPhysicalObject, out var oe) && oe.isMine);
                });
                c.Emit(OpCodes.Brtrue, skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
        {
            if (OnlineManager.lobby != null)
            {
                saveStateNumber = OnlineManager.lobby.gameMode.GetStorySessionPlayer(game);
                if (isStoryMode(out var story))
                {
                    story.storyClientSettings.isDead = false;
                }
            }
            orig(self, saveStateNumber, game);
        }

        private void RainWorldGame_RawUpdate(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            orig(self, dt);
            if (OnlineManager.lobby != null)
            {
                DebugOverlay.Update(self, dt);
            }
        }

        private void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
        {
            orig(self);
            if (OnlineManager.lobby != null)
            {
                DebugOverlay.RemoveOverlay(self);

                OnlineManager.lobby.gameMode.clientSettings.inGame = false;
                if (isStoryMode(out var story))
                {
                    story.storyClientSettings.inGame = false;
                }

                if (OnlineManager.lobby.gameMode is MeadowGameMode mgm)
                {
                    MeadowProgression.progressionData.currentCharacterProgress.saveLocation = mgm.avatar.apo.pos;
                    MeadowProgression.SaveProgression();
                    self.rainWorld.progression.SaveToDisk(false, true, false); // save maps
                }

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
                // if (creature is Player && ((Player) creature).playerState.slugcatCharacter == Ext_SlugcatStatsName.OnlineSessionRemotePlayer && shortCut.shortCutType == ShortcutData.Type.RoomExit)
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdarg(1),
                    i => i.MatchIsinst<Player>(),
                    i => i.MatchBrfalse(out skip)
                    );
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate((Creature creature) => { return OnlineManager.lobby != null && ((Player)creature).playerState.slugcatCharacter == Ext_SlugcatStatsName.OnlineSessionRemotePlayer; });
                c.Emit(OpCodes.Brtrue, skip);
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
        private void Room_ctor(On.Room.orig_ctor orig, Room self, RainWorldGame game, World world, AbstractRoom abstractRoom)
        {
            orig(self, game, world, abstractRoom);
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
                return;
            }
            orig(self, spawnRoom);
        }

        private void GameSession_AddPlayer(On.GameSession.orig_AddPlayer orig, GameSession self, AbstractCreature player)
        {
            orig(self, player);

            if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not ArenaCompetitiveGameMode)
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
