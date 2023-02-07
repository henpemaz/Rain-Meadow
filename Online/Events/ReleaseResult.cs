namespace RainMeadow
{
    public class ReleaseResult
    {

        public class Unsubscribed : ReleaseResult
        {
        }

        public class Error : ReleaseResult
        {
        }

        internal class Released : ReleaseResult
        {
        }
    }
}