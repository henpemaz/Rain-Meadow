using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Linq;

namespace RainMeadow
{
    partial class RainMeadow
    {
        
        private void EntityHooks()
        {
            On.OverWorld.WorldLoaded += OverWorld_WorldLoaded; // creature moving between WORLDS

            On.AbstractRoom.MoveEntityToDen += AbstractRoom_MoveEntityToDen; // maybe leaving room, maybe entering world
            On.AbstractWorldEntity.Destroy += AbstractWorldEntity_Destroy; // creature moving between rooms
            On.AbstractRoom.RemoveEntity_AbstractWorldEntity += AbstractRoom_RemoveEntity; // creature moving between rooms
            On.AbstractRoom.AddEntity += AbstractRoom_AddEntity; // creature moving between rooms
            
            On.AbstractCreature.Abstractize += AbstractCreature_Abstractize; // get real
            On.AbstractPhysicalObject.Abstractize += AbstractPhysicalObject_Abstractize; // get real
            On.AbstractCreature.Realize += AbstractCreature_Realize; // get real
            On.AbstractPhysicalObject.Realize += AbstractPhysicalObject_Realize; // get real
            
            On.AbstractPhysicalObject.Update += AbstractPhysicalObject_Update; // Don't think
            On.AbstractCreature.Update += AbstractCreature_Update; // Don't think
            On.AbstractCreature.OpportunityToEnterDen += AbstractCreature_OpportunityToEnterDen; // Don't think

            On.AbstractCreature.ChangeRooms += AbstractCreature_ChangeRooms; // Don't move
            On.AbstractPhysicalObject.ChangeRooms += AbstractPhysicalObject_ChangeRooms; // Don't move
            On.AbstractCreature.Move += AbstractCreature_Move; // Don't move
            On.AbstractPhysicalObject.Move += AbstractPhysicalObject_Move; // Don't move
        }

        // Don't move
        // remotes that aren't being moved can only move if going into the right roomSession
        private void AbstractPhysicalObject_Move(On.AbstractPhysicalObject.orig_Move orig, AbstractPhysicalObject self, WorldCoordinate newCoord)
        {
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.owner.isMe && !oe.beingMoved && !(oe.roomSession != null && oe.roomSession.absroom.index == newCoord.room))
                {
                    RainMeadow.Error($"Remote entity trying to move: {oe} at {oe.roomSession} {System.Environment.StackTrace}");
                    return;
                }
            }
            orig(self, newCoord);
        }

        // Don't move
        private void AbstractCreature_Move(On.AbstractCreature.orig_Move orig, AbstractCreature self, WorldCoordinate newCoord)
        {
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.owner.isMe && !oe.beingMoved && !(oe.roomSession != null && oe.roomSession.absroom.index == newCoord.room))
                {
                    RainMeadow.Error($"Remote entity trying to move: {oe} at {oe.roomSession} {System.Environment.StackTrace}");
                    return;
                }
            }
            orig(self, newCoord);
        }


        // creature moving between rooms
        // vanilla calls removeentity + addentity but entity.pos is only updated LATER so we need this instead of addentity
        private void AbstractPhysicalObject_ChangeRooms(On.AbstractPhysicalObject.orig_ChangeRooms orig, AbstractPhysicalObject self, WorldCoordinate newCoord)
        {
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.owner.isMe && !oe.beingMoved && !(oe.roomSession != null && oe.roomSession.absroom.index == newCoord.room))
                {
                    RainMeadow.Error($"Remote entity trying to move: {oe} at {oe.roomSession} {System.Environment.StackTrace}");
                    return;
                }
            }
            orig(self, newCoord);
            if (OnlineManager.lobby != null && RoomSession.map.TryGetValue(self.world.GetAbstractRoom(newCoord.room), out var rs) && OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(rs, self))
            {
                rs.ApoEnteringRoom(self, newCoord);
            }
        }

        // Don't move
        private void AbstractCreature_ChangeRooms(On.AbstractCreature.orig_ChangeRooms orig, AbstractCreature self, WorldCoordinate newCoord)
        {
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.owner.isMe && !oe.beingMoved && !(oe.roomSession != null && oe.roomSession.absroom.index == newCoord.room))
                {
                    RainMeadow.Error($"Remote entity trying to move: {oe} at {oe.roomSession} {System.Environment.StackTrace}");
                    return;
                }
            }
            orig(self, newCoord);
        }

        // Don't think
        private void AbstractCreature_OpportunityToEnterDen(On.AbstractCreature.orig_OpportunityToEnterDen orig, AbstractCreature self, WorldCoordinate den)
        {
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.owner.isMe && !oe.beingMoved)
                {
                    RainMeadow.Error($"Remote entity trying to move: {oe} at {oe.roomSession} {System.Environment.StackTrace}");
                    return;
                }
            }
            orig(self, den);
        }

        // Don't think
        private void AbstractCreature_Update(On.AbstractCreature.orig_Update orig, AbstractCreature self, int time)
        {
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.owner.isMe && !oe.beingMoved)
                {
                    return;
                }
            }
            orig(self, time);
        }

        // Don't think
        private void AbstractPhysicalObject_Update(On.AbstractPhysicalObject.orig_Update orig, AbstractPhysicalObject self, int time)
        {
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.owner.isMe && !oe.beingMoved)
                {
                    return;
                }
            }
            orig(self, time);
        }

        
        private void AbstractPhysicalObject_Realize(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
        {
            orig(self);
            if(OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if(!oe.owner.isMe && !oe.realized && oe.isTransferable)
                {
                    if (oe.roomSession == null || !oe.roomSession.participants.ContainsKey(oe.owner)) //if owner of oe is subscribed (is participant) do not request
                    {
                        oe.Request();
                    }
                }
                if (oe.owner.isMe)
                {
                    oe.realized = true;
                }
            }
        }

        // get real
        private void AbstractCreature_Realize(On.AbstractCreature.orig_Realize orig, AbstractCreature self)
        {
            orig(self);
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.owner.isMe && !oe.realized && oe.isTransferable)
                {
                    if (oe.roomSession == null || !oe.roomSession.participants.ContainsKey(oe.owner)) //if owner of oe is subscribed (is participant) do not request
                    {
                        oe.Request();
                    }
                }
                if (oe.owner.isMe)
                {
                    oe.realized = true;
                }
            }
        }

        // get real
        private void AbstractPhysicalObject_Abstractize(On.AbstractPhysicalObject.orig_Abstractize orig, AbstractPhysicalObject self, WorldCoordinate coord)
        {
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.owner.isMe && !oe.beingMoved)
                {
                    RainMeadow.Error($"Remote entity trying to move: {oe} at {oe.roomSession} {System.Environment.StackTrace}");
                    return;
                }
            }
            orig(self, coord);
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out oe))
            {
                if (oe.realized && oe.isTransferable && oe.owner.isMe)
                {
                    oe.Release();
                }
                if (oe.owner.isMe)
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
                if (!oe.owner.isMe && !oe.beingMoved)
                {
                    RainMeadow.Error($"Remote entity trying to move: {oe} at {oe.roomSession} {System.Environment.StackTrace}");
                    return;
                }
            }
            orig(self, coord);
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out oe))
            {
                if (oe.realized && oe.isTransferable && oe.owner.isMe)
                {
                    oe.Release();
                }
                if (oe.owner.isMe)
                {
                    oe.realized = false;
                }
            }
        }
        

        // not the main entry-point for room entities moving around
        // creature.move doesn't set the new pos until after it has moved, that's the issue
        // this is only for things that are ADDED directly to the room
        private void AbstractRoom_AddEntity(On.AbstractRoom.orig_AddEntity orig, AbstractRoom self, AbstractWorldEntity ent)
        {
            if (OnlineManager.lobby != null && ent is AbstractPhysicalObject apo0 && OnlinePhysicalObject.map.TryGetValue(apo0, out var oe))
            {
                if (!oe.owner.isMe && !oe.beingMoved)
                {
                    RainMeadow.Error($"Remote entity trying to move: {oe} at {oe.roomSession} {System.Environment.StackTrace}");
                    return;
                }
            }
            orig(self, ent);
            if (OnlineManager.lobby != null && ent is AbstractPhysicalObject apo && apo.pos.room == self.index)
            {
                if (WorldSession.map.TryGetValue(self.world, out var ws) && OnlineManager.lobby.gameMode.ShouldSyncObjectInWorld(ws, apo)) ws.ApoEnteringWorld(apo);
                if (RoomSession.map.TryGetValue(self, out var rs) && OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(rs, apo)) rs.ApoEnteringRoom(apo, apo.pos);
            }
        }

        private void AbstractRoom_RemoveEntity(On.AbstractRoom.orig_RemoveEntity_AbstractWorldEntity orig, AbstractRoom self, AbstractWorldEntity entity)
        {
            if (OnlineManager.lobby != null && entity is AbstractPhysicalObject apo0 && OnlinePhysicalObject.map.TryGetValue(apo0, out var oe))
            {
                if (!oe.owner.isMe && !oe.beingMoved)
                {
                    RainMeadow.Error($"Remote entity trying to move: {oe} at {oe.roomSession} {System.Environment.StackTrace}");
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
                if (!oe.owner.isMe && !oe.beingMoved)
                {
                    RainMeadow.Error($"Remote entity trying to move: {oe} at {oe.roomSession} {System.Environment.StackTrace}");
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
                if (!oe.owner.isMe && !oe.beingMoved)
                {
                    RainMeadow.Error($"Remote entity trying to move: {oe} at {oe.roomSession} {System.Environment.StackTrace}");
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
            if(OnlineManager.lobby != null)
            {
                var oldWorld = self.activeWorld;
                var newWorld = self.worldLoader.world;
                Room room = null;

                // Regular gate switch
                // pre: remove remote entities
                if (self.reportBackToGate != null && RoomSession.map.TryGetValue(self.reportBackToGate.room.abstractRoom, out var roomSession))
                {
                    // we go over all APOs in the room
                    RainMeadow.Debug("Gate switchery 1");
                    room = self.reportBackToGate.room;
                    var entities = room.abstractRoom.entities;
                    for (int i = entities.Count - 1; i >= 0; i--)
                    {
                        if (entities[i] is AbstractPhysicalObject apo && OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
                        {
                            // if they're not ours, they need to be removed from the room SO THE GAME DOESN'T MOVE THEM
                            if (!oe.owner.isMe)
                            {
                                RainMeadow.Debug("removing remote entity " + oe);
                                roomSession.entities.Remove(oe);
                                room.abstractRoom.RemoveEntity(apo);
                                if(apo.realizedObject != null)
                                {
                                    room.RemoveObject(apo.realizedObject);
                                    room.CleanOutObjectNotInThisRoom(apo.realizedObject);
                                }
                            }
                            else // mine leave the old online world
                            {
                                RainMeadow.Debug("removing my entity " + oe);
                                roomSession.LocalEntityLeft(oe);
                                roomSession.worldSession.LocalEntityLeft(oe);
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
                    RainMeadow.Debug("Gate switchery 2");
                    var entities = room.abstractRoom.entities;
                    for (int i = entities.Count - 1; i >= 0; i--)
                    {
                        if (entities[i] is AbstractPhysicalObject apo && OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
                        {
                            if (oe.owner.isMe)
                            {
                                RainMeadow.Debug("readding entity to world" + oe);
                                oe.enterPos = apo.pos;
                                roomSession2.worldSession.LocalEntityEntered(oe);
                            }
                            else // what happened here
                            {
                                RainMeadow.Error("an entity that came through the gate wasnt mine: " + oe);
                            }
                        }
                    }
                    roomSession2.Activate(); // adds entities that are already in the room
                }
            }
            else
            {
                orig(self);
            }
        }
    }
}
