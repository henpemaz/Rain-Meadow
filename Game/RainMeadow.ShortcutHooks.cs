using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        private void ShortcutHooks()
        {
            On.Room.AddObject += RoomOnAddObject; // Prevent adding item to update list twice

            IL.ShortcutHandler.Update += ShortcutHandler_Update; // cleanup of entities in shortcut system
            On.ShortcutHandler.VesselAllowedInRoom += ShortcutHandlerOnVesselAllowedInRoom; // Prevent creatures from entering a room if their online counterpart has not yet entered!

            On.ShortcutHandler.CreatureTakeFlight += ShortcutHandler_CreatureTakeFlight;
            On.Creature.SuckedIntoShortCut += CreatureSuckedIntoShortCut;
            
            On.Creature.SpitOutOfShortCut += Creature_SpitOutOfShortCut;
            // On.Creature.SpitOutOfShortCut += CreatureSpitOutOfShortCut;
        }

        // adds to entities already so no need to hook it!
        // private void AbstractRoom_MoveEntityOutOfDen(On.AbstractRoom.orig_MoveEntityOutOfDen orig, AbstractRoom self, AbstractWorldEntity ent) { }

        // Prevent adding item to update list twice
        private void RoomOnAddObject(On.Room.orig_AddObject orig, Room self, UpdatableAndDeletable obj)
        {
            if (OnlineManager.lobby != null && self.game != null && self.updateList.Contains(obj))
            {
                Debug($"Object {obj} - {(obj is PhysicalObject po ? po.abstractPhysicalObject.ID : obj)} already in the update list! Skipping...");
                var stackTrace = Environment.StackTrace;
                if (!stackTrace.Contains("AbstractSpaceVisualizer")) // We know about this
                    Error(Environment.StackTrace); // Log cases that we still haven't found 
                return;
            }
            orig(self, obj);
            if (OnlineManager.lobby != null)
            {
                if (obj is PhysicalObject po && po.abstractPhysicalObject is AbstractPhysicalObject apo && !apo.GetOnlineObject(out _))
                {
                    self.world.GetResource()?.ApoEnteringWorld(apo);
                    self.abstractRoom.GetResource()?.ApoEnteringRoom(apo, apo.pos);
                }
            }
        }

        // removes entities that should be deleted when going between rooms
        // not very robust also currently only handles creatures, should check recursively for grasps/connections
        private void ShortcutHandler_Update(ILContext il)
        {
            try
            {

                var c = new ILCursor(il);


                //// cleanup betweenroomswaitinglobby of wandering entities
                //// this is right before the forloop on betweenRoomsWaitingLobby
                //c.GotoNext(moveType: MoveType.Before,
                //    i => i.MatchLdarg(0),
                //    i => i.MatchLdfld<ShortcutHandler>("betweenRoomsWaitingLobby"),
                //    i => i.MatchCallOrCallvirt(out _),
                //    i => i.MatchLdcI4(1)
                //    );
                //c.MoveAfterLabels();
                //c.Emit(OpCodes.Ldarg_0);
                //c.EmitDelegate((ShortcutHandler self) =>
                //{
                //    if (OnlineManager.lobby != null)
                //    {
                //        for (var i = self.betweenRoomsWaitingLobby.Count - 1; i >= 0; i--)
                //        {
                //            var vessel = self.betweenRoomsWaitingLobby[i];
                //            if (OnlinePhysicalObject.map.TryGetValue(vessel.creature.abstractPhysicalObject, out var oe))
                //            {
                //                if (!oe.isMine && oe.roomSession?.absroom != vessel.room)
                //                {
                //                    self.betweenRoomsWaitingLobby.Remove(vessel);
                //                    foreach (var obj in vessel.creature.abstractCreature.GetAllConnectedObjects())
                //                    {
                //                        if (obj.realizedObject is Creature c) c.inShortcut = false;
                //                    }
                //                }
                //            }
                //        }
                //    }
                //});


                // if (this.betweenRoomsWaitingLobby[k].room.realizedRoom == null)
                // becomes if (this.betweenRoomsWaitingLobby[k].room.realizedRoom == null && ...)
                int indexvar = default;
                ILLabel skip = default;
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<ShortcutHandler>("betweenRoomsWaitingLobby"),
                    i => i.MatchLdloc(out indexvar),
                    i => i.MatchCallOrCallvirt(out _),
                    i => i.MatchLdfld<ShortcutHandler.Vessel>("room"),
                    i => i.MatchLdfld<AbstractRoom>("realizedRoom"),
                    i => i.MatchBrtrue(out skip)
                    );

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, indexvar);
                c.EmitDelegate((ShortcutHandler self, int i) =>
                {
                    if (OnlineManager.lobby != null)
                    {
                        var vessel = self.betweenRoomsWaitingLobby[i];
                        if (OnlinePhysicalObject.map.TryGetValue(vessel.creature.abstractPhysicalObject, out var oe))
                        {
                            if (!oe.isMine && oe.roomSession?.absroom != vessel.room)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                });
                c.Emit(OpCodes.Brtrue, skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        // Prevent creatures from entering a room if their online counterpart has not yet entered!
        private bool ShortcutHandlerOnVesselAllowedInRoom(On.ShortcutHandler.orig_VesselAllowedInRoom orig, ShortcutHandler self, ShortcutHandler.Vessel vessel)
        {
            var result = orig(self, vessel);
            if (OnlineManager.lobby == null) return result;

            var absCrit = vessel.creature.abstractCreature;
            if (!OnlinePhysicalObject.map.TryGetValue(absCrit, out var onlineEntity))
            {
                RainMeadow.Error($"Untracked entity: " + absCrit);
                return result;
            }

            var connectedObjects = vessel.creature.abstractCreature.GetAllConnectedObjects();
            if (connectedObjects.All(x => x.IsLocal())) {
                return result; 
            }

            foreach (var apo in connectedObjects)
            {
                if (OnlinePhysicalObject.map.TryGetValue(apo, out var innerOnlineEntity))
                {
                    if ((innerOnlineEntity == onlineEntity) && onlineEntity.isMine) continue;
                    if (innerOnlineEntity.roomSession?.absroom != vessel.room)
                    {
                        Trace($"Denied because of connected object: {innerOnlineEntity}");
                        result = false; // Same for all connected entities
                        if (apo.Room != vessel.room && apo.CanMove()) {

                            bool ready = true;
                            if (!innerOnlineEntity.isTransferable && apo.realizedObject is not null) {
                                apo.world.ActivateRoom(vessel.room);
                                if (apo is AbstractCreature critter) {
                                    ready = ready && self.CreatureAllowedInRoom(critter, vessel.room.realizedRoom);
                                }  
                            }
                          
                            RoomSession? session = vessel.room.GetResource();
                            if (session is not null) {
                                session.Needed();
                                if (session.isAvailable && !session.isPending && ready) {
                                    WorldCoordinate newCoord = new WorldCoordinate(vessel.room.index, -1, -1, -1);
                                    apo.MoveMovable(newCoord);
                                }
                            }
                        }

                    }
                }
                else
                {
                    RainMeadow.Error($"Untracked entity: " + apo);
                    RainMeadow.Error($"connected to: {absCrit} {onlineEntity}");
                }
            }

            if (result == false) Trace($"OnlineEntity {onlineEntity} not yet in destination room, keeping hostage...");
            return result;
        }

        private void ShortcutHandler_CreatureTakeFlight(On.ShortcutHandler.orig_CreatureTakeFlight orig, ShortcutHandler self, Creature creature, AbstractRoomNode.Type type, WorldCoordinate start, WorldCoordinate dest)
        {
            if (OnlineManager.lobby is null)
            {
                orig(self, creature, type, start, dest);
                return;
            }

            if (!OnlinePhysicalObject.map.TryGetValue(creature.abstractCreature, out var onlineEntity))
            {
                Error($"Entity {creature} - {creature.abstractCreature.ID} doesn't exist in online space!");
                orig(self, creature, type, start, dest);
                return;
            }

            var onlineCreature = (OnlineCreature)onlineEntity;

            if (onlineCreature.isMine)
            {
                RainMeadow.Debug($"{onlineCreature} took flight");
                onlineCreature.BroadcastRPCInRoom(onlineCreature.TookFlight, type, start, dest);
            }
            else if (onlineCreature.enteringShortCut) // If this call was from a processing event
            {
                onlineCreature.enteringShortCut = false;
            }
            else
            {
                RainMeadow.Error($"Remote entity trying to take flight: {onlineCreature} at {onlineCreature.roomSession}");
                return;
            }
            orig(self, creature, type, start, dest);
        }


        private List<OnlineCreature> creatures_who_reclaim_sticks = new();
        private bool IsTakingUnmoveableObject(Creature self, IntVector2 entrancePos) {
            // currently unused.
            return false; 
        }

        // event driven shortcutting for remotes
        private void CreatureSuckedIntoShortCut(On.Creature.orig_SuckedIntoShortCut orig, Creature self, IntVector2 entrancePos, bool carriedByOther)
        {
            if (OnlineManager.lobby == null)
            {
                orig(self, entrancePos, carriedByOther);
                return;
            }

            var room = self.room.abstractRoom.GetResource();

            if (!OnlinePhysicalObject.map.TryGetValue(self.abstractCreature, out var onlineEntity))
            {
                Error($"Entity {self} - {self.abstractCreature.ID} doesn't exist in online space!");
                orig(self, entrancePos, carriedByOther);
                return;
            }

            var onlineCreature = (OnlineCreature)onlineEntity;
            if (self.inShortcut)
            {
                RainMeadow.Error($"{onlineCreature} was already in shortcuts");
            }

            if (onlineCreature.enteringShortCut) // If this call was from a processing event
            {
                RainMeadow.Debug($"{onlineCreature} sucked into shortcut from remote");

                if (!IsTakingUnmoveableObject(self, entrancePos)) {
                    orig(self, entrancePos, carriedByOther);
                    onlineCreature.enteringShortCut = false;
                    if (room.isOwner) // proccessed, now broadcast
                    {
                        onlineCreature.BroadcastRPCInRoomExceptOwners(onlineCreature.SuckedIntoShortCut, entrancePos, carriedByOther, false);
                    }              
                } else self.enteringShortCut = null;

            }
            else if (onlineCreature.isMine)
            {
                if (!IsTakingUnmoveableObject(self, entrancePos)) {
                    orig(self, entrancePos, carriedByOther);
                    RainMeadow.Debug($"{onlineCreature} sucked into shortcut locally");
                    
                    if (room.isOwner) // now broadcast
                    {
                        onlineCreature.BroadcastRPCInRoomExceptOwners(onlineCreature.SuckedIntoShortCut, entrancePos, carriedByOther, false);
                    }
                    else // tell room-owner about it so it gets broadcasted
                    {
                        room.owner.InvokeRPC(onlineCreature.SuckedIntoShortCut, entrancePos, carriedByOther, false);
                    }
                } else self.enteringShortCut = null;
            }
            else
            {
                // Don't run
                // Clear shortcut that it was meant to enter
                self.enteringShortCut = null;
            }
        }

        private void Creature_SpitOutOfShortCut(On.Creature.orig_SpitOutOfShortCut orig, Creature self, IntVector2 pos, Room newRoom, bool spitOutAllSticks) 
        {
            RainMeadow.DebugMe();            
            orig(self, pos, newRoom, spitOutAllSticks);
            if (OnlineManager.lobby == null) {
                return;
            }

            if (!self.abstractCreature.GetOnlineCreature(out var onlineEntity))
            {
                Error($"Entity {self} - {self.abstractCreature.ID} doesn't exist in online space!");
                return;
            }


            if (onlineEntity.isMine) 
            {
                if (spitOutAllSticks && (newRoom.abstractRoom.GetResource() is RoomSession rs)) 
                {
                    List<OnlinePlayer> players_who_need_to_know = new();
                    if (onlineEntity.currentlyJoinedResource is RoomSession room)
                    {
                        players_who_need_to_know.AddDistinctRange(room.participants);
                    }
                    foreach (var connectedobj in self.abstractCreature.GetAllConnectedObjects())
                    {
                        if (connectedobj.GetOnlineObject(out var inneronlineEntity))
                        {
                            if (inneronlineEntity.owner is not null) 
                            {
                                players_who_need_to_know.Add(inneronlineEntity.owner);
                            }
                        }
                    }
                    
                    players_who_need_to_know = players_who_need_to_know.Distinct().ToList();
                    foreach (var participant in players_who_need_to_know)
                    {
                        if (!participant.isMe)
                        {
                            participant.InvokeRPC(onlineEntity.SpitOutOfShortCut, pos, rs, spitOutAllSticks);
                        }
                    }
                }
            }
        }
    }
}
