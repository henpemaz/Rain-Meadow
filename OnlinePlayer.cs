using Steamworks;

namespace RainMeadow
{
    public class OnlinePlayer
    {
        public Steamworks.CSteamID id;

        public OnlinePlayer(CSteamID id)
        {
            this.id = id;
        }
    }
}
