namespace RainMeadow
{
    public class ParticipantInResource
    {
        public OnlinePlayer player;
        public bool everSentLease;
        public PlayerTickReference memberSinceTick;

        public ParticipantInResource(OnlinePlayer player, OnlineResource resource)
        {
            this.player = player;
            memberSinceTick = new PlayerTickReference(resource.supervisor, resource.supervisor.tick);
        }
    }
}