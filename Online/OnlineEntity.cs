using System;
using System.Runtime.CompilerServices;

namespace RainMeadow
{
    // Welcome to polymorphism hell
    public partial class OnlineEntity
    {
        public static ConditionalWeakTable<AbstractPhysicalObject, OnlineEntity> map = new();

        // todo maybe abstract this into "entity" and "game entity" ?? what would I use it for though? persona data at lobby level?
        public AbstractPhysicalObject entity;
        public OnlinePlayer owner;
        public EntityId id;
        public int seed;
        public bool isTransferable = true;

        public WorldSession worldSession;
        public RoomSession roomSession;
        public OnlineResource lowestResource => (roomSession as OnlineResource ?? worldSession);
        public OnlineResource highestResource => (worldSession as OnlineResource ?? roomSession);

        public bool realized;
        public WorldCoordinate enterPos; // todo keep this updated, currently loading with creatures mid-room still places them in shortcuts

        public bool isPending => pendingRequest != null;
        public OnlineEvent pendingRequest;

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
            RainMeadow.DebugMe();
            OnlineEntity oe = null;
            
            if (OnlineManager.recentEntities.TryGetValue(newEntityEvent.entityId, out oe))
            {
                RainMeadow.Debug("reusing existing entity " + oe);
                var creature = oe.entity as AbstractCreature;
                creature.slatedForDeletion = false;
                if (creature.realizedObject is PhysicalObject po) po.slatedForDeletetion = false;

                oe.enterPos = newEntityEvent.initialPos;
                oe.realized = newEntityEvent.realized;

                if (!world.IsRoomInRegion(oe.entity.pos.room))
                {
                    oe.entity.world = world;
                    oe.entity.pos = oe.enterPos;
                    WorldSession.registeringRemoteEntity = true;
                    world.GetAbstractRoom(oe.enterPos).AddEntity(oe.entity);
                    WorldSession.registeringRemoteEntity = false;
                }
                // we don't update other fields because they shouldn't change... in theory...
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
                WorldSession.registeringRemoteEntity = true;
                var creature = new AbstractCreature(world, StaticWorld.GetCreatureTemplate(type), null, newEntityEvent.initialPos, id);
                world.GetAbstractRoom(newEntityEvent.initialPos).AddEntity(creature);
                WorldSession.registeringRemoteEntity = false;
                if (creature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat) // for some dumb reason it doesn't get a default
                {
                    creature.state = new PlayerState(creature, 0, RainMeadow.Ext_SlugcatStatsName.OnlineSessionRemotePlayer, false);
                }
                oe = new OnlineEntity(creature, newEntityEvent.owner, newEntityEvent.entityId, newEntityEvent.seed, newEntityEvent.initialPos, newEntityEvent.isTransferable);
                OnlineEntity.map.Add(creature, oe);
                OnlineManager.recentEntities.Add(newEntityEvent.entityId, oe);
            }
            oe.realized = newEntityEvent.realized;

            return oe;
        }

        public void EnteredRoom(RoomSession newRoom)
        {
            RainMeadow.Debug(this);
            if (entity is not AbstractCreature creature) { throw new InvalidOperationException("entity not a creature"); } // todo support noncreechers
            if (roomSession != null && roomSession != newRoom) // still in previous room
            {
                LeftRoom(roomSession);
            }
            roomSession = newRoom;
            if (!owner.isMe)
            {
                RainMeadow.Debug("A remote creature entered, adding it to the room");
                entity.Move(enterPos);
                if (newRoom.absroom.realizedRoom is Room realRoom && creature.AllowedToExistInRoom(realRoom))
                {
                    RainMeadow.Debug("spawning creature " + creature);
                    if (enterPos.TileDefined)
                    {
                        RainMeadow.Debug("added directly to the room");
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
                        }
                    }
                    else
                    {
                        RainMeadow.Debug("added to absroom as abstract entity");
                    }
                }
            }
        }

        public void LeftRoom(RoomSession oldRoom)
        {
            RainMeadow.Debug(this);
            if (roomSession == oldRoom)
            {
                if (!owner.isMe)
                {
                    RainMeadow.Debug("Removing entity from room: " + this);
                    oldRoom.absroom.RemoveEntity(entity);
                    if (entity.realizedObject is PhysicalObject po)
                    {
                        if (oldRoom.absroom.realizedRoom is Room room)
                        {
                            room.RemoveObject(po);
                            room.CleanOutObjectNotInThisRoom(po);
                        }
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
                roomSession = null;
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
                if (worldSession != null)
                {
                    OnlineManager.RemoveFeed(worldSession, this);
                    worldSession.SubresourcesUnloaded();
                }
                if (roomSession != null)
                {
                    OnlineManager.RemoveFeed(roomSession, this);
                    roomSession.SubresourcesUnloaded();
                }
            }
            if (newOwner.isMe) // we only start feeding after we get the broadcast of new owner.
                               // Maybe this should be in ResolveRequest instead? but then there's no guarantee the resource owners will have the new ID
                               // at least there will be no collisions so if we ignore that data its ok?
            {
                if (worldSession != null)
                {
                    OnlineManager.AddFeed(worldSession, this);
                }
                if (roomSession != null)
                {
                    OnlineManager.AddFeed(roomSession, this);
                }
                realized = entity.realizedObject != null; // owner is responsible for upkeeping this
            }
        }

        // I was in a resource and I was left behind
        public void Deactivated(OnlineResource onlineResource)
        {
            RainMeadow.Debug(this);
            if (onlineResource is WorldSession && this.worldSession == onlineResource) this.worldSession = null;
            if (onlineResource is RoomSession && this.roomSession == onlineResource) this.roomSession = null;
            if (owner.isMe) OnlineManager.RemoveFeed(onlineResource, this);
        }

        // I request, to someone else
        public void Request()
        {
            RainMeadow.Debug(this);
            if (owner.isMe) throw new InvalidProgrammerException("this entity is already mine");
            if (!isTransferable) throw new InvalidProgrammerException("cannot be transfered");
            if (isPending) throw new InvalidProgrammerException("this entity has a pending request");
            if (!lowestResource.isAvailable) throw new InvalidProgrammerException("in unavailable resource");
            if (!owner.hasLeft && lowestResource.memberships.ContainsKey(owner))
            {
                pendingRequest = owner.QueueEvent(new EntityRequest(this));
            }
            else
            {
                pendingRequest = highestResource.owner.QueueEvent(new EntityRequest(this));
            }
        }

        // I've been requested and I'll pass the entity on
        public void Requested(EntityRequest request)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Requested by : " + request.from.name);
            if (isTransferable && this.owner.isMe)
            {
                request.from.QueueEvent(new EntityRequestResult.Ok(request)); // your request was well received, now please be patient while I transfer it
                this.highestResource.EntityNewOwner(this, request.from);
            }
            else if (isTransferable && (owner.hasLeft || !lowestResource.memberships.ContainsKey(owner)) && this.highestResource.owner.isMe)
            {
                request.from.QueueEvent(new EntityRequestResult.Ok(request));
                this.highestResource.EntityNewOwner(this, request.from);
            }
            else
            {
                if (!isTransferable) RainMeadow.Debug("Denied because not transferable");
                else if (!owner.isMe) RainMeadow.Debug("Denied because not mine");
                request.from.QueueEvent(new EntityRequestResult.Error(request));
            }
        }

        // my request has been answered to
        // is this really needed?
        // I thought of stuff like "breaking grasps" if a request for the grasped object failed
        public void ResolveRequest(EntityRequestResult requestResult)
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
        public void Release()
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
                this.pendingRequest = highestResource.owner.QueueEvent(new EntityReleaseEvent(this, lowestResource));
            }
        }

        // someone released "to me"
        public void Released(EntityReleaseEvent entityRelease)
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
                    if (!this.owner.isMe) this.highestResource.EntityNewOwner(this, PlayersManager.mePlayer);
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
        public void ResolveRelease(EntityReleaseResult result)
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
