namespace RainMeadow
{
    public class PlayerMemebership : ResourceMembership
    {
        public OnlinePlayer player;

        public PlayerMemebership(OnlinePlayer player, OnlineResource resource) : base(resource)
        {
            this.player = player;
        }
    }
}