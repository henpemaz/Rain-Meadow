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
            RainMeadow.Debug("Added persona settings!");
            var def = new MeadowPersonaSettingsDefinition(new OnlineEntity.EntityId(OnlineManager.mePlayer.inLobbyId, PersonaSettingsEntity.personaID), OnlineManager.mePlayer, false);
            personaSettings = new MeadowPersonaSettings(def);
            personaSettings.EnterResource(lobby);
        }

        public override ProcessManager.ProcessID MenuProcessId()
        {
            return RainMeadow.Ext_ProcessID.MeadowMenu;
        }

        public override AbstractCreature SpawnPersona(RainWorldGame game, WorldCoordinate location)
        {
            var settings = (personaSettings as MeadowPersonaSettings);
            var skinData = MeadowProgression.skinData[settings.skin];
            var abstractCreature = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate(skinData.creatureType), null, location, new EntityID(-1, 0));
            if(skinData.creatureType == CreatureTemplate.Type.Slugcat)
            {
                abstractCreature.state = new PlayerState(abstractCreature, 0, skinData.statsName, false);
                game.session.AddPlayer(abstractCreature);
            }
            game.world.GetAbstractRoom(abstractCreature.pos.room).AddEntity(abstractCreature);
            return abstractCreature;

            //if (false)//personaType == PersonaType.Slugcat)
            //{
            //    var abstractCreature = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate("Slugcat"), null, location, new EntityID(-1, 0));
            //    abstractCreature.state = new PlayerState(abstractCreature, 0, game.GetStorySession.saveState.saveStateNumber, false);
            //    game.world.GetAbstractRoom(abstractCreature.pos.room).AddEntity(abstractCreature);
            //    game.session.AddPlayer(abstractCreature);
            //    return abstractCreature;

            //}
            //else if (true)//personaType == PersonaType.Squidcicada)
            //{
            //    var abstractCreature = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.CicadaA), null, location, new EntityID(-1, 0));
            //    game.world.GetAbstractRoom(abstractCreature.pos.room).AddEntity(abstractCreature);
            //    return abstractCreature;
            //}
        }
    }
}
