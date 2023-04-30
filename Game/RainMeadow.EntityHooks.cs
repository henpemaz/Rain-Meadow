using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    partial class RainMeadow
    {
        public static bool sSpawningPersonas;

        private void EntityHooks()
        {
            On.OverWorld.WorldLoaded += OverWorld_WorldLoaded; // creature moving between WORLDS

            On.AbstractRoom.MoveEntityToDen += AbstractRoom_MoveEntityToDen; // creature moving between rooms
            On.AbstractWorldEntity.Destroy += AbstractWorldEntity_Destroy; // creature moving between rooms
            On.AbstractRoom.RemoveEntity_AbstractWorldEntity += AbstractRoom_RemoveEntity; // creature moving between rooms
            On.AbstractRoom.AddEntity += AbstractRoom_AddEntity; // creature moving between rooms
            On.AbstractPhysicalObject.ChangeRooms += AbstractPhysicalObject_ChangeRooms; // creature moving between rooms
            
            On.Room.AddObject += RoomOnAddObject; // Prevent adding item to update list twice

            IL.ShortcutHandler.Update += ShortcutHandler_Update; // cleanup of deleted entities in shortcut system
            On.ShortcutHandler.VesselAllowedInRoom += ShortcutHandlerOnVesselAllowedInRoom; // Prevent creatures from entering a room if their online counterpart has not yet entered!
            
            On.AbstractCreature.Abstractize += AbstractCreature_Abstractize; // get real
            On.AbstractPhysicalObject.Abstractize += AbstractPhysicalObject_Abstractize; // get real
            On.AbstractCreature.Realize += AbstractCreature_Realize; // get real
            On.AbstractPhysicalObject.Realize += AbstractPhysicalObject_Realize; // get real
            
            On.RainWorldGame.SpawnPlayers_bool_bool_bool_bool_WorldCoordinate += RainWorldGame_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate; // Personas are set as non-transferable
            
            On.AbstractPhysicalObject.Update += AbstractPhysicalObject_Update; // Don't think
            On.AbstractCreature.Update += AbstractCreature_Update; // Don't think
            On.AbstractCreature.OpportunityToEnterDen += AbstractCreature_OpportunityToEnterDen; // Don't think
        }

        // Don't think
        private void AbstractCreature_OpportunityToEnterDen(On.AbstractCreature.orig_OpportunityToEnterDen orig, AbstractCreature self, WorldCoordinate den)
        {
            if (OnlineManager.lobby != null && OnlineEntity.map.TryGetValue(self, out var oe))
            {
                if (!oe.owner.isMe)
                {
                    return;
                }
            }
            orig(self, den);
        }

        // Don't think
        private void AbstractCreature_Update(On.AbstractCreature.orig_Update orig, AbstractCreature self, int time)
        {
            if (OnlineManager.lobby != null && OnlineEntity.map.TryGetValue(self, out var oe))
            {
                if (!oe.owner.isMe)
                {
                    return;
                }
            }
            orig(self, time);
        }

        // Don't think
        private void AbstractPhysicalObject_Update(On.AbstractPhysicalObject.orig_Update orig, AbstractPhysicalObject self, int time)
        {
            if (OnlineManager.lobby != null && OnlineEntity.map.TryGetValue(self, out var oe))
            {
                if (!oe.owner.isMe)
                {
                    return;
                }
            }
            orig(self, time);
        }

        // Personas are set as non-transferable
        private AbstractCreature RainWorldGame_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate(On.RainWorldGame.orig_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate orig, RainWorldGame self, bool player1, bool player2, bool player3, bool player4, WorldCoordinate location)
        {
            if (OnlineManager.lobby != null)
            {
                sSpawningPersonas = true;
            }
            var ac = orig(self, player1, player2, player3, player4, location);
            sSpawningPersonas = false;
            return ac;
        }

        private void AbstractPhysicalObject_Realize(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
        {
            orig(self);
            if(OnlineManager.lobby != null && OnlineEntity.map.TryGetValue(self, out var oe))
            {
                if(!oe.owner.isMe && !oe.realized && oe.isTransferable)
                {
                    oe.Request();
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
            if (OnlineManager.lobby != null && OnlineEntity.map.TryGetValue(self, out var oe))
            {
                if (!oe.owner.isMe && !oe.realized && oe.isTransferable)
                {
                    if (oe.roomSession == null || !oe.roomSession.memberships.ContainsKey(oe.owner)) //if owner of oe is subscribed (is participant) do not request
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
            orig(self, coord);
            if (OnlineManager.lobby != null && OnlineEntity.map.TryGetValue(self, out var oe))
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
            orig(self, coord);
            if (OnlineManager.lobby != null && OnlineEntity.map.TryGetValue(self, out var oe))
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

        // Prevent creatures from entering a room if their online counterpart has not yet entered!
        private bool ShortcutHandlerOnVesselAllowedInRoom(On.ShortcutHandler.orig_VesselAllowedInRoom orig, ShortcutHandler self, ShortcutHandler.Vessel vessel)
        {
            var result = orig(self, vessel);
            if (OnlineManager.lobby == null) return result;

            var absCrit = vessel.creature.abstractCreature;
            OnlineEntity.map.TryGetValue(absCrit, out var onlineEntity);
            if (onlineEntity.owner.isMe) return result; // If entity is ours, game handles it normally.
            
            if (onlineEntity.roomSession?.absroom != vessel.room) result = false; // If OnlineEntity is not yet in the room, keep waiting.
            
            var connectedObjects = vessel.creature.abstractCreature.GetAllConnectedObjects();
            foreach (var apo in connectedObjects)
            {
                if (apo is AbstractCreature crit)
                {
                    OnlineEntity.map.TryGetValue(crit, out var innerOnlineEntity);
                    if (innerOnlineEntity.roomSession?.absroom != vessel.room) result = false; // Same for all connected entities
                }
            }

            if (result == false) Debug($"OnlineEntity {onlineEntity.id} not yet in destination room, keeping hostage...");
            return result;
        }
        
        // removes entities that should be deleted when going between rooms
        // not very robust also currently only handles creatures, should check recursively for grasps/connections
        private void ShortcutHandler_Update(ILContext il)
        {
            try
            {
                // cleanup betweenroomswaitinglobby of wandering entities
                var c = new ILCursor(il);
                
                c.GotoNext(moveType: MoveType.Before,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<ShortcutHandler>("betweenRoomsWaitingLobby"),
                    i => i.MatchCallOrCallvirt(out _),
                    i => i.MatchLdcI4(1)
                    );
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((ShortcutHandler self) => {
                    if(OnlineManager.lobby != null)
                    {
                        for (var i = self.betweenRoomsWaitingLobby.Count - 1; i >= 0; i--)
                        {
                            var vessel = self.betweenRoomsWaitingLobby[i];
                            if (OnlineEntity.map.TryGetValue(vessel.creature.abstractPhysicalObject, out var oe))
                            {
                                if(!oe.owner.isMe && oe.roomSession?.absroom != vessel.room)
                                {
                                    self.betweenRoomsWaitingLobby.Remove(vessel);
                                }
                            }
                        }
                    }
                });

                //// if moved and deleted, skip
                //ILLabel skip = null;
                //int indexLoc = 0;
                //c.GotoNext(moveType: MoveType.Before,
                //    i => i.MatchCallOrCallvirt<ShortcutHandler>("VesselAllowedInRoom"),
                //    i => i.MatchBrfalse(out skip) // get the skip target
                //    );
                //c.GotoNext(moveType: MoveType.Before,
                //    i => i.MatchLdarg(0),
                //    i => i.MatchLdfld<ShortcutHandler>("betweenRoomsWaitingLobby"),
                //    i => i.MatchLdloc(out indexLoc) // get the current index
                //    );
                //c.GotoNext(moveType: MoveType.After,
                //    i => i.MatchCallOrCallvirt<AbstractPhysicalObject>("Move") //here we juuuust moved
                //    );
                //c.MoveAfterLabels();
                //c.Emit(OpCodes.Ldarg_0);
                //c.Emit(OpCodes.Ldloc, indexLoc);
                //c.EmitDelegate((ShortcutHandler self, int index) => {
                //    if(OnlineManager.lobby != null)
                //    {
                //        var vessel = self.betweenRoomsWaitingLobby[index];
                //        if (vessel.creature.slatedForDeletetion)
                //        {
                //            RainMeadow.Debug("removing deleted creature" + vessel.creature);
                //            vessel.creature.slatedForDeletetion = false;
                //            self.betweenRoomsWaitingLobby.RemoveAt(index);
                //            return true;
                //        }
                //    }
                //    return false;
                //});
                //c.Emit(OpCodes.Brtrue, skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        // creature moving between rooms
        // vanilla calls removeentity + addentity but entity.pos is only updated LATER so we need this instead of addentity
        private void AbstractPhysicalObject_ChangeRooms(On.AbstractPhysicalObject.orig_ChangeRooms orig, AbstractPhysicalObject self, WorldCoordinate newCoord)
        {
            //RainMeadow.DebugMethod();
            orig(self, newCoord);
            if (OnlineManager.lobby != null && !self.slatedForDeletion && RoomSession.map.TryGetValue(self.world.GetAbstractRoom(newCoord.room), out var rs) && OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(rs, self))
            {
                rs.ApoEnteringRoom(self, newCoord);
            }
        }

        // not the main entry-point for room entities moving around
        // creature.move doesn't set the new pos until after it has moved, that's the issue
        // this is only for things that are ADDED directly to the room
        private void AbstractRoom_AddEntity(On.AbstractRoom.orig_AddEntity orig, AbstractRoom self, AbstractWorldEntity ent)
        {
            //RainMeadow.DebugMethod();
            orig(self, ent);
            if (OnlineManager.lobby != null && ent is AbstractPhysicalObject apo && apo.pos.room == self.index)
            {
                if (WorldSession.map.TryGetValue(self.world, out var ws) && OnlineManager.lobby.gameMode.ShouldSyncObjectInWorld(ws, apo)) ws.EntityEnteringWorld(apo);
                if (RoomSession.map.TryGetValue(self, out var rs) && OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(rs, apo)) rs.ApoEnteringRoom(apo, apo.pos);
            }
        }

        private void AbstractRoom_RemoveEntity(On.AbstractRoom.orig_RemoveEntity_AbstractWorldEntity orig, AbstractRoom self, AbstractWorldEntity entity)
        {
            //RainMeadow.DebugMethod();
            orig(self, entity);
            if (OnlineManager.lobby != null && entity is AbstractPhysicalObject apo && RoomSession.map.TryGetValue(self, out var rs) && OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(rs, apo))
            {
                rs.ApoLeavingRoom(apo);
            }
        }

        private void AbstractWorldEntity_Destroy(On.AbstractWorldEntity.orig_Destroy orig, AbstractWorldEntity self)
        {
            //RainMeadow.DebugMethod();
            orig(self);
            if (OnlineManager.lobby != null && self is AbstractPhysicalObject apo)
            {
                if (RoomSession.map.TryGetValue(self.Room, out var rs) && OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(rs, apo)) rs.ApoLeavingRoom(apo);
                if (WorldSession.map.TryGetValue(self.world, out var ws) && OnlineManager.lobby.gameMode.ShouldSyncObjectInWorld(ws, apo)) ws.EntityLeavingWorld(apo);
            }
        }

        // maybe leaving room, maybe entering world
        private void AbstractRoom_MoveEntityToDen(On.AbstractRoom.orig_MoveEntityToDen orig, AbstractRoom self, AbstractWorldEntity entity)
        {
            orig(self, entity);
            if (OnlineManager.lobby != null && entity is AbstractPhysicalObject apo)
            {
                if (RoomSession.map.TryGetValue(self, out var rs) && OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(rs, apo)) rs.ApoLeavingRoom(apo);
                if (WorldSession.map.TryGetValue(self.world, out var ws) && OnlineManager.lobby.gameMode.ShouldSyncObjectInWorld(ws, apo)) ws.EntityEnteringWorld(apo);
            }
        }

        // adds to entities already so no need to hook it!
        // private void AbstractRoom_MoveEntityOutOfDen(On.AbstractRoom.orig_MoveEntityOutOfDen orig, AbstractRoom self, AbstractWorldEntity ent) { }

        // Prevent adding item to update list twice
        private void RoomOnAddObject(On.Room.orig_AddObject orig, Room self, UpdatableAndDeletable obj)
        {
            if (OnlineManager.lobby != null && self.game != null && self.updateList.Contains(obj))
            {
                RainMeadow.Debug($"Object {(obj is PhysicalObject po ? po.abstractPhysicalObject.ID : obj)} already in the update list! Skipping...");
                var stackTrace = Environment.StackTrace;
                if (!stackTrace.Contains("Creature.PlaceInRoom") && !stackTrace.Contains("AbstractSpaceVisualizer")) // We know about this
                    RainMeadow.Error(Environment.StackTrace); // Log cases that we still haven't found 
                return;
            }
            orig(self, obj);
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
                        if (entities[i] is AbstractPhysicalObject apo && OnlineEntity.map.TryGetValue(apo, out var oe))
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
                                roomSession.EntityLeftResource(oe);
                                roomSession.worldSession.EntityLeftResource(oe);
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
                    OnlineManager.recentEntities = OnlineManager.recentEntities.Where(e => e.Value.roomSession == roomSession2 || e.Value.owner.isMe).ToDictionary();
                    
                    // we go over all APOs in the room
                    RainMeadow.Debug("Gate switchery 2");
                    var entities = room.abstractRoom.entities;
                    for (int i = entities.Count - 1; i >= 0; i--)
                    {
                        if (entities[i] is AbstractPhysicalObject apo && OnlineEntity.map.TryGetValue(apo, out var oe))
                        {
                            if (oe.owner.isMe)
                            {
                                RainMeadow.Debug("readding entity to world" + oe);
                                oe.enterPos = apo.pos;
                                roomSession2.worldSession.EntityEnteredResource(oe);
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
