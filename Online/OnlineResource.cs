using System;
using System.Collections.Generic;
using static Expedition.ExpeditionProgression;

namespace RainMeadow
{
    // OnlineResources are transferible, subscriptable resources, limited to a resource that others can consume (lobby, world, room)
    // The owner of the resource coordinates states, distributes subresources and solves conflicts
    public abstract class OnlineResource
    {
        public OnlineResource super;
        public OnlinePlayer _owner;
        public OnlinePlayer owner => _owner ?? super?.owner;
        public PlayerEvent pendingRequest;
        public List<SendingTask> sendingTasks;

        public bool isOwner => owner.id == OnlineManager.me;
        public bool isClaimed => _owner != null;

        public virtual void Update()
        {
            foreach (var task in sendingTasks)
            {
                task.Update();
            }
        }

        public virtual void Request()
        {
            if (isOwner)
            {
                _owner = OnlineManager.mePlayer;
                return;
            }
            if (pendingRequest != null) return;
            
            pendingRequest = owner.RequestResource(this);
        }

        public virtual RequestResult Requested(ResourceRequest request)
        {
            if (isOwner)
            {
                if(_owner == null)
                {
                    // Leased to player
                    _owner = request.from;
                    return new RequestResultLeased();
                }
                // Player subscribed to resource
                sendingTasks.Add(new SendingTask(this, request.from));
                return new RequestResultSubscribed();
            }
            // Not mine, can't lease
            return new RequestResultError();
        }

        public virtual void Release()
        {
            if (!isClaimed) return;
            if (pendingRequest != null) return;
            if (_owner.id == OnlineManager.me)
            {
                if (this.sendingTasks.Count > 0)
                {
                    pendingRequest = this.sendingTasks[0].subscriber.TransferResource(this);
                }
                _owner = OnlineManager.mePlayer;
                return;
            }

            owner.ReleaseResource(this);
        }

        // todo handle situations of race?
        // this might be missing some chekcs, I was tired when I wrote it
        public virtual void Released(ReleaseRequest request)
        {
            if (isOwner)
            {
                // Player unsubscribed from resource
                sendingTasks.RemoveAll(t => t.subscriber == request.from);
                return new ReleaseResultUnsubscribed();
            }
            // Not mine, can't release
            return new ReleaseResultError();
        }

        public virtual void Transfered(TransferRequest request)
        {

        }

        public abstract ResourceState SendState(ResourceState lastAckState, long ts);

        public abstract void ReceiveState(ResourceState newState, long ts);
    }
}
