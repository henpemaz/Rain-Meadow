using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Linq;

namespace RainMeadow
{
    partial class RainMeadow
    {
        private void ShortcutHooks()
        {
            On.Room.AddObject += RoomOnAddObject; // Prevent adding item to update list twice

            IL.ShortcutHandler.Update += ShortcutHandler_Update; // cleanup of deleted entities in shortcut system
            On.ShortcutHandler.VesselAllowedInRoom += ShortcutHandlerOnVesselAllowedInRoom; // Prevent creatures from entering a room if their online counterpart has not yet entered!
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
                    if (OnlineManager.lobby != null)
                    {
                        for (var i = self.betweenRoomsWaitingLobby.Count - 1; i >= 0; i--)
                        {
                            var vessel = self.betweenRoomsWaitingLobby[i];
                            if (OnlinePhysicalObject.map.TryGetValue(vessel.creature.abstractPhysicalObject, out var oe))
                            {
                                if (!oe.isMine && oe.roomSession?.absroom != vessel.room)
                                {
                                    self.betweenRoomsWaitingLobby.Remove(vessel);
                                }
                            }
                        }
                    }
                });
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
            OnlinePhysicalObject.map.TryGetValue(absCrit, out var onlineEntity);
            if (onlineEntity.isMine) return result; // If entity is ours, game handles it normally.

            if (onlineEntity.roomSession?.absroom != vessel.room) result = false; // If OnlineEntity is not yet in the room, keep waiting.

            var connectedObjects = vessel.creature.abstractCreature.GetAllConnectedObjects();
            foreach (var apo in connectedObjects)
            {
                if (apo is AbstractCreature crit)
                {
                    OnlinePhysicalObject.map.TryGetValue(crit, out var innerOnlineEntity);
                    if (innerOnlineEntity.roomSession?.absroom != vessel.room) result = false; // Same for all connected entities
                }
            }

            if (result == false) Debug($"OnlineEntity {onlineEntity.id} not yet in destination room, keeping hostage...");
            return result;
        }

    }
}
