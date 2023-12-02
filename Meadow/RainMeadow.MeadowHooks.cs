using System;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        private void MeadowHooks()
        {
            MeadowCustomization.EnableCicada();
            MeadowCustomization.EnableLizard();

            On.RoomCamera.Update += RoomCamera_Update;
        }

        private void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera self)
        {
            if(OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode meadowGameMode)
            {
                if(self.hud == null && self.followAbstractCreature?.realizedObject is Creature owner)
                {
                    if(owner != meadowGameMode.avatar.realizedCreature) { RainMeadow.Error($"Camera owner != avatar {owner} {meadowGameMode.avatar}"); }

                    self.hud = new HUD.HUD(new FContainer[]
                    {
                        self.ReturnFContainer("HUD"),
                        self.ReturnFContainer("HUD2")
                    }, self.room.game.rainWorld, owner is Player player? player : MeadowCustomization.creatureController.TryGetValue(owner.abstractCreature, out var controller) ? controller : throw new InvalidProgrammerException("Not player nor controlled creature"));

                    MeadowCustomization.InitMeadowHud(self);
                }
            }
            orig(self);
        }
    }
}
