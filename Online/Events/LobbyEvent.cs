namespace RainMeadow
{
    internal class LobbyEvent
    {
        public LobbyEventType type;

        public enum LobbyEventType
        {
            SessionStarted
        }

        public LobbyEvent(LobbyEventType type)
        {
            this.type = type;
        }
    }
}