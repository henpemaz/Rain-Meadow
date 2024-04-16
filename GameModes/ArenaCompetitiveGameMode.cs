using RainMeadow.GameModes;
using System.Linq;

namespace RainMeadow
{
    public class ArenaCompetitiveGameMode : OnlineGameMode
    {
        // TODO: When owner leaves lobby, they release the resource. This should not happen

        public bool dummyTest = false;

        public ArenaCompetitiveGameMode(Lobby lobby) : base(lobby)
        {
        }

        public ArenaClientSettings arenaClientSettings => clientSettings as ArenaClientSettings;


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
        public override bool PlayerCanOwnResource(OnlinePlayer from, OnlineResource onlineResource)
        {
            if (onlineResource is WorldSession)
            {
                return lobby.owner == from;
            }
            return true;
        }

/*        public override AbstractCreature SpawnAvatar(RainWorldGame game, WorldCoordinate location)
        {

        }*/

        internal override void PlayerLeftLobby(OnlinePlayer player)
        {
            base.PlayerLeftLobby(player);
            if (player == lobby.owner)
            {
                OnlineManager.instance.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
            }
        }


        internal override void AddAvatarSettings()
        {
            RainMeadow.Debug("Adding arena avatar settings!");
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