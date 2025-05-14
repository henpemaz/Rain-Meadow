using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        private void ShortcutHooks()
        {
            On.Room.AddObject += RoomOnAddObject; // Prevent adding item to update list twice

            IL.ShortcutHandler.Update += ShortcutHandler_Update; // cleanup of entities in shortcut system
            On.ShortcutHandler.Update += ShortcutHandler_Update1;
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

                // this.betweenRoomsWaitingLobby[k].creature.abstractCreature.Move(newCoord)
                // becomes 
                //
                
                int inbetween_room_index_loc = 0;
                int newCoord_loc = 0;
                c.GotoNext(MoveType.Before,
                // 579	0795	ldarg.0
                // 580	0796	ldfld	class [mscorlib]System.Collections.Generic.List`1<class ShortcutHandler/Vessel> ShortcutHandler::betweenRoomsWaitingLobby
                // 581	079B	ldloc.s	V_8 (8)
                // 582	079D	callvirt	instance !0 class [mscorlib]System.Collections.Generic.List`1<class ShortcutHandler/Vessel>::get_Item(int32)
                // 583	07A2	ldfld	class Creature ShortcutHandler/Vessel::creature
                // 584	07A7	callvirt	instance class AbstractCreature Creature::get_abstractCreature()
                // 585	07AC	ldloc.s	V_10 (10)
                // 586	07AE	callvirt	instance void AbstractPhysicalObject::Move(valuetype WorldCoordinate)

                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<ShortcutHandler>(nameof(ShortcutHandler.betweenRoomsWaitingLobby)),
                    x => x.MatchLdloc(out inbetween_room_index_loc),
                    x => x.MatchCallvirt(out _),
                    x => x.MatchLdfld<ShortcutHandler.Vessel>(nameof(ShortcutHandler.Vessel.creature)),
                    x => x.MatchCallvirt(out _),
                    x => x.MatchLdloc(out newCoord_loc),
                    x => x.MatchCallvirt<AbstractPhysicalObject>(nameof(AbstractPhysicalObject.Move))
                );

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, inbetween_room_index_loc);
                c.Emit(OpCodes.Ldloc, newCoord_loc);
                c.EmitDelegate((ShortcutHandler handler, int inbetween_room_index, WorldCoordinate cord) => {
                    Debug($"{handler}, {inbetween_room_index}, {cord}");
                    if (OnlineManager.lobby != null) {
                        var creature = handler.betweenRoomsWaitingLobby[inbetween_room_index].creature?.abstractCreature;
                        if (creature?.GetOnlineCreature() is OnlineCreature oc) {
                            oc.AllMoving(true);
                            creature.Move(cord);
                            oc.AllMoving(false);
                            return true;
                        }
                    }
                    return false;
                });


                // skip vanilla move if we have an online creature.
                int curindex = c.Index;
                ILCursor[] cursor;
                c.FindNext(out cursor,
                    x => x.MatchCallvirt<AbstractPhysicalObject>(nameof(AbstractPhysicalObject.Move))
                );

                c.Emit(OpCodes.Brtrue, cursor[0].Next.Next);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        // prevent creature from being spit out of a shortcut if we don't own it
        // owner will send us an RPC to spit it out.
        private void ShortcutHandler_Update1(On.ShortcutHandler.orig_Update orig, ShortcutHandler self)
        {
            try {
                for (int i = self.transportVessels.Count - 1; i >= 0; i--)
                {
                    if (!self.transportVessels[i].creature.IsLocal())
                    {
                        Room realized_room = self.transportVessels[i].room.realizedRoom;
                        IntVector2 next_pos = ShortcutHandler.NextShortcutPosition(self.transportVessels[i].pos, self.transportVessels[i].lastPos, realized_room);
                        if (realized_room.GetTile(next_pos).Terrain == Room.Tile.TerrainType.ShortcutEntrance) {
                            self.transportVessels[i].wait = 5;
                        }
                    }
                }
            } catch (Exception err) {
                RainMeadow.Error(err);
            }

            orig(self);
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
                        if (apo.Room != vessel.room && innerOnlineEntity.isMine) {

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
                                    apo.MoveOnly(newCoord);
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
