﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;

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
        }

        private void Creature_SpitOutOfShortCut(On.Creature.orig_SpitOutOfShortCut orig, Creature self, IntVector2 pos, Room newRoom, bool spitOutAllSticks)
        {
            if (self is Player && !self.IsLocal())
            {
                self.collisionLayer = 0; // doesn't help non-MSC
            }
            orig(self, pos, newRoom, spitOutAllSticks);
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
            if (onlineEntity.isMine) return result; // If entity is ours, game handles it normally.
            if (onlineEntity.roomSession?.absroom != vessel.room)
            {
                Trace($"Denied because in wrong room: vessel at {vessel.room.name}:{vessel.room.index} entity at:{onlineEntity.roomSession?.absroom.name ?? "null"}{onlineEntity.roomSession?.absroom.index.ToString() ?? "null"}");
                result = false; // If OnlineEntity is not yet in the room, keep waiting.
            }

            var connectedObjects = vessel.creature.abstractCreature.GetAllConnectedObjects();
            foreach (var apo in connectedObjects)
            {
                if (OnlinePhysicalObject.map.TryGetValue(apo, out var innerOnlineEntity))
                {
                    if (innerOnlineEntity.roomSession?.absroom != vessel.room)
                    {
                        Trace($"Denied because of connected object: {innerOnlineEntity}");
                        result = false; // Same for all connected entities
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

        // event driven shortcutting for remotes
        private void CreatureSuckedIntoShortCut(On.Creature.orig_SuckedIntoShortCut orig, Creature self, IntVector2 entrancePos, bool carriedByOther)
        {
            if (OnlineManager.lobby == null)
            {
                orig(self, entrancePos, carriedByOther);
                return;
            }

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
                orig(self, entrancePos, carriedByOther);
                onlineCreature.enteringShortCut = false;
            }
            else if (onlineCreature.isMine)
            {
                // tell everyone else that I am about to enter a shortcut!
                RainMeadow.Debug($"{onlineCreature} sucked into shortcut");

                onlineCreature.BroadcastRPCInRoom(onlineCreature.SuckedIntoShortCut, entrancePos, carriedByOther);
                if (self is Player pl)
                {
                    if (pl.grasps != null) // we're  dragging
                    {
                        for (int num = pl.grasps.Length - 1; num >= 0; num--)
                        {
                            if (pl.grasps[num] != null && pl.grasps[num].grabbed is Player grabbedPlayer)
                            {
                                if (OnlinePhysicalObject.map.TryGetValue(grabbedPlayer.abstractPhysicalObject, out var onlineSlugBeingDragged))
                                {
                                    onlineSlugBeingDragged.BroadcastRPCInRoom((onlineSlugBeingDragged as OnlineCreature).SuckedIntoShortCut, entrancePos, carriedByOther);
                                }
                            }
                        }
                    }

                    if (ModManager.MSC && pl.slugOnBack != null && pl.slugOnBack.HasASlug) // we're backpacking
                    {
                        if (OnlinePhysicalObject.map.TryGetValue(pl.slugOnBack.slugcat.abstractPhysicalObject, out var onlineSlugOnBack))
                        {
                            onlineSlugOnBack.BroadcastRPCInRoom((onlineSlugOnBack as OnlineCreature).SuckedIntoShortCut, entrancePos, carriedByOther);
                        }
                    }
                }
                orig(self, entrancePos, carriedByOther);
            }
            else
            {
                // Don't run
                // Clear shortcut that it was meant to enter
                self.enteringShortCut = null;
            }
        }
    }
}
