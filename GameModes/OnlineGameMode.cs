using System;
using System.Collections.Generic;

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

        public PersonaSettingsEntity personaSettings;

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

        public virtual AbstractCreature SpawnPersona(RainWorldGame self, WorldCoordinate location)
        {
            return null;
        }
    }
}
