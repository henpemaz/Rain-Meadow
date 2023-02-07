namespace RainMeadow
{
    public class RequestResult : PlayerEvent
    {
        public class Subscribed : RequestResult
        {
        }

        public class Leased : RequestResult
        {
        }

        public class Error : RequestResult
        {
        }
    }
}