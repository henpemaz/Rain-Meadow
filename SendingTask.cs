using System;

namespace RainMeadow
{
    public class SendingTask
    {
        public OnlineResource onlineResource;
        public OnlinePlayer subscriber;

        public SendingTask(OnlineResource onlineResource, OnlinePlayer subscriber)
        {
            this.onlineResource = onlineResource;
            this.subscriber = subscriber;
        }

        public void Update()
        {
            throw new NotImplementedException();
        }
    }
}