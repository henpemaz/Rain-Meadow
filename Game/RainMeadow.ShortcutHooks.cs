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
            On.ShortcutHandler.Update += ShortcutHandler_UpdateWaitForConnections;
            On.ShortcutHandler.VesselAllowedInRoom += ShortcutHandlerOnVesselAllowedInRoom; // Prevent creatures from entering a room if their online counterpart has not yet entered!
            On.ShortcutHandler.CreatureTakeFlight += ShortcutHandler_CreatureTakeFlight;
            On.Creature.SuckedIntoShortCut += CreatureSuckedIntoShortCut;
        }

        private void ShortcutHandler_UpdateWaitForConnections(On.ShortcutHandler.orig_Update orig, ShortcutHandler self) {
            if (OnlineManager.lobby != null) {
                foreach (ShortcutHandler.ShortCutVessel vessel in self.transportVessels) {
                    if (vessel.creature == null) continue;

                    foreach (AbstractPhysicalObject obj in vessel.creature.abstractCreature.GetAllConnectedObjects()) {
                        
                        var onlineobj = obj.GetOnlineObject();
                        if (onlineobj == null) {
                            Error($"Entity {obj} - {obj.ID} doesn't exist in online space!");
                            continue;
                        }

                        if (obj == vessel.creature.abstractCreature) continue;
                        if (onlineobj.isTransferable || onlineobj.isMine) continue;
                        if (!onlineobj.readyForVessel) vessel.wait = 5;
                    }    
                }
            }
            orig(self);
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

        class ReadyForVesselNotifier : UpdatableAndDeletable, INotifyWhenRoomIsReady {
            public OnlinePhysicalObject onlinePhysicalObject;

            public ReadyForVesselNotifier(OnlinePhysicalObject opo) {
                onlinePhysicalObject = opo;
            }
            void INotifyWhenRoomIsReady.AIMapReady() {
                onlinePhysicalObject.BroadcastRPCInRoom(onlinePhysicalObject.ReadyForVessel);
                this.Destroy();
            }

            void INotifyWhenRoomIsReady.ShortcutsReady() { }

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
                // This is so that our unowned connected objects load the room we are about to enter.
                // Specifically helpful for backpacked player Slugcats.
                
                ShortcutData shortcutData = self.room.shortcutData(entrancePos);
                if (shortcutData.shortCutType == ShortcutData.Type.RoomExit) {
                    int destroom = self.room.abstractRoom.connections[shortcutData.destNode];
                    if (destroom > -1) {
                        // If the shortcut we are entering is leaving the room.
                        foreach (AbstractPhysicalObject obj in self.abstractCreature.GetAllConnectedObjects()) {
                            // what objects are coming with us?
                            var onlineobj = obj.GetOnlineObject();
                            if (onlineobj == null) {
                                Error($"Entity {obj} - {obj.ID} doesn't exist in online space!");
                                continue;
                            }

                            // They're not ready for the vessel because we haven't told them about it yet.
                            onlineobj.readyForVessel = false;
                            if (onlineobj.isTransferable || !onlineobj.isMine) continue;

                            // If the objects mine. I should get ready...
                            var abstractRoom = self.room.world.GetAbstractRoom(destroom);
                            abstractRoom.world.ActivateRoom(abstractRoom);
                            
                            // Broadcast to everyone when I'm ready
                            if (abstractRoom.realizedRoom.readyForAI) {
                                onlineobj.BroadcastRPCInRoom(onlineobj.ReadyForVessel);
                            } else {
                                abstractRoom.realizedRoom.AddObject(new ReadyForVesselNotifier(onlineobj));
                            }   
                        }
                    }
                }  else {
                    foreach (AbstractPhysicalObject obj in self.abstractCreature.GetAllConnectedObjects()) {
                        var onlineobj = obj.GetOnlineObject();
                        if (onlineobj == null) {
                            Error($"Entity {obj} - {obj.ID} doesn't exist in online space!");
                            continue;
                        }

                        // If it's not a room exit. they are ready.
                        onlineobj.readyForVessel = true;
                    }
                }
                

                RainMeadow.Debug($"{onlineCreature} sucked into shortcut from remote");
                orig(self, entrancePos, carriedByOther);
                onlineCreature.enteringShortCut = false;
                if (room.isOwner) // proccessed, now broadcast
                {
                    onlineCreature.BroadcastRPCInRoomExceptOwners(onlineCreature.SuckedIntoShortCut, entrancePos, carriedByOther);
                }
            }
            else if (onlineCreature.isMine)
            {
                orig(self, entrancePos, carriedByOther);
                RainMeadow.Debug($"{onlineCreature} sucked into shortcut locally");
                
                if (room.isOwner) // now broadcast
                {
                    onlineCreature.BroadcastRPCInRoomExceptOwners(onlineCreature.SuckedIntoShortCut, entrancePos, carriedByOther);
                }
                else // tell room-owner about it so it gets broadcasted
                {
                    room.owner.InvokeRPC(onlineCreature.SuckedIntoShortCut, entrancePos, carriedByOther);
                }
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
