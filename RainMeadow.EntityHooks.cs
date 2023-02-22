using System;

namespace RainMeadow
{
    partial class RainMeadow
    {
        private void EntityHooks()
        {
            On.AbstractRoom.AddEntity += AbstractRoom_AddEntity;
            On.AbstractRoom.RemoveEntity_AbstractWorldEntity += AbstractRoom_RemoveEntity;

            On.ShortcutHandler.ShortCutVessel.SetAllPositions += ShortCutVessel_SetAllPositions; // aka vessel enter room
            On.AbstractCreature.AllowedToExistInRoom += AbstractCreature_AllowedToExistInRoom;
        }

        // dunno if required
        private bool AbstractCreature_AllowedToExistInRoom(On.AbstractCreature.orig_AllowedToExistInRoom orig, AbstractCreature self, Room room)
        {
            if (self.Room.world.game.session is OnlineGameSession os)
            {
                if (!OnlineManager.lobby.worldSessions[self.Room.world.region.name].roomSessions[self.Room.name].isAvailable)
                {
                    RainMeadow.Debug("preventing creature movement to unavailable room");
                    return false;
                }
            }
            return orig(self, room);
        }

        // called when vessel arrives in room, much better timing
        private void ShortCutVessel_SetAllPositions(On.ShortcutHandler.ShortCutVessel.orig_SetAllPositions orig, ShortcutHandler.ShortCutVessel self, RWCustom.IntVector2 pos)
        {
            orig(self, pos);
            // todo switch to "allconnectedobjects"
            if (self.room.world.game.session is OnlineGameSession os && self.creature?.abstractCreature is AbstractPhysicalObject apo)
            {
                var wc = new WorldCoordinate() { room = self.room.index, abstractNode = self.entranceNode };
                wc.WashTileData();
                OnlineManager.lobby.worldSessions[self.room.world.region.name].roomSessions[self.room.name].EntityEnteringRoom(apo, wc);
            }
        }

        // ideally not the main entry-point, things will have been added already
        // creature.move doesn't set the new pos until after it has moved, that's the issue
        private void AbstractRoom_AddEntity(On.AbstractRoom.orig_AddEntity orig, AbstractRoom self, AbstractWorldEntity ent)
        {
            orig(self, ent);
            if (self.world.game.session is OnlineGameSession os && !ent.slatedForDeletion && ent is AbstractPhysicalObject apo && apo.pos.room == self.index)
            {
                OnlineManager.lobby.worldSessions[self.world.region.name].roomSessions[self.name].EntityEnteringRoom(apo, apo.pos); // could this be that this is the old pos???
            }
        }

        private void AbstractRoom_RemoveEntity(On.AbstractRoom.orig_RemoveEntity_AbstractWorldEntity orig, AbstractRoom self, AbstractWorldEntity entity)
        {
            orig(self, entity);
            if (self.world.game.session is OnlineGameSession os && !entity.slatedForDeletion && entity is AbstractPhysicalObject apo)
            {
                OnlineManager.lobby.worldSessions[self.world.region.name].roomSessions[self.name].EntityLeavingRoom(apo);
            }
        }
    }
}
