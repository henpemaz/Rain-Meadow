using System.Collections.Generic;
using UnityEngine;

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
            On.ArenaSitting.NextLevel += ArenaSitting_NextLevel;

        }

        private void ArenaSitting_NextLevel(On.ArenaSitting.orig_NextLevel orig, ArenaSitting self, ProcessManager manager)
        {
            if (OnlineManager.lobby != null)
            {
                ArenaGameSession getArenaGameSession = (manager.currentMainLoop as RainWorldGame).GetArenaGameSession;

/*                if (manager.currentMainLoop is RainWorldGame)
                {
                    if (self.gameTypeSetup.saveCreatures)
                    {
                        for (int i = 0; i < getArenaGameSession.game.world.NumberOfRooms; i++)
                        {
                            for (int j = 0; j < getArenaGameSession.game.world.GetAbstractRoom(getArenaGameSession.game.world.firstRoomIndex + i).creatures.Count; j++)
                            {
                                if (getArenaGameSession.game.world.GetAbstractRoom(getArenaGameSession.game.world.firstRoomIndex + i).creatures[j].state.alive)
                                {
                                    self.creatures.Add(getArenaGameSession.game.world.GetAbstractRoom(getArenaGameSession.game.world.firstRoomIndex + i).creatures[j]);
                                }
                            }

                            for (int k = 0; k < getArenaGameSession.game.world.GetAbstractRoom(getArenaGameSession.game.world.firstRoomIndex + i).entitiesInDens.Count; k++)
                            {
                                if (getArenaGameSession.game.world.GetAbstractRoom(getArenaGameSession.game.world.firstRoomIndex + i).entitiesInDens[k] is AbstractCreature && (getArenaGameSession.game.world.GetAbstractRoom(getArenaGameSession.game.world.firstRoomIndex + i).entitiesInDens[k] as AbstractCreature).state.alive)
                                {
                                    self.creatures.Add(getArenaGameSession.game.world.GetAbstractRoom(getArenaGameSession.game.world.firstRoomIndex + i).entitiesInDens[k] as AbstractCreature);
                                }
                            }
                        }

                       self.savCommunities = getArenaGameSession.creatureCommunities;
                        self.savCommunities.session = null;
                    }
                    else
                    {
                        self.creatures.Clear();
                        self.savCommunities = null;
                    }

                    self.firstGameAfterMenu = false;
                    if (ModManager.MSC && getArenaGameSession.challengeCompleted)
                    {
                        manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MultiplayerMenu);
                        return;
                    }
                }*/


                ///






                // We need to kick everyone out
                // ArenaGameSession getArenaGameSession = (manager.currentMainLoop as RainWorldGame).GetArenaGameSession;
                AbstractRoom absRoom = getArenaGameSession.game.world.abstractRooms[0];
                Room room = absRoom.realizedRoom;
                WorldSession worldSession = WorldSession.map.GetValue(absRoom.world, (w) => throw new KeyNotFoundException());

                if (RoomSession.map.TryGetValue(absRoom, out var roomSession))
                {
                    // we go over all APOs in the room
                    Debug("Next level switching");
                    var entities = absRoom.entities;
                    for (int i = entities.Count - 1; i >= 0; i--)
                    {
                        if (entities[i] is AbstractPhysicalObject apo && OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
                        {
                            // if they're not ours, they need to be removed from the room SO THE GAME DOESN'T MOVE THEM
                            if (!oe.isMine)
                            {
                                // not-online-aware removal
                                Debug("removing remote entity from game " + oe);
                                oe.beingMoved = true;
                                if (oe.apo.realizedObject is Creature c && c.inShortcut)
                                {
                                    if (c.RemoveFromShortcuts()) c.inShortcut = false;
                                }
                                entities.Remove(oe.apo);
                                absRoom.creatures.Remove(oe.apo as AbstractCreature);

                                // room.RemoveObject(oe.apo.realizedObject);
                                // room.CleanOutObjectNotInThisRoom(oe.apo.realizedObject);
                                oe.beingMoved = false;
                            }
                            else // mine leave the old online world elegantly
                            {
                                Debug("removing my entity from online " + oe);
                                oe.LeaveResource(roomSession);
                                oe.LeaveResource(roomSession.worldSession);
                            }
                        }
                    }



                    try
                    {
                        roomSession.abstractOnDeactivate = true;
                        roomSession.FullyReleaseResource();
                        roomSession.worldSession.FullyReleaseResource();
                        // maybe we need to do this too
                        // TODO: Fix the rendering issue (must not be loading properly when someone else is holding onto the room?)
                        // From Henpe: That's what a room looks like without the shaders, something crashed while it was loading
                        // probably the error of: InvalidOperation: Pending LoadingHooks.cs:247
                        // Also check PlayerOnDie cuz that's unhappy

                    }
                    catch
                    {
                        RainMeadow.Debug("oh boy");
                    }

                    if (roomSession.isPending)
                    {
                        Debug("Arena room pending" + room);
                        roomSession.releaseWhenPossible = true;
                    }


/*                    self.currentLevel++;
                    if (self.currentLevel >= self.levelPlaylist.Count && !self.gameTypeSetup.repeatSingleLevelForever)
                    {
                        manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MultiplayerResults);
                        return;
                    }

                    manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
                    if (self.gameTypeSetup.savingAndLoadingSession)
                    {
                        self.SaveToFile(manager.rainWorld);
                    }*/

                    orig(self, manager);
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
                        rs.abstractOnDeactivate = true;
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
            if (OnlineManager.lobby != null && self.game != null && WorldSession.map.TryGetValue(self.world, out var ws0))
            {
                if (!ws0.isAvailable)
                {
                    lock (self)
                    {
                        self.requestCreateWorld = false;
                        orig(self);
                    }
                    OnlineManager.ForceLoadUpdate();
                    return;
                }
                else if (self.requestCreateWorld)
                {
                    self.setupValues.worldCreaturesSpawn = OnlineManager.lobby.gameMode.ShouldLoadCreatures(self.game, ws0);
                    Debug($"world loading creating new world, worldCreaturesSpawn? {self.setupValues.worldCreaturesSpawn}");
                }
            }
            orig(self);
            if (OnlineManager.lobby != null && self.game != null && WorldSession.map.TryGetValue(self.world, out var ws))
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
            if (OnlineManager.lobby != null && self.game != null)
            {
                WorldSession ws = null;
                if (isArenaMode(out var _))
                {
                    // Arena has null region and single-room world
                    try
                    {
                        // Gets us to the next stage, but not graceful and includes render failures
                        // TODO: Figure out where we need to gracefully kick players out of the game. ArenaSitting.NextLevel?

                        OnlineManager.lobby.worldSessions["arena"].FullyReleaseResource();
                    }
                    catch
                    {
                        RainMeadow.Debug("World is not claimed");
                    }
                    Debug("Requesting arena world resource");
                    ws = OnlineManager.lobby.worldSessions["arena"];

                }
                else
                {
                    Debug("Requesting new region: " + region.name);
                    ws = OnlineManager.lobby.worldSessions[region.name];
                }
                if (ws.isAvailable && ws.releaseWhenPossible) // mid-release
                {
                    while(ws.isAvailable && OnlineManager.lobby != null)
                    {
                        OnlineManager.ForceLoadUpdate();
                        Thread.Sleep(1);
                    }
                }
                ws.Request();
                ws.BindWorld(self.world);
                self.setupValues.worldCreaturesSpawn = OnlineManager.lobby.gameMode.ShouldLoadCreatures(self.game, ws);
            }
        }

        private void ArenaGameSession_Update(On.ArenaGameSession.orig_Update orig, ArenaGameSession self)
        {

            if (self.arenaSitting.attempLoadInGame && self.arenaSitting.gameTypeSetup.savingAndLoadingSession)
            {
                self.arenaSitting.attempLoadInGame = false;
                self.arenaSitting.LoadFromFile(self, self.game.world, self.game.rainWorld);
            }

            if (self.initiated)
            {
                self.counter++;
            }
            else if (self.room != null && self.room.shortCutsReady)
            {
                self.Initiate();
            }

            if (self.room != null && self.chMeta != null && self.chMeta.deferred)
            {
                self.room.deferred = true;
            }

            self.thisFrameActivePlayers = self.PlayersStillActive(addToAliveTime: true, dontCountSandboxLosers: false);

/*            if (!self.sessionEnded)
            {
*//*                if (self.endSessionCounter > 0)
                {
                    if (self.ShouldSessionEnd())
                    {
                        self.endSessionCounter--;
                        if (self.endSessionCounter == 0)
                        {
                            self.EndSession();
                        }
                    }
                    else
                    {
                        self.endSessionCounter = -1;
                    }
                }
                else if (self.endSessionCounter == -1 && self.ShouldSessionEnd())
                {
                    self.endSessionCounter = 30;
                }*//*
            }
*/
            for (int i = 0; i < self.behaviors.Count; i++)
            {
                if (self.behaviors[i].slatedForDeletion)
                {
                    self.RemoveBehavior(i);
                }
                else
                {
                    self.behaviors[i].Update();
                }
            }

            if (self.game.world.rainCycle.TimeUntilRain < -1000 && !self.sessionEnded)
            {
                self.outsidePlayersCountAsDead = false;
                self.EndSession();
            }
            // stop game over by not adding the rest of orig code
        }
    }
}
