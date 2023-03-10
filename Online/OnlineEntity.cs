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

        public class EntityId : System.IEquatable<EntityId> // How we refer to a game entity online
        {
            public ulong originalOwner;
            public int id;

            public EntityId(ulong originalOwner, int id)
            {
                this.originalOwner = originalOwner;
                this.id = id;
            }

            public override string ToString()
            {
                return $"#{id}-{originalOwner}";
            }
            public override bool Equals(object obj) => this.Equals(obj as EntityId);
            public bool Equals(EntityId other)
            {
                return other != null && id == other.id && originalOwner == other.originalOwner;
            }
            public override int GetHashCode() => id.GetHashCode() + originalOwner.GetHashCode();

            public static bool operator ==(EntityId lhs, EntityId rhs)
            {
                return lhs is null ? rhs is null : lhs.Equals(rhs);
            }
            public static bool operator !=(EntityId lhs, EntityId rhs) => !(lhs == rhs);
        }

        // todo maybe abstract this into "entity" and "game entity" ?? what would I use it for though? persona data at lobby level?
        public AbstractPhysicalObject entity;
        public OnlinePlayer owner;
        public EntityId id;
        public int seed;
        public bool isTransferable = true; // todo make own personas not transferable

        public WorldSession world;
        public RoomSession room;
        public OnlineResource lowestResource => (room as OnlineResource ?? world);
        public OnlineResource highestResource => (world as OnlineResource ?? room);

        public bool realized;
        public WorldCoordinate enterPos; // todo keep this updated, currently loading with creatures mid-room still places them in shortcuts

        public bool isPending => pendingRequest != null;
        public PlayerEvent pendingRequest;

        public OnlineEntity(AbstractPhysicalObject entity, OnlinePlayer owner, EntityId id, int seed, WorldCoordinate pos, bool isTransferable)
        {
            this.entity = entity;
            this.owner = owner;
            this.id = id;
            this.seed = seed;
            this.enterPos = pos;
            this.isTransferable = isTransferable;
        }

        public override string ToString()
        {
            return $"{entity}:{id} from {owner}";
        }

        public static OnlineEntity CreateOrReuseEntity(NewEntityEvent newEntityEvent, World world)
        {
            RainMeadow.DebugMethod();
            OnlineEntity oe = null;
            
            if (OnlineManager.recentEntities.TryGetValue(newEntityEvent.entityId, out oe))
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
                oe = new OnlineEntity(creature, newEntityEvent.owner, newEntityEvent.entityId, newEntityEvent.seed, newEntityEvent.initialPos, newEntityEvent.isTransferable);
                OnlineEntity.map.Add(creature, oe);
                OnlineManager.recentEntities.Add(newEntityEvent.entityId, oe);
            }
            
            return oe;
        }

        internal void EnteredRoom(RoomSession newRoom)
        {
            RainMeadow.Debug(this);
            if (entity is not AbstractCreature creature) { throw new InvalidOperationException("entity not a creature"); } // todo support noncreechers
            if (room != null && room != newRoom) // still in previous room
            {
                LeftRoom(room);
            }
            room = newRoom;
            if (!owner.isMe)
            {
                RainMeadow.Debug("A remote creature entered, adding it to the room");
                if (newRoom.absroom.realizedRoom is Room realRoom && creature.AllowedToExistInRoom(realRoom))
                {
                    RainMeadow.Debug("spawning creature " + creature);
                    if (enterPos.TileDefined)
                    {
                        RainMeadow.Debug("added directly to the room");
                        newRoom.absroom.AddEntity(creature);
                        creature.RealizeInRoom(); // places in room
                    }
                    else if (enterPos.NodeDefined)
                    {
                        RainMeadow.Debug("added directly to shortcut system");
                        creature.Realize();
                        creature.realizedCreature.inShortcut = true;
                        // this calls MOVE on the next tick which remove-adds
                        newRoom.absroom.world.game.shortcuts.CreatureEnterFromAbstractRoom(creature.realizedCreature, newRoom.absroom, enterPos.abstractNode);
                    }
                    else
                    {
                        RainMeadow.Debug("INVALID POS??" + enterPos);
                        throw new InvalidOperationException("entity must have a vaild position");
                    }
                }
                else
                {
                    RainMeadow.Debug("not spawning creature " + creature);
                    RainMeadow.Debug($"reasons {newRoom.absroom.realizedRoom is not null} {(newRoom.absroom.realizedRoom != null && creature.AllowedToExistInRoom(newRoom.absroom.realizedRoom))}");
                    if (creature.realizedCreature != null)
                    {
                        if (!enterPos.TileDefined && enterPos.NodeDefined && newRoom.absroom.realizedRoom != null && newRoom.absroom.realizedRoom.shortCutsReady)
                        {
                            RainMeadow.Debug("added realized creature to shortcut system");
                            creature.realizedCreature.inShortcut = true;
                            // this calls MOVE on the next tick which remove-adds, this could be bad?
                            newRoom.absroom.world.game.shortcuts.CreatureEnterFromAbstractRoom(creature.realizedCreature, newRoom.absroom, enterPos.abstractNode);
                        }
                        else
                        {
                            // can't abstractize properly because previous location is lost
                            RainMeadow.Debug("cleared realized creature and added to absroom as abstract entity");
                            creature.realizedCreature = null;
                            newRoom.absroom.AddEntity(creature);
                        }
                    }
                    else
                    {
                        RainMeadow.Debug("added to absroom as abstract entity");
                        newRoom.absroom.AddEntity(creature);
                    }
                }
            }
        }

        internal void LeftRoom(RoomSession oldRoom)
        {
            RainMeadow.Debug(this);
            if (room == oldRoom)
            {
                if (!owner.isMe)
                {
                    RainMeadow.Debug("Removing entity from room: " + this);
                    oldRoom.absroom.RemoveEntity(entity);
                    if (entity.realizedObject is PhysicalObject po)
                    {
                        if (oldRoom.absroom.realizedRoom is Room room) room.RemoveObject(po);
                        if (po is Creature c && c.inShortcut)
                        {
                            if (c.RemoveFromShortcuts()) c.inShortcut = false;
                        }
                    }
                }
                else
                {
                    RainMeadow.Debug("my own entity leaving");
                }
                room = null;
            }
        }

        public void NewOwner(OnlinePlayer newOwner)
        {
            RainMeadow.Debug(this);
            var wasOwner = owner;

            owner = newOwner;

            if (wasOwner.isMe)
            {
                // this screams "iterate" but at the same time... it's just these two for RW, maybe next game
                if (world != null)
                {
                    OnlineManager.RemoveFeed(world, this);
                    world.SubresourcesUnloaded();
                }
                if (room != null)
                {
                    OnlineManager.RemoveFeed(room, this);
                    room.SubresourcesUnloaded();
                }
            }
            if (newOwner.isMe) // we only start feeding after we get the broadcast of new owner.
                               // Maybe this should be in ResolveRequest instead? but then there's no guarantee the resource owners will have the new ID
                               // at least there will be no collisions so if we ignore that data its ok?
            {
                if (world != null)
                {
                    OnlineManager.AddFeed(world, this);
                }
                if (room != null)
                {
                    OnlineManager.AddFeed(room, this);
                }
                realized = entity.realizedObject != null; // owner is responsible for upkeeping this
            }
        }

        // I was in a resource and I was left behind
        internal void Deactivated(OnlineResource onlineResource)
        {
            RainMeadow.Debug(this);
            if (onlineResource is WorldSession && this.world == onlineResource) this.world = null;
            if (onlineResource is RoomSession && this.room == onlineResource) this.room = null;
            if (owner.isMe) OnlineManager.RemoveFeed(onlineResource, this);
        }

        // I request, to someone else
        internal void Request()
        {
            RainMeadow.Debug(this);
            if (owner.isMe) throw new InvalidProgrammerException("this entity is already mine");
            if (!isTransferable) throw new InvalidProgrammerException("cannot be transfered");
            if (isPending) throw new InvalidProgrammerException("this entity has a pending request");
            if (!lowestResource.isAvailable) throw new InvalidProgrammerException("in unavailable resource");

            owner.RequestEntity(this);
        }

        // I've been requested and I'll pass the entity on
        internal void Requested(EntityRequest request)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Requested by : " + request.from.name);
            if (isTransferable && this.owner.isMe && !isPending)
            {
                request.from.QueueEvent(new EntityRequestResult.Ok(request)); // your request was well received, now please be patient while I transfer it
                this.highestResource.EntityNewOwner(this, request.from);
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
            if (requestResult is EntityRequestResult.Ok) // I'm the new owner of this entity (comes as separate event though)
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

        // I release this entity (to room host or world host)
        internal void Release()
        {
            RainMeadow.Debug(this);
            if (!owner.isMe) throw new InvalidProgrammerException("not mine");
            if (!isTransferable) throw new InvalidProgrammerException("cannot be transfered");
            if (isPending) throw new InvalidProgrammerException("this entity has a pending request");

            if (highestResource.owner.isMe)
            {
                RainMeadow.Debug("Staying as supervisor");
            }
            else
            {
                highestResource.owner.ReleaseEntity(this);
            }
        }

        // someone released "to me"
        internal void Released(EntityReleaseEvent entityRelease)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Released by : " + entityRelease.from.name);
            if (isTransferable && this.owner == entityRelease.from  && this.highestResource.owner.isMe) // theirs and I can transfer
            {
                entityRelease.from.QueueEvent(new EntityReleaseResult.Ok(entityRelease)); // ok to them
                var res = entityRelease.inResource;
                if (res.isAvailable || res.super.isActive)
                {
                    if (this.owner != res.owner) this.highestResource.EntityNewOwner(this, res.owner);
                }
                else
                {
                    if (!this.owner.isMe) this.highestResource.EntityNewOwner(this, OnlineManager.mePlayer);
                }
            }
            else
            {
                if (!isTransferable) RainMeadow.Error("Denied because not transferable");
                else if (owner != entityRelease.from) RainMeadow.Error("Denied because not theirs");
                else if (!highestResource.owner.isMe) RainMeadow.Error("Denied because I don't supervise it");
                else if (isPending) RainMeadow.Error("Denied because pending");
                entityRelease.from.QueueEvent(new EntityReleaseResult.Error(entityRelease));
            }
        }

        // got an answer back from my release
        internal void ResolveRelease(EntityReleaseResult result)
        {
            RainMeadow.Debug(this);
            if (result is EntityReleaseResult.Ok)
            {
                // ?
            }
            else if (result is EntityReleaseResult.Error) // Something went wrong, I should retry
            {
                // todo retry logic
                RainMeadow.Error("request failed for " + this);
            }
            pendingRequest = null;
        }
    }
}
