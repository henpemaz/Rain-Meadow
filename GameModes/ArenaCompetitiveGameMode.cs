using System.Collections.Generic;

namespace RainMeadow
{
    public class ArenaCompetitiveGameMode : OnlineGameMode
    {
        public bool isInGame = false;
        public int clientWaiting = 0;
        public int clientsAreReadiedUp = 0;
        public bool allPlayersReadyLockLobby = false;
        public bool returnToLobby = false;
        public Dictionary<string, int> onlineArenaSettingsInterfaceMultiChoice = new Dictionary<string, int>();
        public Dictionary<string, bool> onlineArenaSettingsInterfaceeBool = new Dictionary<string, bool>();
        public Dictionary<string, int> playersInLobbyChoosingSlugs = new Dictionary<string, int>();
        public int setupTime = 300;
        public int playerEnteredGame = 0;
        public Dictionary<string, bool> playersReadiedUp = new Dictionary<string, bool>();
        public bool countdownInitiatedHoldFire = true;
        public ArenaPrepTimer arenaPrepTimer;

        public int playerResultColorizizerForMSCAndHighLobbyCount;

        public ArenaClientSettings arenaClientSettings;
        public SlugcatCustomization avatarSettings;

        public List<string> playList = new List<string>();

        public List<ushort> arenaSittingOnlineOrder = new List<ushort>();

        public ArenaCompetitiveGameMode(Lobby lobby) : base(lobby)
        {
            avatarSettings = new SlugcatCustomization() { nickname = OnlineManager.mePlayer.id.name };
            arenaClientSettings = new ArenaClientSettings();
            arenaClientSettings.playingAs = SlugcatStats.Name.White;
        }

        public void ResetGameTimer()
        {
            setupTime = 300;
        }

        public void ResetViolence()
        {
            countdownInitiatedHoldFire = true;
            playerEnteredGame = 0;
        }

        public override bool ShouldLoadCreatures(RainWorldGame game, WorldSession worldSession)
        {
            return false;
        }

        public override ProcessManager.ProcessID MenuProcessId()
        {
            return RainMeadow.Ext_ProcessID.ArenaLobbyMenu;
        }

        public override bool ShouldSyncAPOInWorld(WorldSession ws, AbstractPhysicalObject apo)
        {
            return true;
        }
        public override bool PlayerCanOwnResource(OnlinePlayer from, OnlineResource onlineResource)
        {
            if (onlineResource is WorldSession || onlineResource is RoomSession)
            {
                return lobby.owner == from;
            }
            return true;
        }


        public override void PlayerLeftLobby(OnlinePlayer player)
        {
            base.PlayerLeftLobby(player);
            if (player == lobby.owner)
            {
                OnlineManager.instance.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
            }
        }

        public override bool AllowedInMode(PlacedObject item)
        {
            return base.AllowedInMode(item) || OnlineGameModeHelpers.PlayerGrabbableItems.Contains(item.type);
        }

        public override bool ShouldSpawnRoomItems(RainWorldGame game, RoomSession roomSession)
        {
            return roomSession.owner == null || roomSession.isOwner;
        }

        public override void ResourceAvailable(OnlineResource onlineResource)
        {
            base.ResourceAvailable(onlineResource);

            if (onlineResource is Lobby lobby)
            {
                lobby.AddData(new ArenaLobbyData());
            }
        }

        public override void AddClientData()
        {
            clientSettings.AddData(arenaClientSettings);
        }

        public override void ConfigureAvatar(OnlineCreature onlineCreature)
        {
            onlineCreature.AddData(avatarSettings);
        }

        public override void Customize(Creature creature, OnlineCreature oc)
        {
            if (oc.TryGetData<SlugcatCustomization>(out var data))
            {
                RainMeadow.Debug(oc);
                RainMeadow.creatureCustomizations.GetValue(creature, (c) => data);
            }
        }
    }
}
