namespace RainMeadow
{
    public class ResourceMembership
    {
        public OnlineResource resource;
        public TickReference memberSinceTick;

        public ResourceMembership(OnlineResource resource)
        {
            this.resource = resource;
            memberSinceTick = new TickReference(resource.supervisor, resource.supervisor.tick);
        }
    }
}