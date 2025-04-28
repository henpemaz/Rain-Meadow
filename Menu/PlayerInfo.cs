namespace RainMeadow
{
    public class PlayerInfo
    {
        public delegate void OpenProfileLinkDelegate();
        public OpenProfileLinkDelegate openProfile;
        public string name;

        public PlayerInfo(OpenProfileLinkDelegate openProfile, string name)
        {
            this.openProfile = openProfile;
            this.name = name;
        }
    }
}
