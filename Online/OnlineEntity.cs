using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Expedition.ExpeditionProgression;

namespace RainMeadow
{
    // Welcome to polymorphism hell
    public partial class OnlineEntity
    {
        internal static ConditionalWeakTable<AbstractPhysicalObject, OnlineEntity> map = new();

        // todo abstract this into "entity" and "game entity"
        public AbstractPhysicalObject entity;
        public OnlinePlayer owner;
        public int id;
        public int seed;

        public WorldCoordinate enterPos;
        public WorldSession world;
        public RoomSession room;
        public OnlineResource lowestResource => (room as OnlineResource ?? world);
        public OnlineResource highestResource => (world as OnlineResource ?? room);

        public bool realized;
        public bool isTransferable = true; // todo make own personas not transferable

        public bool isPending => pendingRequest != null;
        public PlayerEvent pendingRequest;

        public OnlineEntity(AbstractPhysicalObject entity, OnlinePlayer owner, int id, int seed, WorldCoordinate pos)
        {
            this.entity = entity;
            this.owner = owner;
            this.id = id;
            this.seed = seed;
            this.enterPos = pos;
        }

        public override string ToString()
        {
            return $"{entity}:{id} from {owner}";
        }

        public static OnlineEntity CreateOrReuseEntity(NewEntityEvent newEntityEvent, World world)
        {
            OnlineEntity oe = null;
            
            if (newEntityEvent.owner.recentEntities.TryGetValue(newEntityEvent.entityId, out oe))
            {
                RainMeadow.Debug("reusing existing entity " + oe);
                var creature = oe.entity as AbstractCreature;
                creature.slatedForDeletion = false;
                if (creature.realizedObject is PhysicalObject po) po.slatedForDeletetion = false;

                oe.enterPos = newEntityEvent.initialPos;
                oe.entity.pos = oe.enterPos;
            }
            else
            {
                RainMeadow.Debug("spawning new entity");
                // it is very tempting to switch to the generic tostring/fromstring from the savesystem, BUT
                // it would be almost impossible to sanitize input and who knows what someone could do through that
                if (!newEntityEvent.isCreature) throw new NotImplementedException("cant do non-creatures yet");
                CreatureTemplate.Type type = new CreatureTemplate.Type(newEntityEvent.template, false);
                if (type.Index == -1)
                {
                    RainMeadow.Debug(type);
                    RainMeadow.Debug(newEntityEvent.template);
                    throw new InvalidOperationException("invalid template");
                }
                EntityID id = world.game.GetNewID();
                id.altSeed = newEntityEvent.seed;
                RainMeadow.Debug(id);
                RainMeadow.Debug(newEntityEvent.initialPos);
                WorldSession.registeringRemoteEntity = true;
                var creature = new AbstractCreature(world, StaticWorld.GetCreatureTemplate(type), null, newEntityEvent.initialPos, id);
                WorldSession.registeringRemoteEntity = false;
                RainMeadow.Debug(creature);
                if (creature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat) // for some dumb reason it doesn't get a default
                {
                    creature.state = new PlayerState(creature, 0, RainMeadow.Ext_SlugcatStatsName.OnlineSessionRemotePlayer, false);
                }
                oe = new OnlineEntity(creature, newEntityEvent.owner, newEntityEvent.entityId, newEntityEvent.seed, newEntityEvent.initialPos);
                OnlineEntity.map.Add(creature, oe);
                newEntityEvent.owner.recentEntities.Add(newEntityEvent.entityId, oe);
            }
            
            return oe;
        }

        public void NewOwner(OnlinePlayer newOwner, int newId)
        {
            var wasOwner = owner;
            var wasId = id;

            owner = newOwner;
            id = newId;

            wasOwner.recentEntities.Remove(wasId);
            newOwner.recentEntities.Add(newId, this);

            if (wasOwner.isMe)
            {
                // this screams "iterate" but at the same time... it's just these two for RW, maybe next game
                if (world != null)
                {
                    OnlineManager.RemoveFeed(world, this);
                }
                if (room != null)
                {
                    OnlineManager.RemoveFeed(room, this);
                }
            }
            if (newOwner.isMe) // we only start feeding after we get the broadcast of new owner.
                               // Maybe this should be in ResolveRequest instead? but then there's no guarantee the resource owner will have the new ID
            {
                if (world != null)
                {
                    OnlineManager.AddFeed(world, this);
                }
                if (room != null)
                {
                    OnlineManager.AddFeed(room, this);
                }
            }
        }

        // I request, to someone else
        internal void Request()
        {
            RainMeadow.Debug(this);
            if (owner.isMe) throw new InvalidProgrammerException("this entity is already mine");
            if (!isTransferable) throw new InvalidProgrammerException("cannot be transfered");
            if (isPending) throw new InvalidProgrammerException("this entity has a pending request");
            if (!lowestResource.isAvailable) throw new InvalidProgrammerException("in unavailable resource");

            owner.RequestEntity(this, this.entity.ID.number);
        }

        // I've been requested and I'll pass the entity on
        internal void Requested(EntityRequest request)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Requested by : " + request.from.name);
            if (isTransferable && this.owner.isMe && !isPending)
            {
                request.from.QueueEvent(new EntityRequestResult.Ok(request)); // your request was well received, now please be patient while I transfer it
                this.highestResource.EntityNewOwner(this, request.from, request.newId);
            }
            else
            {
                if (!isTransferable) RainMeadow.Debug("Denied because not transferable");
                else if (!owner.isMe) RainMeadow.Debug("Denied because not mine");
                else if (isPending) RainMeadow.Debug("Denied because pending");
                request.from.QueueEvent(new EntityRequestResult.Error(request));
            }
        }

        // my request has been answered to
        // is this really needed?
        // I thought of stuff like "breaking grasps" if a request for the grasped object failed
        internal void ResolveRequest(EntityRequestResult requestResult)
        {
            RainMeadow.Debug(this);
            if (requestResult is EntityRequestResult.Ok) // I'm the new owner of this entity
            {
                // confirm pending grasps?
            }
            else if (requestResult is EntityRequestResult.Error) // Something went wrong, I should retry
            {
                // todo retry logic
                // abort pending grasps?
                RainMeadow.Error("request failed for " + this);
            }
            pendingRequest = null;
        }

        internal void Release()
        {
            throw new NotImplementedException();
        }

        internal void Released()
        {
            throw new NotImplementedException();
        }

        internal void ResolveRelease()
        {
            throw new NotImplementedException();
        }
    }
}
