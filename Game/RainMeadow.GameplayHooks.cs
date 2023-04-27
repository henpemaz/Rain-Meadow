using System.Linq;

namespace RainMeadow
{
    partial class RainMeadow
    {
        public void GameplayHooks()
        {
            On.Player.ctor += Player_ctor;
            On.ShelterDoor.Close += ShelterDoorOnClose;
        }

        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if(OnlineManager.lobby != null)
            {
                // remote player
                if (OnlineEntity.map.TryGetValue(self.abstractPhysicalObject, out var ent) && self.playerState.slugcatCharacter == Ext_SlugcatStatsName.OnlineSessionRemotePlayer)
                {
                    self.controller = new OnlineController(ent, self);
                }
            }
        }
        
        private void ShelterDoorOnClose(On.ShelterDoor.orig_Close orig, ShelterDoor self)
        {
            if (OnlineManager.lobby == null)
            {
                orig(self);
                return;
            }

            var scug = self.room.game.Players.First();
            var realizedScug = (Player)scug.realizedCreature;
            if (realizedScug == null || !self.room.PlayersInRoom.Contains(realizedScug)) return;
            if (!realizedScug.readyForWin) return;
            orig(self);
        }
    }
}
