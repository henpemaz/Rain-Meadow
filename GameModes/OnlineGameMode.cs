using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    // OnlineGameSession is tightly coupled to a lobby, and the highest ownership level
    public abstract partial class OnlineGameMode
    {
        public Lobby lobby;

        public class OnlineGameModeType : ExtEnum<OnlineGameModeType>
        {
            public OnlineGameModeType(string value, bool register = false) : base(value, register) { }
            public static OnlineGameModeType Meadow = new("Meadow", true);
            public static OnlineGameModeType Story = new("Story", true);
            public static OnlineGameModeType FreeRoam = new("FreeRoam", true);
            public static OnlineGameModeType ArenaCompetitive = new("ArenaCompetitive", true);

            public static Dictionary<OnlineGameModeType, string> descriptions = new()
            {
                { Meadow, "A peaceful mode about exploring around and discovering little secrets, together or on your own." },
                { Story, "Adventure together with friends in the world of Rain World, fight together and die together." },
                { FreeRoam, "Silly around, no creatures." },
                { ArenaCompetitive, "You sweaty bastards." }
            };
        }

        public static Dictionary<OnlineGameModeType, Type> gamemodes = new()
        {
            { OnlineGameModeType.Meadow, typeof(MeadowGameMode) },
            { OnlineGameModeType.Story, typeof(StoryGameMode) },
            { OnlineGameModeType.FreeRoam, typeof(FreeRoamGameMode) },
            { OnlineGameModeType.ArenaCompetitive, typeof(ArenaCompetitiveGameMode) }
        };

        public static OnlineGameMode FromType(OnlineGameModeType onlineGameModeType, Lobby lobby)
        {
            return (OnlineGameMode)Activator.CreateInstance(gamemodes[onlineGameModeType], lobby);
        }

        // todo handle modded ones
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

        public OnlineGameMode(Lobby lobby)
        {
            this.lobby = lobby;
        }

        // todo support jolly or other forms of local co-op
        public OnlineCreature avatar;
        public ClientSettings clientSettings;

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

        public virtual bool ShouldSyncObjectInWorld(WorldSession ws, AbstractPhysicalObject apo)
        {
            return true;
        }

        public virtual bool ShouldSyncObjectInRoom(RoomSession rs, AbstractPhysicalObject apo)
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

        public virtual ProcessManager.ProcessID MenuProcessId()
        {
            return RainMeadow.Ext_ProcessID.LobbyMenu;
        }

        public virtual AbstractCreature SpawnAvatar(RainWorldGame self, WorldCoordinate location)
        {
            return null; // game runs default code
        }

        internal virtual void NewEntity(OnlineEntity oe, OnlineResource inResource)
        {
            
        }

        internal virtual void AddAvatarSettings()
        {
            RainMeadow.Debug("Adding avatar settings!");
            clientSettings = new StoryClientSettings(new OnlineEntity.EntityId(OnlineManager.mePlayer.inLobbyId, OnlineEntity.EntityId.IdType.settings, 0), OnlineManager.mePlayer);
            clientSettings.EnterResource(lobby);
        }

        internal virtual void SetAvatar(OnlineCreature onlineCreature)
        {
            RainMeadow.Debug(onlineCreature);
            this.avatar = onlineCreature;
            this.clientSettings.avatarId = onlineCreature.id;
        }

        internal virtual void ResourceAvailable(OnlineResource onlineResource)
        {
            
        }

        internal virtual void ResourceActive(OnlineResource onlineResource)
        {
            if(onlineResource is Lobby)
            {
                AddAvatarSettings();
            }
        }

        public virtual bool PlayerCanOwnResource(OnlinePlayer from, OnlineResource onlineResource)
        {
            return true;
        }

        public virtual void LobbyReadyCheck() 
        { 
            
        }

        internal virtual void PlayerLeftLobby(OnlinePlayer player)
        {
            
        }

        internal virtual void NewPlayerInLobby(OnlinePlayer player)
        {

        }

        internal virtual void LobbyTick(uint tick)
        {
            
        }

        internal virtual void Customize(Creature creature, OnlineCreature oc)
        {
            if (lobby.playerAvatars.Any(a=>a.Value == oc.id))
            {
                RainMeadow.Debug($"Customizing avatar {creature} for {oc.owner}");
                var settings = lobby.entities.Values.First(em => em.entity is ClientSettings avs && avs.avatarId == oc.id).entity as ClientSettings;

                // this adds the entry in the CWT
                var mcc = RainMeadow.creatureCustomizations.GetValue(creature, (c) => settings.MakeCustomization());

                if(creature is Player player && !oc.isMine)
                {
                    player.controller = new OnlineController(oc, player);
                }

                // todo one day come back to making emote support universal
                //if (oc.TryGetData<MeadowCreatureData>(out var mcd))
                //{
                //    EmoteDisplayer.map.GetValue(creature, (c) => new EmoteDisplayer(creature, oc, mcd, mcc));
                //}
                //else
                //{
                //    RainMeadow.Error("missing mcd?? " + oc);
                //}
            }
        }
    }
}
