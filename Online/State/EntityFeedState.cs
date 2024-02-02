namespace RainMeadow
{
    public class EntityFeedState : OnlineState
    {
        [OnlineField(polymorphic = true)]
        public OnlineEntity.EntityState entityState;
        [OnlineField]
        public OnlineResource inResource;

        public EntityFeedState() { }
        public EntityFeedState(OnlineEntity.EntityState entityState, OnlineResource inResource) : base()
        {
            this.entityState = entityState;
            this.inResource = inResource;
        }
    }
}