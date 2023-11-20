namespace RainMeadow
{
    public partial class RoomSession
    {
        [RPCMethod]
        public void AbstractRoomFirstTimeRealized()
        {
            absroom.firstTimeRealized = false;
        }
    }
}
