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
        }

        private void ShortCutVessel_SetAllPositions(On.ShortcutHandler.ShortCutVessel.orig_SetAllPositions orig, ShortcutHandler.ShortCutVessel self, RWCustom.IntVector2 pos)
        {
            orig(self, pos);
            if (self.room.world.game.session is OnlineGameSession os && self.creature?.abstractCreature is AbstractPhysicalObject apo)
            {
                var wc = new WorldCoordinate() { room = self.room.index, abstractNode = self.entranceNode };
                wc.WashTileData();
                OnlineManager.lobby.worldSessions[self.room.world.region.name].roomSessions[self.room.name].EntityEnteringRoom(apo, wc);
            }
        }

        private void AbstractRoom_AddEntity(On.AbstractRoom.orig_AddEntity orig, AbstractRoom self, AbstractWorldEntity ent)
        {
            orig(self, ent);
            if (self.world?.game?.session is OnlineGameSession os && !ent.slatedForDeletion && ent is AbstractPhysicalObject apo)
            {
                OnlineManager.lobby.worldSessions[self.world.region.name].roomSessions[self.name].EntityEnteringRoom(apo, apo.pos);
            }
        }

        private void AbstractRoom_RemoveEntity(On.AbstractRoom.orig_RemoveEntity_AbstractWorldEntity orig, AbstractRoom self, AbstractWorldEntity entity)
        {
            orig(self, entity);
            if (self.world?.game?.session is OnlineGameSession os && !entity.slatedForDeletion && entity is AbstractPhysicalObject apo)
            {
                OnlineManager.lobby.worldSessions[self.world.region.name].roomSessions[self.name].EntityLeavingRoom(apo);
            }
        }
    }
}
