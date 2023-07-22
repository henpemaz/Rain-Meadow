using System;

namespace RainMeadow
{
    public abstract class OnlineEvent
    {
        public abstract EventTypeId eventType { get; } // serialized externally
        public OnlinePlayer from;// not serialized
        public OnlinePlayer to;// not serialized
        public ushort eventId;
        public TickReference dependsOnTick; // not serialized but universally supported, serialize if used for your event type

        public override string ToString()
        {
            return $"{eventId}:{eventType}";
        }

        public virtual long EstimatedSize { get => sizeof(ushort); }

        public virtual void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref eventId);
        }

        public virtual bool CanBeProcessed() // I've been received but I might be early
        {
            return dependsOnTick == null || dependsOnTick.ChecksOut();
        }

        public virtual bool ShouldBeDiscarded() // I've been received but I might be stale/unprocessable
        {
            return dependsOnTick != null && dependsOnTick.Invalid();
        }

        public abstract void Process(); // I've been received and I do something

        public virtual void Abort() // I was not acknowledged and the other guy left, what do
        {
            RainMeadow.Error($"Aborted {this}");
        }

        //public abstract void Acknoledged();

        public enum EventTypeId : byte // will we hit 255 of these I wonder
        {
            None,
            ResourceRequest,
            ResourceRelease,
            ResourceTransfer,
            RequestResultLeased,
            RequestResultSubscribed,
            RequestResultError,
            ReleaseResultReleased,
            ReleaseResultUnsubscribed,
            ReleaseResultError,
            TransferResultOk,
            TransferResultError,
            LeaseChange,
            GenericResultOk,
            GenericResultError,
            CreatureEventViolence,
            CreatureEventSuckedIntoShortCut,
            RegisterNewEntityRequest,
            NewObjectEvent,
            NewCreatureEvent,
            EntityJoinRequest,
            EntityJoinedEvent,
            EntityLeaveRequest,
            EntityLeftEvent,
            EntityTransfererRequest,
            EntityTransferedEvent,
            EntityRequest,
            EntityRelease,
            LobbyLeaseChange,
        }

        public static OnlineEvent NewFromType(EventTypeId eventTypeId)
        {
            OnlineEvent e = null;
            switch (eventTypeId)
            {
                case EventTypeId.None: // fault detection
                    break;
                case EventTypeId.ResourceRequest:
                    e = new ResourceRequest();
                    break;
                case EventTypeId.ResourceRelease:
                    e = new ResourceRelease();
                    break;
                case EventTypeId.ResourceTransfer:
                    e = new ResourceTransfer();
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
                case EventTypeId.CreatureEventViolence:
                    e = new CreatureEvent.Violence();
                    break;
                case EventTypeId.CreatureEventSuckedIntoShortCut:
                    e = new CreatureEvent.SuckedIntoShortCut();
                    break;
                case EventTypeId.GenericResultOk:
                    e = new GenericResult.Ok();
                    break;
                case EventTypeId.GenericResultError:
                    e = new GenericResult.Error();
                    break;
                case EventTypeId.RegisterNewEntityRequest:
                    e = new RegisterNewEntityRequest();
                    break;
                case EventTypeId.NewObjectEvent:
                    e = new NewObjectEvent();
                    break;
                case EventTypeId.NewCreatureEvent:
                    e = new NewCreatureEvent();
                    break;
                case EventTypeId.EntityJoinRequest:
                    e = new EntityJoinRequest();
                    break;
                case EventTypeId.EntityJoinedEvent:
                    e = new EntityJoinedEvent();
                    break;
                case EventTypeId.EntityLeaveRequest:
                    e = new EntityLeaveRequest();
                    break;
                case EventTypeId.EntityLeftEvent:
                    e = new EntityLeftEvent();
                    break;
                case EventTypeId.EntityTransfererRequest:
                    e = new EntityTransferRequest();
                    break;
                case EventTypeId.EntityTransferedEvent:
                    e = new EntityTransferedEvent();
                    break;
                case EventTypeId.EntityRequest:
                    e = new EntityRequest();
                    break;
                case EventTypeId.EntityRelease:
                    e = new EntityReleaseEvent();
                    break;
            }
            if (e is null) throw new InvalidOperationException("invalid event type: " + eventTypeId);
            return e;
        }
    }
}