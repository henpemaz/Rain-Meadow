namespace RainMeadow
{
    public class EntityFeed
    {
        public RoomSession roomSession;
        public OnlineEntity entity;

        public EntityFeed(RoomSession roomSession, OnlineEntity oe)
        {
            this.roomSession = roomSession;
            this.entity = oe;
        }
    }
}