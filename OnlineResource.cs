using System;
using System.Collections.Generic;

namespace RainMeadow
{
    // OnlineResources are transferible sessions, limited to a resource that others can consume (lobby, world, room)
    // The owner of the resource coordinates states, distributes subresources and solves conflicts
    public abstract class OnlineResource
    {
        public OnlineResource super;
        public OnlinePlayer _owner;
        public OnlinePlayer owner => _owner ?? super.owner;
        public ResourceRequest pendingRequest;
        public List<SendingTask> sendingTasks;

        public bool isOwner => owner.id == OnlineManager.me;

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
                    _owner = request.from;
                    return new RequestResultLeased();
                }
                Subscribed(request.from);
                return new RequestResultSubscribed();
            }
            return new RequestResultError();
        }

        public virtual void Subscribed(OnlinePlayer subscriber)
        {
            sendingTasks.Add(new SendingTask(this, subscriber));
        }

        public virtual void Unsubscribed(OnlinePlayer unsubscriber)
        {
            sendingTasks.RemoveAll(t=>t.subscriber == unsubscriber);
        }

        public abstract State SendState(State lastAckState, long ts);

        public abstract void ReceiveState(State newState, long ts);

        public class State
        {
            public byte same;
            public virtual void Write() { }
            public virtual void Read() { }
        }
    }

    public class OnlineSession : OnlineResource
    {
        public Lobby lobby;
        public Dictionary<Region, WorldSession> worldSessions;

        public OnlineSession(Lobby lobby, OnlinePlayer owner)
        {
            this.lobby = lobby;
            this._owner = owner;

            Request(); // Everyone auto-joins the session
        }

        class OnlineSessionState : State
        {

        }

        public override State SendState(State lastAckState, long ts)
        {
            throw new NotImplementedException();
        }
    }

    public class WorldSession : OnlineResource
    {
        public Region region;
        public List<RoomSession> rooms;

        public WorldSession(Region region)
        {
            this.region = region;
        }
    }

    public class RoomSession : OnlineResource
    {
        public AbstractRoom absroom;

        public RoomSession(AbstractRoom absroom)
        {
            this.absroom = absroom;
        }
    }
}
