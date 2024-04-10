using RainMeadow.GameModes;

namespace RainMeadow
{
    public class ArenaCompetitiveGameMode : OnlineGameMode
    {

        public bool dummyTest = false;

        public ArenaCompetitiveGameMode(Lobby lobby) : base(lobby)
        {
        }

        public override bool ShouldLoadCreatures(RainWorldGame game, WorldSession worldSession)
        {
            return false;
        }

        public override ProcessManager.ProcessID MenuProcessId()
        {
            return RainMeadow.Ext_ProcessID.ArenaLobbyMenu;
        }

        public override bool ShouldSyncObjectInWorld(WorldSession ws, AbstractPhysicalObject apo)
        {
            return true;
        }

        internal override void AddAvatarSettings()
        {
            RainMeadow.Debug("Adding arena avatar settings!");
            // Some sort of registration is missing
            clientSettings = new ArenaClientSettings(
                new ArenaClientSettings.Definition(
                    new OnlineEntity.EntityId(OnlineManager.mePlayer.inLobbyId, OnlineEntity.EntityId.IdType.settings, 0)
                    , OnlineManager.mePlayer));
            clientSettings.EnterResource(lobby);
        }

        internal override void ResourceAvailable(OnlineResource onlineResource)
        {
            base.ResourceAvailable(onlineResource);

            if (onlineResource is Lobby lobby)
            {
                lobby.AddData<ArenaLobbyData>(true);
            }


        }

    }
}