namespace RainMeadow
{
    public class StoryGameMode : OnlineGameMode
    {
        public StoryGameMode(Lobby lobby) : base(lobby)
        {
        }

        public override bool ShouldLoadCreatures(RainWorldGame game, WorldSession worldSession)
        {
            if (worldSession is null || !worldSession.isAvailable)
            {
                return false;
            }
            return worldSession.isOwner;
        }

        public override bool ShouldSpawnRoomItems(RainWorldGame game, RoomSession roomSession)
        {
            return roomSession.owner == null || roomSession.owner.isMe;
            // todo if two join at once, this first check is faulty
        }

        public override bool ShouldSyncObjectInWorld(WorldSession ws, AbstractPhysicalObject apo)
        {
            return apo is AbstractCreature;
        }

        public override SlugcatStats.Name LoadWorldAs(RainWorldGame game)
        {
            return SlugcatStats.Name.White;
        }
    }
}
