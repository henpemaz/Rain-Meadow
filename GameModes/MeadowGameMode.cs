namespace RainMeadow
{
    public class MeadowGameMode : OnlineGameMode
    {
        public MeadowGameMode(Lobby lobby) : base(lobby)
        {
            lobby.OnLobbyAvailable += Lobby_OnLobbyAvailable;
        }

        private void Lobby_OnLobbyAvailable()
        {
            RainMeadow.Debug("Adding persona settings!");
            var def = new MeadowPersonaSettingsDefinition(new OnlineEntity.EntityId(OnlineManager.mePlayer.inLobbyId, AvatarSettingsEntity.personaID), OnlineManager.mePlayer, false);
            avatarSettings = new MeadowAvatarSettings(def);
            avatarSettings.EnterResource(lobby);
        }

        public override ProcessManager.ProcessID MenuProcessId()
        {
            return RainMeadow.Ext_ProcessID.MeadowMenu;
        }

        public override AbstractCreature SpawnPersona(RainWorldGame game, WorldCoordinate location)
        {
            var settings = (avatarSettings as MeadowAvatarSettings);
            var skinData = MeadowProgression.skinData[settings.skin];
            var abstractCreature = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate(skinData.creatureType), null, location, new EntityID(-1, 0));
            if(skinData.creatureType == CreatureTemplate.Type.Slugcat)
            {
                abstractCreature.state = new PlayerState(abstractCreature, 0, skinData.statsName, false);
                game.session.AddPlayer(abstractCreature);
            }
            game.world.GetAbstractRoom(abstractCreature.pos.room).AddEntity(abstractCreature);
            return abstractCreature;
        }
    }
}
