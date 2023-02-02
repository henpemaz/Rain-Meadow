namespace RainMeadow
{
    public class RoomSession : OnlineResource
    {
        public AbstractRoom absroom;

        public RoomSession(AbstractRoom absroom)
        {
            this.absroom = absroom;
        }
    }
}
