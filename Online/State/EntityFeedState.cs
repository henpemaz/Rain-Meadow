namespace RainMeadow
{
    public class EntityFeedState : OnlineState
    {
        [OnlineField(polymorphic = true)]
        public EntityState entityState;
        [OnlineResourceRefField]
        public OnlineResource inResource;

        public EntityFeedState() { }
        public EntityFeedState(EntityState entityState, OnlineResource inResource) : base()
        {
            this.entityState = entityState;
            this.inResource = inResource;
        }
    }
}