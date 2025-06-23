using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
            public static OnlineGameModeType Arena = new("Arena", true);

            public Dictionary<string, bool> boolRemixSettings;
            public Dictionary<string, float> floatRemixSettings;
            public Dictionary<string, int> intRemixSettings;


            public static Dictionary<OnlineGameModeType, string> descriptions = new()
            {
                { Meadow, "A peaceful mode about exploring around and discovering little secrets, together or on<LINE>your own." },
                { Story, "Adventure together with friends in the world of Rain World, fight together and die<LINE>together." },
                { Arena, "Fight against unforgiving creatues and foes where only the strong survive." },
            };
        }

        public virtual List<string> nonGameplayRemixSettings { get; set; } = new() { "cfgSpeedrunTimer", "cfgHideRainMeterNoThreat", "cfgLoadingScreenTips", "cfgExtraTutorials", "cfgClearerDeathGradients", "cfgShowUnderwaterShortcuts", "cfgBreathTimeVisualIndicator", "cfgCreatureSense", "cfgTickTock", "cfgFastMapReveal", "cfgThreatMusicPulse", "cfgExtraLizardSounds", "cfgQuieterGates", "cfgDisableScreenShake", "cfgHunterBatflyAutograb", "cfgNoMoreTinnitus", "cfgWallpounce", "cfgOldTongue" };

        public static (Dictionary<string, bool> hostBoolSettings, Dictionary<string, float> hostFloatSettings, Dictionary<string, int> hostIntSettings) GetHostRemixSettings(OnlineGameMode mode)
        {
            Dictionary<string, bool> configurableBools = new();
            Dictionary<string, float> configurableFloats = new();
            Dictionary<string, int> configurableInts = new();

            if (ModManager.MMF && mode.nonGameplayRemixSettings != null)
            {  
                Type type = typeof(MoreSlugcats.MMF);

                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
                var sortedFields = fields.OrderBy(f => f.Name);

                foreach (FieldInfo field in sortedFields)
                {
                    if (mode.nonGameplayRemixSettings.Contains(field.Name)) continue;
                    var reflectedValue = field.GetValue(null);
                    if (reflectedValue is Configurable<bool> boolOption)
                    {
                        configurableBools.Add(field.Name, boolOption._typedValue);
                    }

                    if (reflectedValue is Configurable<float> floatOption)
                    {
                        configurableFloats.Add(field.Name, floatOption._typedValue);
                    }

                    if (reflectedValue is Configurable<int> intOption)
                    {
                        configurableInts.Add(field.Name, intOption._typedValue);
                    }
                }
                RainMeadow.Debug(configurableBools);
                RainMeadow.Debug(configurableInts);
                RainMeadow.Debug(configurableFloats);
            }

            return (configurableBools, configurableFloats, configurableInts);
        }

        public virtual bool PlayersCanStack => true;
        public virtual bool PlayersCanHandhold => true;

        internal static void SetClientRemixSettings(Dictionary<string, bool> hostBoolRemixSettings, Dictionary<string, float> hostFloatRemixSettings, Dictionary<string, int> hostIntRemixSettings)
        {
            Type type = typeof(MoreSlugcats.MMF);

            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            var sortedFields = fields.OrderBy(f => f.Name);

            foreach (FieldInfo field in sortedFields)
            {
                var reflectedValue = field.GetValue(null);
                if (reflectedValue is Configurable<bool> boolOption)
                {
                    for (int i = 0; i < hostBoolRemixSettings.Count; i++)
                    {
                        if (field.Name == hostBoolRemixSettings.Keys.ElementAt(i) && boolOption._typedValue != hostBoolRemixSettings.Values.ElementAt(i))
                        {
                            RainMeadow.Debug($"Remix Key: {field.Name} with value {boolOption._typedValue} does not match host's, setting to {hostBoolRemixSettings.Values.ElementAt(i)}");
                            boolOption._typedValue = hostBoolRemixSettings.Values.ElementAt(i);
                        }
                    }
                }

                if (reflectedValue is Configurable<float> floatOption)
                {
                    for (int i = 0; i < hostFloatRemixSettings.Count; i++)
                    {
                        if (field.Name == hostFloatRemixSettings.Keys.ElementAt(i) && floatOption._typedValue != hostFloatRemixSettings.Values.ElementAt(i))
                        {
                            RainMeadow.Debug($"Remix Key: {field.Name} with value {floatOption._typedValue} does not match host's, setting to {hostFloatRemixSettings.Values.ElementAt(i)}");
                            floatOption._typedValue = hostFloatRemixSettings.Values.ElementAt(i);
                        }
                    }
                }

                if (reflectedValue is Configurable<int> intOption)
                {
                    for (int i = 0; i < hostIntRemixSettings.Count; i++)
                    {

                        if (field.Name == hostIntRemixSettings.Keys.ElementAt(i) && intOption._typedValue != hostIntRemixSettings.Values.ElementAt(i))
                        {
                            RainMeadow.Debug($"Remix Key: {field.Name} with value {intOption._typedValue} does not match host's, setting to {hostIntRemixSettings.Values.ElementAt(i)}");
                            intOption._typedValue = hostIntRemixSettings.Values.ElementAt(i);
                        }
                    }
                }
            }
        }

        public static Dictionary<OnlineGameModeType, Type> gamemodes = new()
        {
            { OnlineGameModeType.Meadow, typeof(MeadowGameMode) },
            { OnlineGameModeType.Story, typeof(StoryGameMode) },
            { OnlineGameModeType.Arena, typeof(ArenaOnlineGameMode) },
        };

        public static OnlineGameMode FromType(OnlineGameModeType onlineGameModeType, Lobby lobby)
        {
            return (OnlineGameMode)Activator.CreateInstance(gamemodes[onlineGameModeType], lobby);
        }

        public static void RegisterType(OnlineGameModeType onlineGameModeType, Type type, string description)
        {
            if (!typeof(OnlineGameMode).IsAssignableFrom(type) || type.GetConstructor(new[] { typeof(Lobby) }) == null) throw new ArgumentException("Needs to be OnlineGameMode with a (Lobby) ctor");
            gamemodes[onlineGameModeType] = type;
            OnlineGameModeType.descriptions[onlineGameModeType] = Utils.Translate(description);
        }

        public static void InitializeBuiltinTypes()
        {
            _ = gamemodes;
        }

        public Lobby lobby;
        public List<OnlineCreature> avatars = new();
        public ClientSettings clientSettings;
        public List<string> mutedPlayers;


        public OnlineGameMode(Lobby lobby)
        {
            this.lobby = lobby;
            this.mutedPlayers = new List<string>();
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
            return cosmeticItems.Contains(item.type);
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

        public virtual void AddClientData()
        {

        }

        public virtual AbstractCreature SpawnAvatar(RainWorldGame self, WorldCoordinate location)
        {
            return null; // game runs default code
        }

        public virtual void NewEntity(OnlineEntity oe, OnlineResource inResource)
        {
            RainMeadow.Debug(oe);
            if (RainMeadow.sSpawningAvatar && oe is OnlineCreature onlineCreature)
            {
                RainMeadow.Debug("Registring avatar: " + onlineCreature);
                this.avatars.Add(onlineCreature);
                ConfigureAvatar(onlineCreature);
            }
        }

        internal virtual void EntityEnteredResource(OnlineEntity oe, OnlineResource inResource)
        {
            
        }

        internal virtual void EntityLeftResource(OnlineEntity oe, OnlineResource inResource)
        {
            
        }

        public abstract void ConfigureAvatar(OnlineCreature onlineCreature);

        public virtual void ResourceAvailable(OnlineResource onlineResource)
        {
            RainMeadow.Debug(onlineResource);
        }

        public virtual void ResourceActive(OnlineResource onlineResource)
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

        public virtual void PlayerLeftLobby(OnlinePlayer player)
        {

        }

        public virtual void NewPlayerInLobby(OnlinePlayer player)
        {

        }

        public virtual void LobbyTick(uint tick)
        {
            clientSettings.avatars = avatars.Select(a => a.id).ToList();
        }

        public abstract void Customize(Creature creature, OnlineCreature oc);

        public virtual void PreGameStart()
        {
            OnlineManager.mePlayer.isActuallySpectating = false;
        }

        public virtual void PostGameStart(RainWorldGame self)
        {
            clientSettings.inGame = true;
            clientSettings.avatars = avatars.Select(a => a.id).ToList();
        }

        public virtual void GameShutDown(RainWorldGame game)
        {
            clientSettings.inGame = false;
            avatars.Clear();
            clientSettings.avatars.Clear();
        }

        public virtual Menu.PauseMenu CustomPauseMenu(ProcessManager manager, RainWorldGame game)
        {
            return null;
        }
    }
}
