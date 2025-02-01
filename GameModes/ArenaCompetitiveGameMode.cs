using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace RainMeadow
{
    public class ArenaOnlineGameMode : OnlineGameMode
    {

        public ExternalArenaGameMode onlineArenaGameMode;
        public string currentGameMode;
        public Dictionary<ExternalArenaGameMode, string> registeredGameModes;

        public bool registeredNewGameModes = false;

        public bool isInGame = false;
        public int playerLeftGame = 0;
        public int clientsAreReadiedUp = 0;
        public int currentLevel = 0;
        public int totalLevelCount = 0;
        public bool allPlayersReadyLockLobby = false;
        public bool returnToLobby = false;
        public bool sainot = RainMeadow.rainMeadowOptions.ArenaSAINOT.Value;
        public bool painCatThrows = RainMeadow.rainMeadowOptions.PainCatThrows.Value;
        public bool painCatEgg = RainMeadow.rainMeadowOptions.PainCatEgg.Value;
        public bool painCatLizard = RainMeadow.rainMeadowOptions.PainCatLizard.Value;

        public string paincatName = "";
        public int lizardEvent = 0;



        public Dictionary<string, int> onlineArenaSettingsInterfaceMultiChoice = new Dictionary<string, int>();
        public Dictionary<string, bool> onlineArenaSettingsInterfaceeBool = new Dictionary<string, bool>();
        //public Dictionary<string, int> playersInLobbyChoosingSlugs = new Dictionary<string, int>();
        public Dictionary<string, int> playerResultColors = new Dictionary<string, int>();
        //public List<MeadowPlayerId> playersReadiedUp = new List<MeadowPlayerId>();
        public List<ushort> playersReadiedUp = new List<ushort>();
        public Dictionary<ushort, int> playersInLobbyChoosingSlugs = new Dictionary<ushort, int>();


        public int playerEnteredGame = 0;
        public bool countdownInitiatedHoldFire;

        public ArenaPrepTimer arenaPrepTimer;
        public int setupTime = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
        public int trackSetupTime;


        public int arenaSaintAscendanceTimer = RainMeadow.rainMeadowOptions.ArenaSaintAscendanceTimer.Value;


        public ArenaClientSettings arenaClientSettings;
        public SlugcatCustomization avatarSettings;

        public List<string> playList = new List<string>();

        public List<ushort> arenaSittingOnlineOrder = new List<ushort>();

        public ArenaOnlineGameMode(Lobby lobby) : base(lobby)
        {
            avatarSettings = new SlugcatCustomization() { nickname = OnlineManager.mePlayer.id.name };
            arenaClientSettings = new ArenaClientSettings();
            arenaClientSettings.playingAs = SlugcatStats.Name.White;
            playerResultColors = new Dictionary<string, int>();
            registeredGameModes = new Dictionary<ExternalArenaGameMode, string>();

        }

        public void ResetInvDetails()
        {
            lizardEvent = UnityEngine.Random.Range(0, 100);
            int whichPaincatName = UnityEngine.Random.Range(0, 7);
            switch (whichPaincatName)
            {
                case 1:
                    paincatName = "Paincat";
                    break;
                case 2:
                    paincatName = "Inv";
                    break;
                case 3:
                    paincatName = "Enot";
                    break;
                case 4:
                    paincatName = "Sofanthiel";
                    break;
                case 5:
                    paincatName = "Gorbo";
                    break;
                case 6:
                    paincatName = "???";
                    break;
            }

        }

        public void ResetGameTimer()
        {
            setupTime = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
            trackSetupTime = setupTime;
        }

        public void ResetViolence()
        {
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
        static HashSet<AbstractPhysicalObject.AbstractObjectType> blockList = new()
        {
            AbstractPhysicalObject.AbstractObjectType.BlinkingFlower,
            AbstractPhysicalObject.AbstractObjectType.SporePlant,
            AbstractPhysicalObject.AbstractObjectType.AttachedBee

        };
        public override bool ShouldSyncAPOInWorld(WorldSession ws, AbstractPhysicalObject apo)
        {
            if (blockList.Contains(apo.type))
            {
                return false;
            }
            return true;
        }

        public override bool ShouldSyncAPOInRoom(RoomSession rs, AbstractPhysicalObject apo)
        {
            if (blockList.Contains(apo.type))
            {
                return false;
            }
            return true;
        }

        public override bool ShouldRegisterAPO(OnlineResource resource, AbstractPhysicalObject apo)
        {
            if (blockList.Contains(apo.type))
            {
                return false;
            }
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
            if (item.type == PlacedObject.Type.SporePlant)
            {
                return false;
            }

            return base.AllowedInMode(item) || playerGrabbableItems.Contains(item.type);
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

        public override bool ShouldSpawnFly(FliesWorldAI self, int spawnRoom)
        {
            return onlineArenaGameMode.SpawnBatflies(self, spawnRoom);


        }

    }
}
