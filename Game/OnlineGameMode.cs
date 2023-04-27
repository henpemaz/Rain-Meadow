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

        public OnlineGameMode(Lobby lobby)
        {
            this.lobby = lobby;
        }

        internal static OnlineGameMode FromType(OnlineGameModeType onlineGameModeType, Lobby lobby)
        {
            if (onlineGameModeType == OnlineGameModeType.Meadow)
            {
                return new MeadowGameMode(lobby);
            }
            if (onlineGameModeType == OnlineGameModeType.Story)
            {
                return new StoryGameMode(lobby);
            }
            if (onlineGameModeType == OnlineGameModeType.FreeRoam)
            {
                return new FreeRoamGameMode(lobby);
            }
            if (onlineGameModeType == OnlineGameModeType.ArenaCompetitive)
            {
                return new ArenaCompetitiveGameMode(lobby);
            }
            return null;
        }

        public virtual void FilterItems(Room room)
        {
            foreach (var item in room.roomSettings.placedObjects)
            {
                if(item.active && !AllowedInMode(item))
                {
                    item.active = false;
                }
            }
        }

        public virtual bool AllowedInMode(PlacedObject item)
        {
            return OnlineGameModeHelpers.cosmeticItems.Contains(item.type);
        }

        public virtual bool ShouldSpawnRoomItems(RainWorldGame game)
        {
            return false;
        }

        public virtual bool ShouldLoadCreatures(RainWorldGame game, WorldSession worldSession)
        {
            return false;
            if (worldSession is null || !worldSession.isAvailable)
            {
                return false;
            }
            return worldSession.isOwner;
        }

        public virtual bool ShouldSyncObjectInWorld(WorldSession ws, AbstractPhysicalObject apo)
        {
            return apo is AbstractCreature;
        }

        public virtual bool ShouldSyncObjectInRoom(RoomSession rs, AbstractPhysicalObject apo)
        {
            return true;
        }

        internal virtual bool ShouldSpawnFly(FliesWorldAI self, int spawnRoom)
        {
            return false;
        }

        internal virtual SlugcatStats.Name GetStorySessionPlayer(RainWorldGame self)
        {
            return RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer;
        }
    }
}
