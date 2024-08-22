using System;

namespace RainMeadow
{
    public abstract class OnlineEvent
    {
        public abstract EventTypeId eventType { get; } // serialized externally
        public OnlinePlayer from;// not serialized
        public OnlinePlayer to;// not serialized
        public ushort eventId;

        public override string ToString()
        {
            return $"{eventId}:{eventType}";
        }

        public virtual void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref eventId);
        }

        public abstract void Process(); // I've been received and I do something

        public virtual void Abort() // I was not acknowledged and the other guy left, what do, can be run on local or on remote
        {
            RainMeadow.Error($"Aborted {this}");
        }

        //public abstract void Acknoledged();

        public enum EventTypeId : byte // will we hit 255 of these I wonder
        {
            None,
            GenericResultOk,
            GenericResultError,
            GenericResultFail,
            NewObjectEvent,
            NewCreatureEvent,
            NewMeadowPersonaSettingsEvent,
            RPCEvent,
        }

        // there used to be a lot more stuff in here until I made everything into RPCs and state
        public static OnlineEvent NewFromType(EventTypeId eventTypeId)
        {
            OnlineEvent e = null;
            switch (eventTypeId)
            {
                case EventTypeId.None: // fault detection
                    break;
                case EventTypeId.GenericResultOk:
                    e = new GenericResult.Ok();
                    break;
                case EventTypeId.GenericResultFail:
                    e = new GenericResult.Fail();
                    break;
                case EventTypeId.GenericResultError:
                    e = new GenericResult.Error();
                    break;
                case EventTypeId.RPCEvent:
                    e = new RPCEvent();
                    break;
            }
            if (e is null) throw new InvalidOperationException("invalid event type: " + eventTypeId);
            return e;
        }
    }
}