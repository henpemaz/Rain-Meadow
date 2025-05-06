using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
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
        }

        private void ArenaSitting_NextLevel(On.ArenaSitting.orig_NextLevel orig, ArenaSitting self, ProcessManager manager)
        {
            if (isArenaMode(out var arena))
            {
                arena.onlineArenaGameMode.ArenaSessionNextLevel(arena, orig, self, manager);

                for (int q = 0; q < arena.arenaSittingOnlineOrder.Count; q++)
                {
                    OnlinePlayer? onlinePlayer = ArenaHelpers.FindOnlinePlayerByLobbyId(arena.arenaSittingOnlineOrder[q]);
                    if (onlinePlayer != null)
                    {
                        onlinePlayer.InvokeOnceRPC(ArenaRPCs.Arena_NextLevelCall);
                    }
                }

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
                                if (oe.apo.realizedObject != null)
                                {
                                    room.RemoveObject(oe.apo.realizedObject);
                                    room.CleanOutObjectNotInThisRoom(oe.apo.realizedObject);
                                }
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
                    if (OnlineManager.lobby.isOwner)
                    {
                        arena.currentLevel = self.currentLevel;
                    }

                    if (self.currentLevel >= arena.playList.Count && !self.gameTypeSetup.repeatSingleLevelForever)
                    {
                        manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MultiplayerResults);

                        if (!OnlineManager.lobby.isOwner)
                        {
                            OnlineManager.lobby.owner.InvokeRPC(ArenaRPCs.Arena_ResetPlayersLeft);
                        }

                        return;
                    }



                    // Remove gone players

                    for (int i = self.players.Count - 1; i >= 0; i--)
                    {
                        OnlinePlayer? onlineArenaSittingPlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, self.players[i].playerNumber);
                        if (onlineArenaSittingPlayer == null)
                        {
                            if (OnlineManager.lobby.isOwner)
                            {
                                // Find the index of the missing player's inLobbyId in arenaSittingOnlineOrder
                                // Safely check if the index 'i' is within the bounds of arena.arenaSittingOnlineOrder
                                if (i >= 0 && i < arena.arenaSittingOnlineOrder.Count)
                                {
                                    // Now it's safe to access arena.arenaSittingOnlineOrder[i]
                                    if (arena.arenaSittingOnlineOrder[i] == (onlineArenaSittingPlayer?.inLobbyId ?? null))
                                    {
                                        RainMeadow.Debug("Arena: Removing missing player from sitting");
                                        arena.arenaSittingOnlineOrder.RemoveAt(i);
                                        RainMeadow.Debug("Arena: Removed missing player from sitting");
                                    }
                                }
                                else
                                {
                                    RainMeadow.Debug($"Warning: Index {i} is out of bounds for arena.arenaSittingOnlineOrder.");
                                }
                            }
                            RainMeadow.Debug("Arena: Removing missing player from local sitting");
                            self.players.RemoveAt(i);
                            RainMeadow.Debug("Arena: Removed missing player from local sitting");
                        }
                        else
                        {
                            if (arena.playersLateWaitingInLobbyForNextRound.Count > 0)
                            {
                                RainMeadow.Debug("Found a late client");

                                // Iterate backwards to safely remove from self.players
                                for (int p = arena.playersLateWaitingInLobbyForNextRound.Count - 1; p >= 0; p--)
                                {
                                    OnlinePlayer? lateClient = ArenaHelpers.FindOnlinePlayerByLobbyId(arena.playersLateWaitingInLobbyForNextRound[p]);
                                    RainMeadow.Debug($"Original client: {onlineArenaSittingPlayer}");
                                    RainMeadow.Debug($"Late client: {lateClient}");

                                    if (lateClient != null && lateClient == onlineArenaSittingPlayer)
                                    {
                                        RainMeadow.Debug("Found a late client who matches a sitting player in-game");

                                        if (OnlineManager.lobby.isOwner)
                                        {
                                            if (i >= 0 && i < arena.arenaSittingOnlineOrder.Count)
                                            {
                                                RainMeadow.Debug("Arena: Removing late player from sitting");
                                                arena.arenaSittingOnlineOrder.RemoveAt(i);
                                                RainMeadow.Debug("Arena: Removed late player from sitting");
                                            }
                                        }
                                        RainMeadow.Debug($"Arena: Removing pending player's old sitting entry: {lateClient}");
                                        self.players.RemoveAt(i); // Remove from the outer loop's current index
                                    }
                                }
                            }
                        }
                    }

                    if (arena.playersLateWaitingInLobbyForNextRound.Count > 0)
                    {
                        // Add waiting players
                        if (OnlineManager.lobby.isOwner)
                        {
                            foreach (var player in arena.playersLateWaitingInLobbyForNextRound)
                            {
                                if (!arena.arenaSittingOnlineOrder.Contains(player))
                                {
                                    RainMeadow.Debug($"Arena: Adding pending player inLobbyId: {player}");
                                    arena.arenaSittingOnlineOrder.Add(player);
                                }
                            }
                        }
                        for (int y = 0; y < arena.playersLateWaitingInLobbyForNextRound.Count; y++)
                        {

                            OnlinePlayer? onlineP = ArenaHelpers.FindOnlinePlayerByLobbyId(arena.playersLateWaitingInLobbyForNextRound[y]);
                            if (onlineP == null)
                            {
                                continue;
                            }

                            ArenaSitting.ArenaPlayer newArenaPlayer = new ArenaSitting.ArenaPlayer(arena.arenaSittingOnlineOrder.Count - 1)
                            {
                                playerNumber = arena.arenaSittingOnlineOrder.Count - 1,
                                playerClass = ((OnlineManager.lobby.clientSettings[onlineP].GetData<ArenaClientSettings>()).playingAs),
                                hasEnteredGameArea = true
                            };
                            self.players.Add(newArenaPlayer);
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
        private void WorldLoader_ctor(On.WorldLoader.orig_ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues orig, WorldLoader self, RainWorldGame game, SlugcatStats.Name playerCharacter, SlugcatStats.Timeline timeline, bool singleRoomWorld, string worldName, Region region, RainWorldGame.SetupValues setupValues)
        {
            if (OnlineManager.lobby != null)
            {
                playerCharacter = OnlineManager.lobby.gameMode.LoadWorldAs(game);


            }
            orig(self, game, playerCharacter, timeline, singleRoomWorld, worldName, region, setupValues);
            if (OnlineManager.lobby != null && self.game != null)
            {
                try
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
                catch (System.NullReferenceException e) // happens in riv ending
                {
                    RainMeadow.Debug("NOTE: rivulet hackfix null ref exception is bad!");
                }
            }
        }
    }
}
