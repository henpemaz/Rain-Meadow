using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Playables;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        protected abstract World World { get; }
        public List<OnlineEntity> entities;
        private List<EntityResourceEvent> incomingEntities; // entities coming from onwer but that I can't process yet

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
                foreach (var player in participants)
                {
                    if (player.isMe || player == oe.owner) continue;
                    player.QueueEvent(new NewEntityEvent(this, oe));
                }
            }
            else if (oe.owner.isMe) // I notify the owner about my entity in the room
            {
                RainMeadow.Debug("notifying owner of entity joining");
                // todo should this be handshaked? owner might change
                owner.QueueEvent(new NewEntityEvent(this, oe));
                OnlineManager.AddFeed(this, oe);
            }
            else
            {
                RainMeadow.Debug("externally controlled entity joining, not notifying anyone");
            }
        }

        // I've been notified that a new creature has entered, and I must recreate the equivalent in my game
        internal void OnNewEntity(NewEntityEvent newEntityEvent)
        {
            RainMeadow.Debug(this);
            if (!isAvailable) { throw new InvalidOperationException("not available"); }
            if (!isActive)
            {
                RainMeadow.Debug("queueing for later");
                this.incomingEntities.Add(newEntityEvent);
                return;
            }
            if (newEntityEvent.owner.isMe) { throw new InvalidOperationException("notified of my own creation"); }
            OnlineEntity oe = OnlineEntity.CreateOrReuseEntity(newEntityEvent, this.World);
            EntityEnteredResource(oe);
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
                foreach (var player in participants)
                {
                    if (player.isMe || player == oe.owner) continue;
                    player.QueueEvent(new EntityLeftEvent(this, oe));
                }
            }
            else if (oe.owner.isMe) // I notify the owner about my entity in the room
            {
                RainMeadow.Debug("notifying owner of entity leaving");
                // todo should this be handshaked? owner might change
                owner.QueueEvent(new EntityLeftEvent(this, oe));
                OnlineManager.RemoveFeed(this, oe);
            }
            else
            {
                RainMeadow.Debug("external entity leaving, not notifying anyone");
            }
        }

        // I've been notified that an entity has left
        internal void OnEntityLeft(EntityLeftEvent entityLeftEvent)
        {
            RainMeadow.Debug(this);
            if (!isAvailable) { throw new InvalidOperationException("not available"); }
            if (!isActive)
            {
                RainMeadow.Debug("queueing for later");
                this.incomingEntities.Add(entityLeftEvent);
                return;
            }
            OnlineEntity oe = entities.FirstOrDefault(e => e.id == entityLeftEvent.entityId);
            if (oe.owner.isMe) { throw new InvalidOperationException("notified of my own creation"); }
            EntityLeftResource(oe);
        }

        // Assign a new owner to this entity, if I own or supervise it, I must notify accordingly
        internal void EntityNewOwner(OnlineEntity oe, OnlinePlayer newOwner)
        {
            RainMeadow.Debug(this);
            if (!isAvailable) { throw new InvalidOperationException("not available"); }
            if (oe.owner == newOwner) throw new InvalidOperationException("reasigned to same owner");
            if (oe.highestResource != this) throw new InvalidOperationException("asigned owner in wrong resource");
            var wasOwner = oe.owner; // will be null in transfer-as-super situations

            if (isOwner) // I am responsible for notifying other players about it
            {
                RainMeadow.Debug("notifying others in resource of entity transfer");
                foreach (var player in participants)
                {
                    if (player.isMe || player == wasOwner) continue; // reeeeally though? I'm sure this wont bite me in the butt at some point
                    player.QueueEvent(new EntityNewOwnerEvent(this, oe.id, newOwner));
                }
            }
            else if (wasOwner.isMe) // I notify the owner about my entity ive donated to someone else
            {
                RainMeadow.Debug("notifying resource owner of entity transfer");
                owner.QueueEvent(new EntityNewOwnerEvent(this, oe.id, newOwner));
            }

            oe.NewOwner(newOwner); // handles the entity actually changing owner
                                          // also calls to SubresourcesUnloaded which might call Release
                                          // which is why I moved it down here :3
        }

        // I'm notified of a new owner for an entity in this resource
        internal void OnEntityNewOwner(EntityNewOwnerEvent entityNewOwner)
        {
            RainMeadow.Debug(this);
            if (!isAvailable) { throw new InvalidOperationException("not available"); }
            if (!isActive)
            {
                RainMeadow.Debug("queueing for later");
                this.incomingEntities.Add(entityNewOwner);
                return;
            }
            OnlineEntity oe = entities.FirstOrDefault(e => e.id == entityNewOwner.entityId);
            if (oe != null)
            {
                EntityNewOwner(oe, entityNewOwner.newOwner);
            }
            else
            {
                RainMeadow.Error("entity mentioned could not be found");
            }
        }
    }
}
