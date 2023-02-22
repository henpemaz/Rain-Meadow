using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public class RoomSession : OnlineResource
    {
        public AbstractRoom absroom;
        public bool abstractOnDeactivate;
        private Dictionary<AbstractWorldEntity, OnlineEntity> entities = new();

        public RoomSession(WorldSession ws, AbstractRoom absroom)
        {
            super = ws;
            this.absroom = absroom;
            deactivateOnRelease = true;
        }

        protected override void ActivateImpl()
        {
            
        }

        protected override void DeactivateImpl()
        {
            if (abstractOnDeactivate)
            {
                absroom.Abstractize();
            }
        }

        protected override void AvailableImpl()
        {
            base.AvailableImpl();
            foreach (var ent in absroom.entities)
            {
                if(ent is AbstractPhysicalObject apo)
                {
                    EntityEnteringRoom(apo, apo.pos);
                }
            }
        }

        protected override void SubscribedImpl(OnlinePlayer player)
        {
            base.SubscribedImpl(player);
            foreach (var ent in entities.Values)
            {
                if (player == ent.owner) continue;
                player.QueueEvent(new NewEntityEvent(this, ent, ent.pos));
            }
        }

        internal override string Identifier()
        {
            return super.Identifier() + absroom.name;
        }

        public override void ReadState(ResourceState newState, ulong ts)
        {
            //throw new System.NotImplementedException();
        }

        protected override ResourceState MakeState(ulong ts)
        {
            return new RoomState(this, ts);
        }

        internal void EntityEnteringRoom(AbstractPhysicalObject entity, WorldCoordinate pos)
        {
            RainMeadow.Debug(this);
            if (!isAvailable) { return; } // throw new InvalidOperationException("not available"); }
            if (!entities.ContainsKey(entity))
            {
                var oe = new OnlineEntity(entity, OnlineManager.mePlayer, entity.ID.number, pos);
                entities.Add(entity, oe);
                NewOnlineEntity(oe, pos);
            }
            else
            {
                RainMeadow.Error("entity was already in room??");
                RainMeadow.Error(entity);
            }
        }

        // A new OnlineEntity was added to the room, notify accordingly
        private void NewOnlineEntity(OnlineEntity oe, WorldCoordinate pos)
        {
            RainMeadow.Debug(this);
            if (!isAvailable) throw new InvalidOperationException("not available");
            if(isOwner) // I am responsible for notifying other players about it
            {
                foreach (var sub in subscriptions)
                {
                    if (sub.player == oe.owner) continue;
                    sub.player.QueueEvent(new NewEntityEvent(this, oe, pos));
                }
            }
            else // I notify the owner about my entity in the room
            {
                if (!oe.owner.isMe) throw new InvalidOperationException("trying to add someone else's entity to the room");
                owner.QueueEvent(new NewEntityEvent(this, oe, pos));
                var feed = new EntityFeed(this, owner, oe);
                this.feeds.Add(feed);
                OnlineManager.AddFeed(feed);
            }
        }

        // I've been notified that a new creature has entered the room
        public void OnNewEntity(NewEntityEvent newEntityEvent)
        {
            // todo more than just creatures
            if(newEntityEvent.owner.isMe) { throw new InvalidOperationException("notified of my own creation"); }
            if(!newEntityEvent.isCreature) throw new NotImplementedException("cant do non-creatures yet");
            CreatureTemplate.Type type = new CreatureTemplate.Type(newEntityEvent.template, false);
            RainMeadow.Debug(type);
            RainMeadow.Debug(newEntityEvent.template);
            if (type.Index == -1)
            {
                throw new InvalidOperationException("invalid template");
            }
            EntityID id = absroom.world.game.GetNewID();
            id.altSeed = newEntityEvent.entityId;
            RainMeadow.Debug(id);
            WorldCoordinate coord = newEntityEvent.pos;
            AbstractCreature creature = new AbstractCreature(absroom.world, StaticWorld.GetCreatureTemplate(type), null, coord, id);
            RainMeadow.Debug(creature);
            if (creature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat) // for some dumb reason it doesn't get a default
            {
                creature.state = new PlayerState(creature, 0, RainMeadow.Ext_SlugcatStatsName.OnlineSessionRemotePlayer, false);
            }
            var oe = new OnlineEntity(creature, OnlineManager.mePlayer, newEntityEvent.entityId, coord);
            entities.Add(creature, oe);
            absroom.AddEntity(creature);
            if(absroom.realizedRoom is Room room && creature.AllowedToExistInRoom(room)) 
            {
                RainMeadow.Debug("spawning creature " + creature);
                if(coord.TileDefined)
                {
                    creature.RealizeInRoom();
                }
                else if(coord.NodeDefined)
                {
                    creature.Realize();
                    absroom.world.game.shortcuts.CreatureEnterFromAbstractRoom(creature.realizedCreature, absroom, coord.abstractNode);
                }
            }
            else
            {
                RainMeadow.Debug("not spawning creature " + creature);
                RainMeadow.Debug($"{absroom.realizedRoom is not null}{(absroom.realizedRoom != null && creature.AllowedToExistInRoom(absroom.realizedRoom))}");
            }
        }

        internal void EntityLeavingRoom(AbstractPhysicalObject entity)
        {
            RainMeadow.Debug(this);
            if (entities.TryGetValue(entity, out var oe))
            {
                entities.Remove(entity);
                if (isAvailable && oe.owner.isMe)
                {
                    EntityLeftRoom(oe); // me entity has left the room, notify
                }
                else
                {
                    entity.Destroy();
                    if(entity.realizedObject is UpdatableAndDeletable uad) uad.Destroy();
                }
            }
        }

        public class OnlineEntity
        {
            // todo more info
            public AbstractPhysicalObject entity;
            public OnlinePlayer owner;
            public int id;
            public WorldCoordinate pos;

            public OnlineEntity(AbstractPhysicalObject entity, OnlinePlayer owner, int id, WorldCoordinate pos)
            {
                this.entity = entity;
                this.owner = owner;
                this.id = id;
                this.pos = pos;
            }
        }

        public class RoomState : ResourceState
        {
            public RoomState(OnlineResource resource, ulong ts) : base(resource, ts)
            {

            }

            public override ResourceStateType stateType => ResourceStateType.RoomState;
        }
    }
}
