using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    // OnlineGameSession is tightly coupled to a lobby, and the highest ownership level
    public abstract partial class OnlineGameMode
    {
        public class OnlineGameModeType : ExtEnum<OnlineGameModeType>
        {
            public OnlineGameModeType(string value, bool register = false) : base(value, register) { }
            public static OnlineGameModeType Meadow = new("Meadow", true);
            public static OnlineGameModeType Story = new("Story", true);
            public static OnlineGameModeType ArenaCompetitive = new("ArenaCompetitive", true);
            public static OnlineGameModeType Custom = new("Custom", true);

            public static Dictionary<OnlineGameModeType, string> descriptions = new()
            {
                { Meadow, "A peaceful mode about exploring around and discovering little secrets, together or on your own." },
                { Story, "Adventure together with friends in the world of Rain World, fight together and die together." },
                { ArenaCompetitive, "You sweaty bastards." },
                { Custom, "Your own way." },
            };
        }

        public static Dictionary<OnlineGameModeType, Type> gamemodes = new()
        {
            { OnlineGameModeType.Meadow, typeof(MeadowGameMode) },
            { OnlineGameModeType.Story, typeof(StoryGameMode) },
            { OnlineGameModeType.ArenaCompetitive, typeof(ArenaCompetitiveGameMode) },
            { OnlineGameModeType.Custom, typeof(CustomGameMode) },
        };

        public static OnlineGameMode FromType(OnlineGameModeType onlineGameModeType, Lobby lobby)
        {
            return (OnlineGameMode)Activator.CreateInstance(gamemodes[onlineGameModeType], lobby);
        }

        public static void RegisterType(OnlineGameModeType onlineGameModeType, Type type, string description)
        {
            if (!typeof(OnlineGameMode).IsAssignableFrom(type) || type.GetConstructor(new[] { typeof(Lobby) }) == null) throw new ArgumentException("Needs to be OnlineGameMode with a (Lobby) ctor");
            gamemodes[onlineGameModeType] = type;
            OnlineGameModeType.descriptions[onlineGameModeType] = description;
        }

        public static void InitializeBuiltinTypes()
        {
            _ = gamemodes;
        }

        public Lobby lobby;
        public List<OnlineCreature> avatars = new();
        public ClientSettings clientSettings;

        public OnlineGameMode(Lobby lobby)
        {
            this.lobby = lobby;
        }

        public virtual void FilterItems(Room room)
        {
            foreach (var item in room.roomSettings.placedObjects)
            {
                if (item.active && !AllowedInMode(item))
                {
                    item.active = false;
                }
            }
        }

        public virtual bool AllowedInMode(PlacedObject item)
        {
            return OnlineGameModeHelpers.cosmeticItems.Contains(item.type);
        }

        public virtual bool ShouldSpawnRoomItems(RainWorldGame game, RoomSession roomSession)
        {
            return false;
        }

        public virtual bool ShouldLoadCreatures(RainWorldGame game, WorldSession worldSession)
        {
            return false;
        }

        public virtual bool ShouldRegisterAPO(OnlineResource resource, AbstractPhysicalObject apo)
        {
            return true;
        }

        public virtual bool ShouldSyncAPOInWorld(WorldSession ws, AbstractPhysicalObject apo)
        {
            return true;
        }

        public virtual bool ShouldSyncAPOInRoom(RoomSession rs, AbstractPhysicalObject apo)
        {
            return true;
        }

        public virtual bool ShouldSpawnFly(FliesWorldAI self, int spawnRoom)
        {
            return false;
        }

        public virtual SlugcatStats.Name GetStorySessionPlayer(RainWorldGame self)
        {
            return RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer;
        }

        public virtual SlugcatStats.Name LoadWorldAs(RainWorldGame game)
        {
            return RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer;
        }

        public abstract ProcessManager.ProcessID MenuProcessId();

        internal virtual void AddClientData()
        {

        }

        public virtual AbstractCreature SpawnAvatar(RainWorldGame self, WorldCoordinate location)
        {
            return null; // game runs default code
        }

        internal virtual void NewEntity(OnlineEntity oe, OnlineResource inResource)
        {
            RainMeadow.Debug(oe);
            if (RainMeadow.sSpawningAvatar && oe is OnlineCreature onlineCreature)
            {
                RainMeadow.Debug("Registring avatar: " + onlineCreature);
                this.avatars.Add(onlineCreature);
                ConfigureAvatar(onlineCreature);
            }
        }

        internal abstract void ConfigureAvatar(OnlineCreature onlineCreature);

        internal virtual void ResourceAvailable(OnlineResource onlineResource)
        {
            RainMeadow.Debug(onlineResource);
        }

        internal virtual void ResourceActive(OnlineResource onlineResource)
        {
            RainMeadow.Debug(onlineResource);
            if (onlineResource is Lobby)
            {
                this.clientSettings = new ClientSettings(new OnlineEntity.EntityId(OnlineManager.mePlayer.inLobbyId, OnlineEntity.EntityId.IdType.settings, 0), OnlineManager.mePlayer);
                AddClientData();
                clientSettings.EnterResource(lobby);
                OnlineManager.instance.manager.RequestMainProcessSwitch(MenuProcessId());
            }
        }

        public virtual bool PlayerCanOwnResource(OnlinePlayer from, OnlineResource onlineResource)
        {
            return true;
        }

        internal virtual void PlayerLeftLobby(OnlinePlayer player)
        {

        }

        internal virtual void NewPlayerInLobby(OnlinePlayer player)
        {

        }

        internal virtual void LobbyTick(uint tick)
        {
            clientSettings.avatars = avatars.Select(a => a.id).ToList();
        }

        internal abstract void Customize(Creature creature, OnlineCreature oc);

        internal virtual void PreGameStart()
        {

        }

        internal virtual void PostGameStart()
        {
            clientSettings.inGame = true;
            clientSettings.avatars = avatars.Select(a => a.id).ToList();
        }

        internal virtual void GameShutDown(RainWorldGame game)
        {
            clientSettings.inGame = false;
            avatars.Clear();
            clientSettings.avatars.Clear();
        }
    }
}
