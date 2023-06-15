namespace RainMeadow
{
    public class EntityMembership : ResourceMembership
    {
        public OnlineEntity entity;

        public EntityMembership(OnlineEntity entity, OnlineResource resource) : base(resource)
        {
            this.entity = entity;
        }
    }
}