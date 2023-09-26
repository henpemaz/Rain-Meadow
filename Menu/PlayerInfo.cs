using Steamworks;

namespace RainMeadow
{
    public class PlayerInfo
    {
        public CSteamID id;
        public string name;

        public PlayerInfo(CSteamID id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }
}
