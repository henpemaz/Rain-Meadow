using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        protected abstract World World { get; }
        public List<OnlineEntity> entities;
        private List<EntityResourceEvent> incomingEntities; // entities coming from onwer but that I can't process yet

        // A new OnlineEntity was added, notify accordingly
        public virtual void EntityEnteredResource(OnlineEntity oe)
        {
            RainMeadow.Debug($"{this} - {oe}");
            if (!isAvailable) throw new InvalidOperationException("not available");
            if (entities.Contains(oe)) throw new InvalidOperationException("already in entities");
            entities.Add(oe);
            if (isOwner) // I am responsible for notifying other players about it
            {
                RainMeadow.Debug("notifying others of entity joining");
                foreach (var member in memberships.Values)
                {
                    if (member.player.isMe || member.player == oe.owner) continue;
                    member.player.QueueEvent(new NewEntityEvent(this, oe, member.memberSinceTick));
                }
            }
            else if (oe.owner.isMe) // I notify the owner about my entity in the room
            {
                RainMeadow.Debug("notifying owner of entity joining");
                // todo should this be handshaked? owner might change
                owner.QueueEvent(new NewEntityEvent(this, oe, memberships[PlayersManager.mePlayer].memberSinceTick));
                OnlineManager.AddFeed(this, oe);
            }
            else
            {
                RainMeadow.Debug("externally controlled entity joining, not notifying anyone");
            }
        }

        // I've been notified that a new creature has entered, and I must recreate the equivalent in my game
        public void OnNewEntity(NewEntityEvent newEntityEvent)
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

        public virtual void EntityLeftResource(OnlineEntity oe)
        {
            RainMeadow.Debug($"{this} - {oe}");
            if (!isAvailable) { RainMeadow.Debug("not available, skipping"); return; }
            if (!entities.Contains(oe)) throw new InvalidOperationException("not in entities");
            entities.Remove(oe);
            if (isOwner) // I am responsible for notifying other players about it
            {
                RainMeadow.Debug("notifying others of entity leaving");
                foreach (var member in memberships.Values)
                {
                    if (member.player.isMe || member.player == oe.owner) continue;
                    member.player.QueueEvent(new EntityLeftEvent(this, oe, member.memberSinceTick));
                }
            }
            else if (oe.owner.isMe) // I notify the owner about my entity in the room
            {
                RainMeadow.Debug("notifying owner of entity leaving");
                // todo should this be handshaked? owner might change
                owner.QueueEvent(new EntityLeftEvent(this, oe, memberships[PlayersManager.mePlayer].memberSinceTick));
                OnlineManager.RemoveFeed(this, oe);
            }
            else
            {
                RainMeadow.Debug("external entity leaving, not notifying anyone");
            }
        }

        // I've been notified that an entity has left
        public void OnEntityLeft(EntityLeftEvent entityLeftEvent)
        {
            RainMeadow.Debug(this);
            if (!isAvailable) { RainMeadow.Debug("not available, skipping"); return; }
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
        public void EntityNewOwner(OnlineEntity oe, OnlinePlayer newOwner, bool notifyPreviousOwner = false)
        {
            RainMeadow.Debug($"{this} - {oe} - {newOwner}");
            if (!isAvailable) { throw new InvalidOperationException("not available"); }
            if (oe.owner == newOwner) throw new InvalidOperationException("reasigned to same owner");
            if (oe.highestResource != this) throw new InvalidOperationException("asigned owner in wrong resource");
            var wasOwner = oe.owner; // will be null in transfer-as-super situations

            if (isOwner) // I am responsible for notifying other players about it
            {
                RainMeadow.Debug("notifying others in resource of entity transfer");
                foreach (var member in memberships.Values)
                {
                    if (member.player.isMe || (member.player == wasOwner && !notifyPreviousOwner)) continue; // on transfers, we need to notify previous owner
                    member.player.QueueEvent(new EntityNewOwnerEvent(this, oe.id, newOwner, member.memberSinceTick));
                }
            }
            else if (wasOwner.isMe) // I notify the owner about my entity ive donated to someone else
            {
                RainMeadow.Debug("notifying resource owner of entity transfer");
                owner.QueueEvent(new EntityNewOwnerEvent(this, oe.id, newOwner, memberships[PlayersManager.mePlayer].memberSinceTick));
            }

            oe.NewOwner(newOwner); // handles the entity actually changing owner
                                          // also calls to SubresourcesUnloaded which might call Release
                                          // which is why I moved it down here :3
        }

        // I'm notified of a new owner for an entity in this resource
        public void OnEntityNewOwner(EntityNewOwnerEvent entityNewOwner)
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
