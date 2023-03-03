using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

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

        public WorldCoordinate initialPos;
        public WorldSession world;
        public RoomSession room;
        public OnlineResource activeResource => (room as OnlineResource ?? world);

        public bool realized;
        public bool isTransferable;
        public PlayerEvent pendingRequest;

        public OnlineEntity(AbstractPhysicalObject entity, OnlinePlayer owner, int id, WorldCoordinate pos)
        {
            this.entity = entity;
            this.owner = owner;
            this.id = id;
            this.initialPos = pos;
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

                oe.initialPos = newEntityEvent.initialPos;
                oe.entity.pos = oe.initialPos;
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
                id.altSeed = newEntityEvent.entityId;
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
                oe = new OnlineEntity(creature, newEntityEvent.owner, newEntityEvent.entityId, newEntityEvent.initialPos);
                OnlineEntity.map.Add(creature, oe);
                newEntityEvent.owner.recentEntities.Add(newEntityEvent.entityId, oe);
            }
            
            return oe;
        }

        internal void Request()
        {
            RainMeadow.Debug(this);
            if (owner.isMe) throw new InvalidProgrammerException("this entity is already mine");
            if (!activeResource.isAvailable) throw new InvalidProgrammerException("in unavailable resource");

            owner.RequestEntity(this, room); // do we have to tell them what resource this is about
            // could be nice to avoid a desynchy situation, sanity-check that the room in question is the room 
            // that the owner knows the entity is at
            // as for world... would there ever be a situation in which we request a world entity at world level from the world owner?
            // nah don't think so, it's all about the room the entity is going to be active in
            // unless for some reason we start having lobby entitites, but I think that's against the whole point of distributing resources thin?
        }

        internal void Requested(EntityRequest entityRequest)
        {
            throw new NotImplementedException();
            // do we store a list of Resources this Entity is in?
            // then we'd know which participants to notify
            // but I mean it's really just room and world, don't overabstract
        }

        internal void ResolveRequest(EntityRequestResult requestResult)
        {
            throw new NotImplementedException();
        }

        // there is only one person who can sort out a release, and it is the room-owner for an entity in room
        // or a world owner for an entity in world
        // but also, when releasing a room we could append a list of our entities 
        // if we release room to room.owner, then owner can claim the entities and broadcast their new owner
        // if we release room to world.owner, then world owner can claim them and so on
        // entities that are tracked on room and world 
        // vs entities that are only tracked in room and destroyed on abstraction
        // as well as not broadcasted about at world level
        // but this cannot be generalized to world releasing to lobby, because lobby isn't aware of entities
        // should lobby be aware of entities
        // should entities automatically enter super when they enter a subresource
        internal void Release()
        {
            throw new NotImplementedException();
        }
    }
}
