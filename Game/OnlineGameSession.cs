namespace RainMeadow
{
    // OnlineGameSession is tightly coupled to a lobby, and the highest ownership level
    public partial class OnlineGameSession : StoryGameSession
    {
        public OnlineGameSession(RainWorldGame game) : base(RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer, game)
        {
            RainMeadow.Debug("OnlineGameSession created");
            OnlineManager.lobby.session = this;
        }

        public void FilterItems(Room room)
        {
            foreach (var item in room.roomSettings.placedObjects)
            {
                if(item.active && !AllowedInMode(item))
                {
                    item.active = false;
                }
            }
        }

        private bool AllowedInMode(PlacedObject item)
        {
            return cosmeticItems.Contains(item.type);

        }

        public bool ShouldSpawnRoomItems(RainWorldGame game)
        {
            return false;
        }

        public bool ShouldLoadCreatures(RainWorldGame game, WorldSession worldSession)
        {
            return false;
            if (worldSession is null || !worldSession.isAvailable)
            {
                return false;
            }
            return worldSession.isOwner;
        }

        public bool ShouldSyncObjectInWorld(WorldSession ws, AbstractPhysicalObject apo)
        {
            return apo is AbstractCreature;
        }

        public bool ShouldSyncObjectInRoom(RoomSession rs, AbstractPhysicalObject apo)
        {
            return true;
        }
    }
}
