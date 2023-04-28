namespace RainMeadow
{
    public class ResourceMembership
    {
        public OnlinePlayer player;
        public bool everSentLease;
        public PlayerTickReference memberSinceTick;

        public ResourceMembership(OnlinePlayer player, OnlineResource resource)
        {
            this.player = player;
            memberSinceTick = new PlayerTickReference(resource.supervisor, resource.supervisor.tick);
        }
    }
}