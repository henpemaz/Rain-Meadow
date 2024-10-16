namespace RainMeadow
{
    public class CustomGameMode : OnlineGameMode
    {
        public SlugcatCustomization avatarSettings;

        public CustomGameMode(Lobby lobby) : base(lobby)
        {
            avatarSettings = new SlugcatCustomization();
        }

        internal override void ConfigureAvatar(OnlineCreature onlineCreature)
        {
            onlineCreature.AddData(avatarSettings);
        }

        internal override void Customize(Creature creature, OnlineCreature oc)
        {
            RainMeadow.Debug(oc);
            if (oc.GetData<SlugcatCustomization>() is AvatarData data)
            {
                // this adds the entry in the CWT
                RainMeadow.creatureCustomizations.GetValue(creature, (c) => data);
            }
        }

        public override ProcessManager.ProcessID MenuProcessId()
        {
            return RainMeadow.Ext_ProcessID.CustomLobbyMenu;
        }
    }
}