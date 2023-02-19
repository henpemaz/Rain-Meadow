using System;
using System.IO;
using static RoomInfo;

namespace RainMeadow
{
    public abstract class PlayerEvent
    {
        public abstract EventTypeId eventType { get; } // serialized externally
        public OnlinePlayer from;// not serialized
        public OnlinePlayer to;// not serialized
        public ulong eventId;

        public virtual long EstimatedSize { get => sizeof(ulong); }

        public virtual void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref eventId);
        }

        internal abstract void Process();

        public enum EventTypeId : byte
        {
            None,
            ResourceRequest,
            ReleaseRequest,
            TransferRequest,
            ReleaseResultReleased,
            ReleaseResultUnsubscribed,
            ReleaseResultError,
            RequestResultLeased,
            RequestResultSubscribed,
            RequestResultError,
            TransferResultError,
            TransferResultOk,
            NewOwnerEvent,
        }

        internal static PlayerEvent NewFromType(EventTypeId eventTypeId)
        {
            PlayerEvent e = null;
            switch (eventTypeId)
            {
                case EventTypeId.None:
                    break;
                case EventTypeId.ResourceRequest:
                    e = new ResourceRequest(null);
                    break;
                case EventTypeId.ReleaseRequest:
                    e = new ReleaseRequest(null, null);
                    break;
                case EventTypeId.TransferRequest:
                    e = new TransferRequest(null, null);
                    break;
                case EventTypeId.ReleaseResultReleased:
                    e = new ReleaseResult.Released(null);
                    break;
                case EventTypeId.ReleaseResultUnsubscribed:
                    e = new ReleaseResult.Unsubscribed(null);
                    break;
                case EventTypeId.ReleaseResultError:
                    e = new ReleaseResult.Error(null);
                    break;
                case EventTypeId.RequestResultLeased:
                    e = new RequestResult.Leased(null);
                    break;
                case EventTypeId.RequestResultSubscribed:
                    e = new RequestResult.Subscribed(null);
                    break;
                case EventTypeId.RequestResultError:
                    e = new RequestResult.Error(null);
                    break;
                case EventTypeId.TransferResultError:
                    e = new TransferResult.Error(null);
                    break;
                case EventTypeId.TransferResultOk:
                    e = new TransferResult.Ok(null);
                    break;
                case EventTypeId.NewOwnerEvent:
                    e = new NewOwnerEvent(null, null);
                    break;
            }
            return e;
        }
    }
}