using System;

namespace RainMeadow
{
    // An entry of state queued to a player, notifies source on send/fail
    public class OnlineStateMessage
    {
        public OnlineState state;
        public IStateSource source;

        public OnlineStateMessage(OnlineState state, IStateSource source) {
            this.state = state;
            this.source = source;
        }

        internal void Failed()
        {
            source.Failed(state);
        }

        internal void Sent()
        {
            source.Sent(state);
        }

        public interface IStateSource // could as well be a base type, used in entityfeed and resourcesubscription only
        {
            void Sent(OnlineState state);
            void Failed(OnlineState state);
        }
    }
}