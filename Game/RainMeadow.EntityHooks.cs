using System;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        // Track entities joining/leaving resources
        // customization stuff reused some hooks
        private void EntityHooks()
        {
            On.OverWorld.WorldLoaded += OverWorld_WorldLoaded; // creature moving between WORLDS

            On.AbstractRoom.MoveEntityToDen += AbstractRoom_MoveEntityToDen; // maybe leaving room, maybe entering world
            On.AbstractWorldEntity.Destroy += AbstractWorldEntity_Destroy; // creature moving between rooms
            On.AbstractRoom.RemoveEntity_AbstractWorldEntity += AbstractRoom_RemoveEntity; // creature moving between rooms
            On.AbstractRoom.AddEntity += AbstractRoom_AddEntity; // creature moving between rooms

            On.AbstractPhysicalObject.ChangeRooms += AbstractPhysicalObject_ChangeRooms;
            On.AbstractCreature.ChangeRooms += AbstractCreature_ChangeRooms1;

            On.AbstractCreature.Abstractize += AbstractCreature_Abstractize; // get real
            On.AbstractPhysicalObject.Abstractize += AbstractPhysicalObject_Abstractize; // get real
            On.AbstractCreature.Realize += AbstractCreature_Realize; // get real, also customization happens here
            On.AbstractPhysicalObject.Realize += AbstractPhysicalObject_Realize; // get real

            On.AbstractCreature.Move += AbstractCreature_Move; // I'm watching your every step
            On.AbstractPhysicalObject.Move += AbstractPhysicalObject_Move; // I'm watching your every step
        }

        private void AbstractCreature_ChangeRooms1(On.AbstractCreature.orig_ChangeRooms orig, AbstractCreature self, WorldCoordinate newCoord)
        {
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.isMine && !oe.beingMoved && !(oe.roomSession != null && oe.roomSession.absroom.index == newCoord.room))
                {
                    Error($"Remote entity trying to move: {oe} at {oe.roomSession} {Environment.StackTrace}");
                    return;
                }
            }
            orig(self, newCoord);
        }

        private void AbstractPhysicalObject_ChangeRooms(On.AbstractPhysicalObject.orig_ChangeRooms orig, AbstractPhysicalObject self, WorldCoordinate newCoord)
        {
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.isMine && !oe.beingMoved && !(oe.roomSession != null && oe.roomSession.absroom.index == newCoord.room))
                {
                    Error($"Remote entity trying to move: {oe} at {oe.roomSession} {Environment.StackTrace}");
                    return;
                }
            }
            orig(self, newCoord);
        }

        // I'm watching your every step
        // remotes that aren't being moved can only move if going into the right roomSession
        private void AbstractPhysicalObject_Move(On.AbstractPhysicalObject.orig_Move orig, AbstractPhysicalObject self, WorldCoordinate newCoord)
        {
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.isMine && !oe.beingMoved && !(oe.roomSession != null && oe.roomSession.absroom.index == newCoord.room))
                {
                    Error($"Remote entity trying to move: {oe} at {oe.roomSession} {Environment.StackTrace}");
                    return;
                }
            }
            var oldCoord = self.pos;
            orig(self, newCoord);
            if (OnlineManager.lobby != null && oldCoord.room != newCoord.room)
            {
                // leaving room is handled in absroom.removeentity
                // adding to room is handled here so the position is updated properly
                if (WorldSession.map.TryGetValue(self.world, out var ws) && OnlineManager.lobby.gameMode.ShouldSyncObjectInWorld(ws, self)) ws.ApoEnteringWorld(self);
                if (RoomSession.map.TryGetValue(self.world.GetAbstractRoom(newCoord.room), out var rs) && OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(rs, self))
                {
                    rs.ApoEnteringRoom(self, newCoord);
                }
            }
        }

        // I'm watching your every step
        private void AbstractCreature_Move(On.AbstractCreature.orig_Move orig, AbstractCreature self, WorldCoordinate newCoord)
        {
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.isMine && !oe.beingMoved && !(oe.roomSession != null && oe.roomSession.absroom.index == newCoord.room))
                {
                    Error($"Remote entity trying to move: {oe} at {oe.roomSession} {Environment.StackTrace}");
                    return;
                }
            }
            orig(self, newCoord);
        }

        private void AbstractPhysicalObject_Realize(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
        {
            if(OnlineManager.lobby != null)
            {
                UnityEngine.Random.seed = self.ID.RandomSeed;
            }
            orig(self);
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.isMine && !oe.realized && oe.isTransferable && !oe.isPending)
                {
                    if (oe.roomSession == null || !oe.roomSession.participants.ContainsKey(oe.owner)) //if owner of oe is subscribed (is participant) do not request
                    {
                        oe.Request();
                    }
                }
                if (oe.isMine)
                {
                    oe.realized = true;
                }
            }
        }

        // get real, and customize
        private void AbstractCreature_Realize(On.AbstractCreature.orig_Realize orig, AbstractCreature self)
        {
            var wasCreature = self.realizedCreature;
            orig(self);
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.isMine && !oe.realized && oe.isTransferable && !oe.isPending)
                {
                    if (oe.roomSession == null || !oe.roomSession.participants.ContainsKey(oe.owner)) //if owner of oe is subscribed (is participant) do not request
                    {
                        oe.Request();
                    }
                }
                if (oe.isMine)
                {
                    oe.realized = self.realizedObject != null;
                }
                if (self.realizedCreature != null && self.realizedCreature != wasCreature && oe is OnlineCreature oc)
                {
                    OnlineManager.lobby.gameMode.Customize(self.realizedCreature, oc);
                }
            }
        }

        // get real
        private void AbstractPhysicalObject_Abstractize(On.AbstractPhysicalObject.orig_Abstractize orig, AbstractPhysicalObject self, WorldCoordinate coord)
        {
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.isMine && !oe.beingMoved)
                {
                    Error($"Remote entity trying to move: {oe} at {oe.roomSession} {Environment.StackTrace}");
                    return;
                }
            }
            orig(self, coord);
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out oe))
            {
                if (oe.realized && oe.isTransferable && oe.isMine && !oe.isPending)
                {
                    oe.Release();
                }
                if (oe.isMine)
                {
                    oe.realized = false;
                }
            }
        }

        // get real
        private void AbstractCreature_Abstractize(On.AbstractCreature.orig_Abstractize orig, AbstractCreature self, WorldCoordinate coord)
        {
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.isMine && !oe.beingMoved && RoomSession.map.TryGetValue(self.Room, out var room) && oe.joinedResources.Contains(room))
                {
                    Error($"Remote entity trying to move: {oe} at {oe.roomSession} {Environment.StackTrace}");
                    return;
                }
            }
            orig(self, coord);
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out oe))
            {
                if (oe.realized && oe.isTransferable && oe.isMine && !oe.isPending)
                {
                    oe.Release();
                }
                if (oe.isMine)
                {
                    oe.realized = false;
                }
            }
        }


        // not the main entry-point for room entities moving around
        // apo.move doesn't set the new pos until after it has moved, that's the issue
        // this is only for things that are ADDED directly to the room
        private void AbstractRoom_AddEntity(On.AbstractRoom.orig_AddEntity orig, AbstractRoom self, AbstractWorldEntity ent)
        {
            if (OnlineManager.lobby != null && ent is AbstractPhysicalObject apo0 && OnlinePhysicalObject.map.TryGetValue(apo0, out var oe))
            {
                if (!oe.isMine && !oe.beingMoved)
                {
                    Error($"Remote entity trying to move: {oe} at {oe.roomSession} {Environment.StackTrace}");
                    return;
                }
            }
            orig(self, ent);
            if (OnlineManager.lobby != null && ent is AbstractPhysicalObject apo && apo.pos.room == self.index) // skips apos being apo.Move'd
            {
                if (WorldSession.map.TryGetValue(self.world, out var ws) && OnlineManager.lobby.gameMode.ShouldSyncObjectInWorld(ws, apo)) ws.ApoEnteringWorld(apo);
                if (RoomSession.map.TryGetValue(self, out var rs) && OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(rs, apo)) rs.ApoEnteringRoom(apo, apo.pos);
            }
        }

        // called from several places, thus handled here rather than in apo.move
        private void AbstractRoom_RemoveEntity(On.AbstractRoom.orig_RemoveEntity_AbstractWorldEntity orig, AbstractRoom self, AbstractWorldEntity entity)
        {
            if (OnlineManager.lobby != null && entity is AbstractPhysicalObject apo0 && OnlinePhysicalObject.map.TryGetValue(apo0, out var oe))
            {
                if (!oe.isMine && !oe.beingMoved)
                {
                    Error($"Remote entity trying to move: {oe} at {oe.roomSession} {Environment.StackTrace}");
                    return;
                }
            }
            orig(self, entity);
            if (OnlineManager.lobby != null && entity is AbstractPhysicalObject apo && RoomSession.map.TryGetValue(self, out var rs) && OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(rs, apo))
            {
                rs.ApoLeavingRoom(apo);
            }
        }

        private void AbstractWorldEntity_Destroy(On.AbstractWorldEntity.orig_Destroy orig, AbstractWorldEntity self)
        {
            if (OnlineManager.lobby != null && self is AbstractPhysicalObject apo0 && OnlinePhysicalObject.map.TryGetValue(apo0, out var oe))
            {
                if (!oe.isMine && !oe.beingMoved)
                {
                    Error($"Remote entity trying to move: {oe} at {oe.roomSession} {Environment.StackTrace}");
                    return;
                }
            }
            orig(self);
            if (OnlineManager.lobby != null && self is AbstractPhysicalObject apo)
            {
                if (RoomSession.map.TryGetValue(self.Room, out var rs) && OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(rs, apo)) rs.ApoLeavingRoom(apo);
                if (WorldSession.map.TryGetValue(self.world, out var ws) && OnlineManager.lobby.gameMode.ShouldSyncObjectInWorld(ws, apo)) ws.ApoLeavingWorld(apo);
            }
        }

        // maybe leaving room, maybe entering world
        private void AbstractRoom_MoveEntityToDen(On.AbstractRoom.orig_MoveEntityToDen orig, AbstractRoom self, AbstractWorldEntity entity)
        {
            if (OnlineManager.lobby != null && entity is AbstractPhysicalObject apo0 && OnlinePhysicalObject.map.TryGetValue(apo0, out var oe))
            {
                if (!oe.isMine && !oe.beingMoved)
                {
                    Error($"Remote entity trying to move: {oe} at {oe.roomSession} {Environment.StackTrace}");
                    return;
                }
            }
            orig(self, entity);
            if (OnlineManager.lobby != null && entity is AbstractPhysicalObject apo)
            {
                if (WorldSession.map.TryGetValue(self.world, out var ws) && OnlineManager.lobby.gameMode.ShouldSyncObjectInWorld(ws, apo)) ws.ApoEnteringWorld(apo);
                if (RoomSession.map.TryGetValue(self, out var rs) && OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(rs, apo)) rs.ApoLeavingRoom(apo);
            }
        }

        // world transition at gates
        private void OverWorld_WorldLoaded(On.OverWorld.orig_WorldLoaded orig, OverWorld self)
        {
            if (OnlineManager.lobby != null)
            {
                AbstractRoom oldAbsroom = self.reportBackToGate.room.abstractRoom;
                AbstractRoom newAbsroom = self.worldLoader.world.GetAbstractRoom(oldAbsroom.name);
                WorldSession oldWorldSession = WorldSession.map.GetValue(oldAbsroom.world, (w) => throw new KeyNotFoundException());
                WorldSession newWorldSession = WorldSession.map.GetValue(newAbsroom.world, (w) => throw new KeyNotFoundException());
                List<AbstractWorldEntity> entitiesFromNewRoom = newAbsroom.entities; // these get ovewritten and need handling
                List<AbstractCreature> creaturesFromNewRoom = newAbsroom.creatures;

                Room room = null;

                // Regular gate switch
                // pre: remove remote entities
                if (self.reportBackToGate != null && RoomSession.map.TryGetValue(self.reportBackToGate.room.abstractRoom, out var roomSession))
                {
                    // we go over all APOs in the room
                    Debug("Gate switchery 1");
                    room = self.reportBackToGate.room;
                    var entities = room.abstractRoom.entities;
                    for (int i = entities.Count - 1; i >= 0; i--)
                    {
                        if (entities[i] is AbstractPhysicalObject apo && OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
                        {
                            // if they're not ours, they need to be removed from the room SO THE GAME DOESN'T MOVE THEM
                            // if they're the overseer and it isn't the host moving it, that's bad as well
                            if (!oe.isMine || (apo is AbstractCreature ac && ac.creatureTemplate.type == CreatureTemplate.Type.Overseer && !newWorldSession.isOwner))
                            {
                                // not-online-aware removal
                                Debug("removing remote entity from game " + oe);
                                oe.beingMoved = true;
                                if (oe.apo.realizedObject is Creature c && c.inShortcut)
                                {
                                    if (c.RemoveFromShortcuts()) c.inShortcut = false;
                                }
                                entities.Remove(oe.apo);
                                room.abstractRoom.creatures.Remove(oe.apo as AbstractCreature);
                                room.RemoveObject(oe.apo.realizedObject);
                                room.CleanOutObjectNotInThisRoom(oe.apo.realizedObject);
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
                    roomSession.worldSession.FullyReleaseResource();
                }

                orig(self); // this replace the list of entities in new world with that from old world

                // post: we add our entities to the new world
                if (room != null && RoomSession.map.TryGetValue(room.abstractRoom, out var roomSession2))
                {
                    // update definitions
                    foreach (var ent in room.abstractRoom.entities)
                    {
                        if (ent is AbstractPhysicalObject apo && OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
                        {
                            // should these be some sort of OnlinePhisicalObject api?
                            if (apo is AbstractCreature ac)
                            {
                                if (isArenaMode(out var _))
                                {
                                    (oe.definition as OnlineCreatureDefinition).serializedObject = SaveState.AbstractCreatureToStringSingleRoomWorld(ac);

                                } else
                                {
                                    (oe.definition as OnlineCreatureDefinition).serializedObject = SaveState.AbstractCreatureToStringStoryWorld(ac);

                                }
                            }
                            else
                            {
                                (oe.definition as OnlinePhysicalObjectDefinition).serializedObject = apo.ToString();
                            }
                        }
                        else
                        {
                            RainMeadow.Error("unhandled entity in gate post switch: " + ent);
                        }
                    }

                    roomSession2.Activate(); // adds entities that are already in the room as mine
                    room.abstractRoom.entities.AddRange(entitiesFromNewRoom); // re-add overwritten entities
                    room.abstractRoom.creatures.AddRange(creaturesFromNewRoom);
                    // room never "loaded" so we handle as entities joining a loaded room
                    for (int i = 0; i < entitiesFromNewRoom.Count; i++)
                    {
                        if (entitiesFromNewRoom[i] is AbstractPhysicalObject apo && OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
                        {
                            oe.OnJoinedResource(roomSession2);
                        }
                    }
                }
                if (OnlineManager.lobby.gameMode is StoryGameMode storyGameMode) 
                {
                    storyGameMode.changedRegions = true;
                }
            }
            else
            {
                orig(self);
            }
        }
    }
}
