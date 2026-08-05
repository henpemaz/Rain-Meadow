using System;
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

            On.RoomRealizer.CanAbstractizeRoom += RoomRealizer_CanAbstractizeRoom2;
        }

        public bool RoomRealizer_CanAbstractizeRoom2(On.RoomRealizer.orig_CanAbstractizeRoom orig, RoomRealizer self, RoomRealizer.RealizedRoomTracker tracker)
        {
            if (OnlineManager.lobby != null && tracker.room.GetResource() is RoomSession roomSession)
            {
                if (roomSession.isActive)
                {
                    if (roomSession.activeEntities.Any(x => !x.isTransferable && x.isMine)) return false;
                }
            }

            return orig(self, tracker);
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

        private void ArenaSitting_NextLevel(
            On.ArenaSitting.orig_NextLevel orig,
            ArenaSitting self,
            ProcessManager manager
        )
        {
            if (isArenaMode(out ArenaOnlineGameMode arenaOnline))
            {
                arenaOnline.externalArenaGameMode.On_ArenaSitting_NextLevel(arenaOnline, orig, self, manager);
                return;
            }

            orig(self, manager);
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
