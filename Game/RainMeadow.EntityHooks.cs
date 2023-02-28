using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Reflection;

namespace RainMeadow
{
    partial class RainMeadow
    {
        private void EntityHooks()
        {
            On.AbstractPhysicalObject.ctor += AbstractPhysicalObject_ctor;
            On.AbstractCreature.ctor += AbstractCreature_ctor; ;

            On.AbstractRoom.AddEntity += AbstractRoom_AddEntity;
            On.AbstractRoom.RemoveEntity_AbstractWorldEntity += AbstractRoom_RemoveEntity;
            On.AbstractPhysicalObject.ChangeRooms += AbstractPhysicalObject_ChangeRooms;

            IL.ShortcutHandler.Update += ShortcutHandler_Update; // cleanup of deleted entities in shortcut system

            On.AbstractPhysicalObject.Move += AbstractPhysicalObject_Move; // debug
            On.RoomRealizer.RealizeAndTrackRoom += RoomRealizer_RealizeAndTrackRoom; // debug
        }

        // disable preemptive loading for ease of debugging
        private void RoomRealizer_RealizeAndTrackRoom(On.RoomRealizer.orig_RealizeAndTrackRoom orig, RoomRealizer self, AbstractRoom room, bool actuallyEntering)
        {
            if (!actuallyEntering) return;
            orig(self, room, actuallyEntering);
        }


        // debug
        private void AbstractPhysicalObject_Move(On.AbstractPhysicalObject.orig_Move orig, AbstractPhysicalObject self, WorldCoordinate newCoord)
        {
            RainMeadow.Debug($"from {self.pos} to {newCoord}");
            orig(self, newCoord);
        }

        // removes entities that should be deleted when going between rooms
        // not very robust also currently only handles creatures, should check recursively for grasps/connections
        private void ShortcutHandler_Update(ILContext il)
        {
            try
            {
                // cleanup betweenroomswaitinglobby of deleted entities
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
                    if(self.game.session is OnlineGameSession)
                    {
                        for (int i = self.transportVessels.Count - 1; i >= 0; i--)
                        {
                            var vessel = self.transportVessels[i];
                            if (vessel.creature.slatedForDeletetion)
                            {
                                RainMeadow.Debug("removing deleted creature" + vessel.creature);
                                vessel.creature.slatedForDeletetion = false;
                                self.transportVessels.RemoveAt(i);
                            }
                        }
                        for (int i = self.betweenRoomsWaitingLobby.Count - 1; i >= 0; i--)
                        {
                            var vessel = self.betweenRoomsWaitingLobby[i];
                            if (vessel.creature.slatedForDeletetion)
                            {
                                RainMeadow.Debug("removing deleted creature" + vessel.creature);
                                vessel.creature.slatedForDeletetion = false;
                                self.betweenRoomsWaitingLobby.RemoveAt(i);
                            }
                        }
                    }
                });

                // if moved and deleted, skip
                ILLabel skip = null;
                int indexLoc = 0;
                c.GotoNext(moveType: MoveType.Before,
                    i => i.MatchCallOrCallvirt<ShortcutHandler>("VesselAllowedInRoom"),
                    i => i.MatchBrfalse(out skip) // get the skip target
                    );
                c.GotoNext(moveType: MoveType.Before,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<ShortcutHandler>("betweenRoomsWaitingLobby"),
                    i => i.MatchLdloc(out indexLoc) // get the current index
                    );
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchCallOrCallvirt<AbstractPhysicalObject>("Move") //here we juuuust moved
                    );
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, indexLoc);
                c.EmitDelegate((ShortcutHandler self, int index) => {
                    if(self.game.session is OnlineGameSession)
                    {
                        var vessel = self.betweenRoomsWaitingLobby[index];
                        if (vessel.creature.slatedForDeletetion)
                        {
                            RainMeadow.Debug("removing deleted creature" + vessel.creature);
                            vessel.creature.slatedForDeletetion = false;
                            self.betweenRoomsWaitingLobby.RemoveAt(index);
                            return true;
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

        // creature moving between rooms
        // vanilla calls removeentity + addentity but entity.pos is only updated LATER so we need this instead of addentity
        private void AbstractPhysicalObject_ChangeRooms(On.AbstractPhysicalObject.orig_ChangeRooms orig, AbstractPhysicalObject self, WorldCoordinate newCoord)
        {
            RainMeadow.DebugMethod();
            orig(self, newCoord);
            if (self.world.game.session is OnlineGameSession os && !self.slatedForDeletion && RoomSession.map.TryGetValue(self.world.GetAbstractRoom(newCoord.room), out var rs) && os.ShouldSyncObjectInRoom(rs, self))
            {
                rs.EntityEnteringRoom(self, newCoord);
            }
        }

        // not the main entry-point for room entities moving around
        // creature.move doesn't set the new pos until after it has moved, that's the issue
        // this is only for things that are ADDED directly to the room
        private void AbstractRoom_AddEntity(On.AbstractRoom.orig_AddEntity orig, AbstractRoom self, AbstractWorldEntity ent)
        {
            RainMeadow.DebugMethod();
            orig(self, ent);
            if (self.world.game.session is OnlineGameSession os && !ent.slatedForDeletion && ent is AbstractPhysicalObject apo && apo.pos.room == self.index && RoomSession.map.TryGetValue(self, out var rs) && os.ShouldSyncObjectInRoom(rs, apo))
            {
                rs.EntityEnteringRoom(apo, apo.pos);
            }
        }

        private void AbstractRoom_RemoveEntity(On.AbstractRoom.orig_RemoveEntity_AbstractWorldEntity orig, AbstractRoom self, AbstractWorldEntity entity)
        {
            RainMeadow.DebugMethod();
            orig(self, entity);
            if (self.world.game.session is OnlineGameSession os && !entity.slatedForDeletion && entity is AbstractPhysicalObject apo && RoomSession.map.TryGetValue(self, out var rs) && os.ShouldSyncObjectInRoom(rs, apo))
            {
                rs.EntityLeavingRoom(apo);
            }
        }

        private void AbstractPhysicalObject_ctor(On.AbstractPhysicalObject.orig_ctor orig, AbstractPhysicalObject self, World world, AbstractPhysicalObject.AbstractObjectType type, PhysicalObject realizedObject, WorldCoordinate pos, EntityID ID)
        {
            RainMeadow.DebugMethod();
            orig(self, world, type, realizedObject, pos, ID);
            if (world?.game?.session is OnlineGameSession os && WorldSession.map.TryGetValue(world, out var ws) && self is not AbstractCreature && os.ShouldSyncObjectInWorld(ws, self))
            {
                ws.NewEntityInWorld(self);
            }
        }


        private void AbstractCreature_ctor(On.AbstractCreature.orig_ctor orig, AbstractCreature self, World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID)
        {
            RainMeadow.DebugMethod();
            orig(self, world, creatureTemplate, realizedCreature, pos, ID);
            if (world?.game?.session is OnlineGameSession os && WorldSession.map.TryGetValue(world, out var ws) && os.ShouldSyncObjectInWorld(ws, self))
            {
                ws.NewEntityInWorld(self);
            }
        }
    }
}
