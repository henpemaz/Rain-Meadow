namespace RainMeadow
{
    public class OnlineEntity
    {
        // todo more info
        public AbstractPhysicalObject entity;
        public OnlinePlayer owner;
        public int id;
        public WorldCoordinate pos;
        internal RoomSession lastInRoom;
        internal int ticksSinceSeen;

        public OnlineEntity(AbstractPhysicalObject entity, OnlinePlayer owner, int id, WorldCoordinate pos, RoomSession lastInRoom)
        {
            this.entity = entity;
            this.owner = owner;
            this.id = id;
            this.pos = pos;
            this.lastInRoom = lastInRoom;
        }
    }
}
