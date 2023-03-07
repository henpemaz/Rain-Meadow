using System;
using System.IO;

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

        public enum EventTypeId : byte // will we hit 255 of these I wonder
        {
            None,
            ResourceRequest,
            ReleaseRequest,
            TransferRequest,
            RequestResultLeased,
            RequestResultSubscribed,
            RequestResultError,
            ReleaseResultReleased,
            ReleaseResultUnsubscribed,
            ReleaseResultError,
            TransferResultOk,
            TransferResultError,
            LeaseChange,
            NewEntityEvent,
            EntityLeftEvent,
            EntityNewOwnerEvent,
            EntityRequest,
            EntityRequestResultOk,
            EntityRequestResultError,
            EntityReleaseEvent,
            EntityReleaseResultOk,
            EntityReleaseResultError,
        }

        internal static PlayerEvent NewFromType(EventTypeId eventTypeId)
        {
            PlayerEvent e = null;
            switch (eventTypeId)
            {
                case EventTypeId.None:
                    break;
                case EventTypeId.ResourceRequest:
                    e = new ResourceRequest();
                    break;
                case EventTypeId.ReleaseRequest:
                    e = new ReleaseRequest();
                    break;
                case EventTypeId.TransferRequest:
                    e = new TransferRequest();
                    break;
                case EventTypeId.ReleaseResultReleased:
                    e = new ReleaseResult.Released();
                    break;
                case EventTypeId.ReleaseResultUnsubscribed:
                    e = new ReleaseResult.Unsubscribed();
                    break;
                case EventTypeId.ReleaseResultError:
                    e = new ReleaseResult.Error();
                    break;
                case EventTypeId.RequestResultLeased:
                    e = new RequestResult.Leased();
                    break;
                case EventTypeId.RequestResultSubscribed:
                    e = new RequestResult.Subscribed();
                    break;
                case EventTypeId.RequestResultError:
                    e = new RequestResult.Error();
                    break;
                case EventTypeId.TransferResultError:
                    e = new TransferResult.Error();
                    break;
                case EventTypeId.TransferResultOk:
                    e = new TransferResult.Ok();
                    break;
                case EventTypeId.LeaseChange:
                    e = new LeaseChangeEvent();
                    break;
                case EventTypeId.NewEntityEvent:
                    e = new NewEntityEvent();
                    break;
                case EventTypeId.EntityLeftEvent:
                    e = new EntityLeftEvent();
                    break;
                case EventTypeId.EntityNewOwnerEvent:
                    e = new EntityNewOwnerEvent();
                    break;
                case EventTypeId.EntityRequest:
                    e = new EntityRequest();
                    break;
                case EventTypeId.EntityRequestResultOk:
                    e = new EntityRequestResult.Ok();
                    break;
                case EventTypeId.EntityRequestResultError:
                    e = new EntityRequestResult.Error();
                    break;
                case EventTypeId.EntityReleaseEvent:
                    e = new EntityReleaseEvent();
                    break;
                case EventTypeId.EntityReleaseResultOk:
                    e = new EntityReleaseResult.Ok();
                    break;
                case EventTypeId.EntityReleaseResultError:
                    e = new EntityReleaseResult.Error();
                    break;
            }
            if (e is null) throw new InvalidOperationException("invalid event type: " + eventTypeId);
            return e;
        }
    }
}