using System;

namespace RainMeadow
{
    partial class RainMeadow
    {
        private void EntityHooks()
        {
            On.AbstractRoom.AddEntity += AbstractRoom_AddEntity;
            On.AbstractRoom.RemoveEntity_AbstractWorldEntity += AbstractRoom_RemoveEntity;
            On.AbstractPhysicalObject.ChangeRooms += AbstractPhysicalObject_ChangeRooms;

            On.AbstractPhysicalObject.Move += AbstractPhysicalObject_Move;
        }


        // debug
        private void AbstractPhysicalObject_Move(On.AbstractPhysicalObject.orig_Move orig, AbstractPhysicalObject self, WorldCoordinate newCoord)
        {
            RainMeadow.Debug($"from {self.pos} to {newCoord}");
            orig(self, newCoord);
        }

        // creature moving between rooms
        // vanilla calls removeentity + addentity but entity.pos is only updated LATER so we need this instead of addentity
        private void AbstractPhysicalObject_ChangeRooms(On.AbstractPhysicalObject.orig_ChangeRooms orig, AbstractPhysicalObject self, WorldCoordinate newCoord)
        {
            RainMeadow.DebugMethod();
            orig(self, newCoord);
            RainMeadow.Debug(2);
            if (self.world.game.session is OnlineGameSession os && !self.slatedForDeletion)
            {
                OnlineManager.lobby.worldSessions[self.world.region.name].roomSessions[self.world.GetAbstractRoom(newCoord.room).name].EntityEnteringRoom(self, newCoord);
            }
        }

        // not the main entry-point, things will have been added already
        // creature.move doesn't set the new pos until after it has moved, that's the issue
        // this is only for things that are ADDED directly to the room
        private void AbstractRoom_AddEntity(On.AbstractRoom.orig_AddEntity orig, AbstractRoom self, AbstractWorldEntity ent)
        {
            RainMeadow.DebugMethod();
            orig(self, ent);
            RainMeadow.Debug(2);
            if (self.world.game.session is OnlineGameSession os && !ent.slatedForDeletion && ent is AbstractPhysicalObject apo && apo.pos.room == self.index)
            {
                OnlineManager.lobby.worldSessions[self.world.region.name].roomSessions[self.name].EntityEnteringRoom(apo, apo.pos);
            }
        }

        private void AbstractRoom_RemoveEntity(On.AbstractRoom.orig_RemoveEntity_AbstractWorldEntity orig, AbstractRoom self, AbstractWorldEntity entity)
        {
            RainMeadow.DebugMethod();
            orig(self, entity);
            RainMeadow.Debug(2);
            if (self.world.game.session is OnlineGameSession os && !entity.slatedForDeletion && entity is AbstractPhysicalObject apo)
            {
                OnlineManager.lobby.worldSessions[self.world.region.name].roomSessions[self.name].EntityLeavingRoom(apo);
            }
        }
    }
}
