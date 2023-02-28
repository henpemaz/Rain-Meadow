using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Playables;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        protected abstract World World { get; }
        protected List<OnlineEntity> entities = new();

        // I've been notified that a new creature has entered the room, and I must recreate the equivalent in my world
        internal void OnNewEntity(NewEntityEvent newEntityEvent)
        {
            RainMeadow.Debug(this);
            if (newEntityEvent.owner.isMe) { throw new InvalidOperationException("notified of my own creation"); }
            // todo more than just creatures
            OnlineEntity oe = OnlineEntity.CreateOrReuseEntity(newEntityEvent, this.World);
            EntityEnteredResource(oe);
        }

        // I've been notified that an entity has left
        internal void OnEntityLeft(EntityLeftEvent entityLeftEvent)
        {
            RainMeadow.Debug(this);
            if (entityLeftEvent.owner.isMe) { throw new InvalidOperationException("notified of my own creation"); }
            OnlineEntity oe = entities.FirstOrDefault(e => e.owner == entityLeftEvent.owner && e.id == entityLeftEvent.entityId);
            EntityLeftResource(oe);
        }

        // A new OnlineEntity was added, notify accordingly
        protected virtual void EntityEnteredResource(OnlineEntity oe)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug(oe);
            if (!isAvailable) throw new InvalidOperationException("not available");
            if (entities.Contains(oe)) throw new InvalidOperationException("already in entities");
            entities.Add(oe);
            RainMeadow.Debug("ADDED " + oe);
            if (isOwner) // I am responsible for notifying other players about it
            {
                RainMeadow.Debug("notifying others of entity joining");
                foreach (var sub in subscriptions)
                {
                    if (sub.player == oe.owner) continue;
                    sub.player.QueueEvent(new NewEntityEvent(this, oe));
                }
            }
            else if (oe.owner.isMe) // I notify the owner about my entity in the room
            {
                RainMeadow.Debug("notifying owner of entity joining");
                owner.QueueEvent(new NewEntityEvent(this, oe));
                OnlineManager.AddFeed(this, oe);
            }
            else
            {
                RainMeadow.Debug("externally controlled entity joining, not notifying anyone");
            }
        }

        protected virtual void EntityLeftResource(OnlineEntity oe)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug(oe);
            if (!isAvailable) throw new InvalidOperationException("not available");
            if (!entities.Contains(oe)) throw new InvalidOperationException("not in entities");
            entities.Remove(oe);
            RainMeadow.Debug("REMOVED " + oe);
            if (isOwner) // I am responsible for notifying other players about it
            {
                RainMeadow.Debug("notifying others of entity leaving");
                foreach (var sub in subscriptions)
                {
                    if (sub.player == oe.owner) continue;
                    sub.player.QueueEvent(new EntityLeftEvent(this, oe));
                }
            }
            else if (oe.owner.isMe) // I notify the owner about my entity in the room
            {
                RainMeadow.Debug("notifying owner of entity leaving");
                owner.QueueEvent(new EntityLeftEvent(this, oe));
                OnlineManager.RemoveFeed(this, oe);
            }
            else
            {
                RainMeadow.Debug("external entity leaving, not notifying anyone");
            }
        }
    }
}
