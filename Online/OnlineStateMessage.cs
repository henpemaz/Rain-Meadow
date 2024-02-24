using System;

namespace RainMeadow
{
    // An entry of state queued to a player, notifies source on send/fail
    public class OnlineStateMessage
    {
        public OnlineState state;
        public IStateSource source;
        public bool sentAsDelta;
        public uint tick;
        public uint baseline;
        public RootDeltaState sourceState;


        public OnlineStateMessage(OnlineState state, RootDeltaState sourceState, IStateSource source, bool sentAsDelta, uint tick, uint baseline)
        {
            this.state = state;
            this.source = source;
            this.sentAsDelta = sentAsDelta;
            this.tick = tick;
            this.sourceState = sourceState;
            this.baseline = baseline;
        }

        internal void Failed()
        {
            source.Failed(this);
        }

        internal void Sent()
        {
            source.Sent(this);
        }

        public interface IStateSource // could as well be a base type, used in entityfeed and resourcesubscription only
        {
            void Sent(OnlineStateMessage stateMessage);
            void Failed(OnlineStateMessage stateMessage);
        }
    }
}