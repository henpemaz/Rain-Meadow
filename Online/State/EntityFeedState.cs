namespace RainMeadow
{
    public class EntityFeedState : OnlineState
    {
        public EntityState entityState;
        public OnlineResource inResource;

        public EntityFeedState() { }
        public EntityFeedState(EntityState entityState, OnlineResource inResource) : base()
        {
            this.entityState = entityState;
            this.inResource = inResource;
        }
    }
}