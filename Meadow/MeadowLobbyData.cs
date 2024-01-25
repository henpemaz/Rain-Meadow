namespace RainMeadow
{
    internal class MeadowLobbyData : OnlineResource.ResourceData
    {
        public Lobby lobby;
        public ushort[] itemsPerRegion;

        public MeadowLobbyData(Lobby lobby)
        {
            this.lobby = lobby;
        }

        internal override OnlineResource.ResourceDataState MakeState(OnlineResource inResource)
        {
            return new MeadowLobbyState(this);
        }

        internal class MeadowLobbyState : OnlineResource.ResourceDataState
        {
            [OnlineField]
            bool placeholder;
            public MeadowLobbyState() { }
            public MeadowLobbyState(MeadowLobbyData meadowLobbyData)
            {

            }

            internal override void ReadTo(OnlineResource onlineResource)
            {

            }
        }
    }
}