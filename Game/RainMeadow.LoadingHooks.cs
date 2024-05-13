namespace RainMeadow
{

    public partial class RainMeadow
    {
        // World/room load unload wait
        private void LoadingHooks()
        {
            On.WorldLoader.ctor_RainWorldGame_Name_bool_string_Region_SetupValues += WorldLoader_ctor;
            On.WorldLoader.Update += WorldLoader_Update;
            On.RoomPreparer.ctor += RoomPreparer_ctor;
            On.RoomPreparer.Update += RoomPreparer_Update;
            On.AbstractRoom.Abstractize += AbstractRoom_Abstractize;
        }

        // Room unload
        private void AbstractRoom_Abstractize(On.AbstractRoom.orig_Abstractize orig, AbstractRoom self)
        {
            if (OnlineManager.lobby != null)
            {
                if (RoomSession.map.TryGetValue(self, out RoomSession rs))
                {
                    if (rs.isAvailable)
                    {
                        Debug("Queueing room release: " + self.name);
                        rs.abstractOnDeactivate = true;
                        rs.FullyReleaseResource();
                        return;
                    }
                    if (rs.isPending)
                    {
                        Debug("Room pending: " + self.name);
                        rs.releaseWhenPossible = true;
                        return;
                    }
                    Debug("Room released: " + self.name);
                }
            }
            orig(self);
        }

        // Room wait and activate
        private void RoomPreparer_Update(On.RoomPreparer.orig_Update orig, RoomPreparer self)
        {
            if (!self.shortcutsOnly && self.room.game != null && OnlineManager.lobby != null)
            {
                if (RoomSession.map.TryGetValue(self.room.abstractRoom, out RoomSession rs))
                {
                    if (true) // force load scenario ????
                    {
                        OnlineManager.ForceLoadUpdate();
                    }
                    if (!rs.isAvailable) return;
                    if (!rs.isActive) rs.Activate();
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
                rs.Request();
            }
        }

        // World wait, activate
        private void WorldLoader_Update(On.WorldLoader.orig_Update orig, WorldLoader self)
        {
            if (OnlineManager.lobby != null && WorldSession.map.TryGetValue(self.world, out var ws0))
            {
                if (!ws0.isAvailable)
                {
                    lock (self) {
                        self.requestCreateWorld = false;
                        orig(self);
                    }
                    OnlineManager.ForceLoadUpdate();
                    return;
                }
                else if(self.requestCreateWorld)
                {
                    self.setupValues.worldCreaturesSpawn = OnlineManager.lobby.gameMode.ShouldLoadCreatures(self.game, ws0);
                    Debug($"world loading creating new world, worldCreaturesSpawn? {self.setupValues.worldCreaturesSpawn}");
                }
            }
            orig(self);
            if (OnlineManager.lobby != null && WorldSession.map.TryGetValue(self.world, out var ws))
            {
                if (self.game.overWorld?.worldLoader != self) // force-load scenario
                {
                    OnlineManager.ForceLoadUpdate();
                }

                // wait until new world state available
                if (self.Finished && !ws.isAvailable)
                {
                    RainMeadow.Error("Region loading finished before online resource is available");
                    self.Finished = false;
                    return;
                }

                // now we need to wait for it before further actions
                if (!self.Finished)
                {
                    return;
                }

                // activate the new world
                if (self.Finished && !ws.isActive)
                {
                    Debug("world loading activating new world");
                    ws.Activate();
                }

                // if there is a gate, the gate's room will be reused, it needs to be made available
                if (self.game.overWorld?.reportBackToGate is RegionGate gate)
                {

                    var newRoom = ws.roomSessions[gate.room.abstractRoom.name];
                    if (!newRoom.isAvailable)
                    {
                        if (!newRoom.isPending)
                        {
                            Debug("world loading requesting new room in next region");
                            newRoom.Request();
                        }
                        self.Finished = false;
                        return;
                    }
                }
            }
        }


        // World request/release
        private void WorldLoader_ctor(On.WorldLoader.orig_ctor_RainWorldGame_Name_bool_string_Region_SetupValues orig, WorldLoader self, RainWorldGame game, SlugcatStats.Name playerCharacter, bool singleRoomWorld, string worldName, Region region, RainWorldGame.SetupValues setupValues)
        {
            if (OnlineManager.lobby != null)
            {
                playerCharacter = OnlineManager.lobby.gameMode.LoadWorldAs(game);
            }
            orig(self, game, playerCharacter, singleRoomWorld, worldName, region, setupValues);
            if (OnlineManager.lobby != null)
            {
                WorldSession ws = null;
                if (game != null && game.IsArenaSession)
                {
                    // Arena has null region and single-room world
                    Debug("Requesting arena world resource");
                    ws = OnlineManager.lobby.worldSessions["arena"];
                }
                else
                {
                    Debug("Requesting new region: " + region.name);
                    ws = OnlineManager.lobby.worldSessions[region.name];
                }
                ws.Request();
                ws.BindWorld(self.world);
                self.setupValues.worldCreaturesSpawn = OnlineManager.lobby.gameMode.ShouldLoadCreatures(self.game, ws);
            }
        }
    }
}
