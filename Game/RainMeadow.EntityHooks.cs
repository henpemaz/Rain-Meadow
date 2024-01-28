using System;

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

            On.AbstractCreature.Abstractize += AbstractCreature_Abstractize; // get real
            On.AbstractPhysicalObject.Abstractize += AbstractPhysicalObject_Abstractize; // get real
            On.AbstractCreature.Realize += AbstractCreature_Realize; // get real, also customization happens here
            On.AbstractPhysicalObject.Realize += AbstractPhysicalObject_Realize; // get real

            On.AbstractCreature.Move += AbstractCreature_Move; // I'm watching your every step
            On.AbstractPhysicalObject.Move += AbstractPhysicalObject_Move; // I'm watching your every step
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
                if (OnlineManager.lobby.gameModeType == OnlineGameMode.OnlineGameModeType.Meadow && self.realizedCreature != null && self.realizedCreature != wasCreature && oe is OnlineCreature oc)
                {
                    MeadowCustomization.Customize(self.realizedCreature, oc);
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
                if (oe.realized && oe.isTransferable && oe.isMine)
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
                if (oe.realized && oe.isTransferable && oe.isMine)
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
                var oldWorld = self.activeWorld;
                var newWorld = self.worldLoader.world;
                Room room = null;

                if (true)
                {
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
                                if (!oe.isMine)
                                {
                                    if (oe.isTransferable && roomSession.worldSession.isOwner) {
                                        roomSession.worldSession.EntityTransfered(oe, OnlineManager.mePlayer);
                                    }
                                    else {
                                        Debug("removing remote entity " + oe);
                                        roomSession.entities.Remove(oe.id);
                                        oe.OnLeftResource(roomSession);
                                    }
                                }
                                else // mine leave the old online world
                                {
                                    Debug("removing my entity " + oe);
                                    oe.LeaveResource(roomSession);
                                    oe.LeaveResource(roomSession.worldSession);
                                }
                            }
                        }
                        roomSession.worldSession.FullyReleaseResource();
                    }

                    orig(self);

                    // post: we add our entities to the new world
                    if (room != null && RoomSession.map.TryGetValue(room.abstractRoom, out var roomSession2))
                    {
                        // Don't reuse entities left from previous region
                        OnlineManager.recentEntities.Clear();

                        // we go over all APOs in the room
                        Debug("Gate switchery 2");
                        var entities = room.abstractRoom.entities;
                        for (int i = entities.Count - 1; i >= 0; i--)
                        {
                            if (entities[i] is AbstractPhysicalObject apo && OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
                            {
                                if (oe.isMine)
                                {
                                    Debug("readding entity to world" + oe);
                                    roomSession2.worldSession.LocalEntityEntered(oe);
                                }
                                else // what happened here
                                {
                                    Error("an entity that came through the gate wasnt mine: " + oe);
                                }
                            }
                        }
                        roomSession2.Activate(); // adds entities that are already in the room
                    }
                }
                else if (false) 
                {
                    RoomSession? oldRoom = null;

                    if (self.reportBackToGate != null && RoomSession.map.TryGetValue(self.reportBackToGate.room.abstractRoom, out var roomSession))
                    {
                        oldRoom = roomSession;
                        room = self.reportBackToGate.room;

                        if (oldRoom.worldSession.isOwner)
                        {
                            // Grab Ownership of everything. We'll sort things out after the room merge
                        }
                        else 
                        {
                            /* wait for OK from roomSession owner
                             * Release Ownership of everything 
                             * Empty out room
                             */
                            var entities = room.abstractRoom.entities;
                            for (int i = entities.Count - 1; i >= 0; i--)
                            {
                                if (entities[i] is AbstractPhysicalObject apo && OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
                                {
                                    // if they're not ours, they need to be removed from the room SO THE GAME DOESN'T MOVE THEM
                                    if (!oe.isMine)
                                    {
                                        Debug("removing remote entity " + oe);
                                        roomSession.entities.Remove(oe.id);
                                        oe.OnLeftResource(roomSession);
                                    }
                                    else // mine leave the old online world
                                    {
                                        Debug("removing my entity " + oe);
                                        oe.LeaveResource(roomSession);
                                        oe.LeaveResource(roomSession.worldSession);
                                    }
                                }
                            }
                            oldRoom.worldSession.FullyReleaseResource();
                        }
                    }
                    Debug("before region merge");
                    orig(self);
                    Debug("after region merge");
                    if (room != null && RoomSession.map.TryGetValue(room.abstractRoom, out var newRoom))
                    { 
                        if (newRoom.isOwner)
                        {
                            /* Release everything in the previous region
                             * Repopulate the current region
                             * Send OK to other players
                             */
                            var entities = room.abstractRoom.entities;
                            for (int i = entities.Count - 1; i >= 0; i--)
                            {
                                if (entities[i] is AbstractPhysicalObject apo && OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
                                {
                                    // if they're not ours, they need to be removed from the room SO THE GAME DOESN'T MOVE THEM
                                    Debug("removing my entity " + oe);
                                    oe.LeaveResource(oldRoom);
                                    oe.LeaveResource(oldRoom.worldSession);
                                }
                            }
                            //oldRoom.worldSession.FullyReleaseResource();
                            newRoom.Activate();
                        }
                        else 
                        { 
                            /* Repopulate
                             *
                             */
                        }
                    }
                }
            }
            else
            {
                orig(self);
            }
        }
    }
}
