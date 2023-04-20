namespace RainMeadow
{
    public class ResourceMembership
    {
        public OnlinePlayer player;
        public bool everSentLease;
        public ResourceMembership(OnlinePlayer player)
        {
            this.player = player;
        }
    }
}