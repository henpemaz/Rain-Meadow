using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Linq;
namespace RainMeadow
{

    public partial class RainMeadow
    {
        // World/room load unload wait
        private void LoadingHooks()
        {
            On.WorldLoader.ctor_RainWorldGame_Name_bool_string_Region_SetupValues += WorldLoader_ctor;
            On.WorldLoader.Update += WorldLoader_Update;
            On.RoomPreparer.Update += RoomPreparer_Update;
            On.RoomPreparer.ctor += RoomPreparer_ctor;
            On.AbstractRoom.Abstractize += AbstractRoom_Abstractize;
            On.ArenaSitting.NextLevel += ArenaSitting_NextLevel;
        }

        private void ArenaSitting_NextLevel(On.ArenaSitting.orig_NextLevel orig, ArenaSitting self, ProcessManager manager)
        {
            if (isArenaMode(out var arena))
            {
                ArenaGameSession getArenaGameSession = (manager.currentMainLoop as RainWorldGame).GetArenaGameSession;


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
                            oe.apo.LoseAllStuckObjects();
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

                                room.RemoveObject(oe.apo.realizedObject);
                                room.CleanOutObjectNotInThisRoom(oe.apo.realizedObject);
                                oe.beingMoved = false;
                            }
                            else // mine leave the old online world elegantly
                            {
                                Debug("removing my entity from online " + oe);
                                oe.ExitResource(roomSession);
                                oe.ExitResource(roomSession.worldSession);
                            }
                        }
                    }

                    if (manager.currentMainLoop is RainWorldGame)
                    {

                        self.creatures.Clear();
                        self.savCommunities = null;

                        self.firstGameAfterMenu = false;

                        if (ModManager.MSC && getArenaGameSession.challengeCompleted)
                        {
                            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MultiplayerMenu);
                            if (!OnlineManager.lobby.isOwner)
                            {
                                OnlineManager.lobby.owner.InvokeRPC(ArenaRPCs.Arena_ResetPlayersLeft);
                            }
                            self.players.Clear();
                            return;
                        }
                    }

                    RainMeadow.Debug("Arena: Moving to next level");
                    self.currentLevel++;

                    if (self.currentLevel >= arena.playList.Count && !self.gameTypeSetup.repeatSingleLevelForever)
                    {

                        manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MultiplayerResults);

                        if (!OnlineManager.lobby.isOwner)
                        {
                            OnlineManager.lobby.owner.InvokeRPC(ArenaRPCs.Arena_ResetPlayersLeft);
                        }

                        return;
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
            if (!self.shortcutsOnly && self.room.game != null && OnlineManager.lobby != null)
            {
                if (RoomSession.map.TryGetValue(self.room.abstractRoom, out RoomSession rs))
                {
                    RainMeadow.Trace($"{rs} : {rs.isPending} {rs.isAvailable} {rs.isActive}");
                    rs.Needed();
                    if (!rs.isAvailable || rs.isPending) return;
                    if ((self.requestShortcutsReady || self.room.shortCutsReady) && !rs.isActive) rs.Activate();
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
                    lock (self)
                    {
                        self.requestCreateWorld = false;
                        orig(self);
                    }
                    if (self.game.overWorld == null)
                    {
                        OnlineManager.ForceLoadUpdate();
                    }
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
                // activate the new world
                if (self.Finished && !ws.isActive)
                {
                    Debug("world loading activating new world");
                    ws.Activate();
                }

                // if there is a gate, the gate's room will be reused, it needs to be made available
                if (self.Finished && self.game.overWorld?.reportBackToGate is RegionGate gate)
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
        private void WorldLoader_ctor(On.WorldLoader.orig_ctor_RainWorldGame_Name_bool_string_Region_SetupValues orig, WorldLoader self, RainWorldGame game, SlugcatStats.Name playerCharacter, bool singleRoomWorld, string worldName, Region region, RainWorldGame.SetupValues setupValues)
        {
            if (OnlineManager.lobby != null)
            {
                playerCharacter = OnlineManager.lobby.gameMode.LoadWorldAs(game);
                if (isArenaMode(out var arena))
                {

                    if (!OnlineManager.lobby.isOwner)
                    {
                        while (OnlineManager.lobby.worldSessions["arena"].isWaitingForState)
                        {
                            RainMeadow.Debug("Waiting for next level to be ready...");
                            Thread.Sleep(200);

                        }

                    }
                }


            }
            orig(self, game, playerCharacter, singleRoomWorld, worldName, region, setupValues);
            if (OnlineManager.lobby != null && self.game != null)
            {
                WorldSession ws = null;
                if (isArenaMode(out var _))
                {
                    RainMeadow.Debug("Arena: Setting up world session");

                    ws = OnlineManager.lobby.worldSessions["arena"];
                }
                else
                {
                    ws = OnlineManager.lobby.worldSessions[region.name];
                }
                ws.BindWorld(self.world);
                self.setupValues.worldCreaturesSpawn = OnlineManager.lobby.gameMode.ShouldLoadCreatures(self.game, ws);
            }
        }
    }
}
