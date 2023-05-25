using System;
using System.Runtime.CompilerServices;

namespace RainMeadow
{
    public class OnlinePhysicalObject : OnlineEntity
    {
        public readonly AbstractPhysicalObject apo;
        public readonly int seed;
        public bool realized;
        public WorldCoordinate enterPos; // todo keep this updated, currently loading with creatures mid-room still places them in shortcuts

        public bool beingMoved;
        public static ConditionalWeakTable<AbstractPhysicalObject, OnlinePhysicalObject> map = new();

        public RoomSession roomSession => this.lowestResource as RoomSession; // shorthand

        internal static OnlinePhysicalObject RegisterPhysicalObject(AbstractPhysicalObject apo, WorldCoordinate pos)
        {
            OnlinePhysicalObject newOe = NewFromApo(apo, pos);
            RainMeadow.Debug("Registering new entity - " + newOe.ToString());
            OnlineManager.recentEntities[newOe.id] = newOe;
            OnlinePhysicalObject.map.Add(apo, newOe);
            return newOe;
        }

        public static OnlinePhysicalObject NewFromApo(AbstractPhysicalObject apo, WorldCoordinate pos)
        {
            if (apo is AbstractCreature ac) return new OnlineCreature(ac, apo.ID.RandomSeed, apo.realizedObject != null, pos, PlayersManager.mePlayer, new OnlineEntity.EntityId(PlayersManager.mePlayer.id.m_SteamID, apo.ID.number), !RainMeadow.sSpawningPersonas);
            return new OnlinePhysicalObject(apo, apo.ID.RandomSeed, apo.realizedObject != null, pos, PlayersManager.mePlayer, new OnlineEntity.EntityId(PlayersManager.mePlayer.id.m_SteamID, apo.ID.number), !RainMeadow.sSpawningPersonas);
        }

        public OnlinePhysicalObject(AbstractPhysicalObject apo, int seed, bool realized, WorldCoordinate pos, OnlinePlayer owner, EntityId id, bool isTransferable) : base(owner, id, isTransferable)
        {
            this.apo = apo;
            this.seed = seed;
            this.enterPos = pos;
            this.realized = realized;
        }

        public override void NewOwner(OnlinePlayer newOwner)
        {
            base.NewOwner(newOwner);
            if (newOwner.isMe)
            {
                realized = apo.realizedObject != null; // owner is responsible for upkeeping this
            }
        }

        internal override NewEntityEvent AsNewEntityEvent(OnlineResource inResource)
        {
            return new NewObjectEvent(seed, enterPos, realized, apo.ToString(), inResource, this, null);
        }

        internal static OnlineEntity FromEvent(NewObjectEvent newObjectEvent, OnlineResource inResource)
        {
            World world = inResource.World;
            EntityID id = world.game.GetNewID();
            id.altSeed = newObjectEvent.seed;

            var apo = SaveState.AbstractPhysicalObjectFromString(world, newObjectEvent.serializedObject);
            var oe = new OnlinePhysicalObject(apo, newObjectEvent.seed, newObjectEvent.realized, newObjectEvent.enterPos, newObjectEvent.owner, newObjectEvent.entityId, newObjectEvent.isTransferable);
            OnlinePhysicalObject.map.Add(apo, oe);
            OnlineManager.recentEntities.Add(oe.id, oe);
            return oe;
        }

        public override void ReadState(EntityState entityState, ulong tick)
        {
            // todo easing??
            // might need to get a ref to the sender all the way here for lag estimations?
            // todo delta handling
            if (lowestResource is RoomSession && !entityState.realizedState) return; // We can skip abstract state if we're receiving state in a room as well
            beingMoved = true;
            entityState.ReadTo(this);
            beingMoved = false;
            latestState = entityState;
        }

        public override EntityState GetState(ulong tick, OnlineResource resource)
        {
            if (resource is WorldSession ws && !OnlineManager.lobby.gameMode.ShouldSyncObjectInWorld(ws, apo)) throw new InvalidOperationException("asked for world state, not synched");
            if (resource is RoomSession rs && !OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(rs, apo)) throw new InvalidOperationException("asked for room state, not synched");
            var realizedState = resource is RoomSession;
            if (realizedState) { if (apo.realizedObject != null && !realized) RainMeadow.Error($"have realized object, but not entity not marked as realized??: {this} in resource {resource}"); }
            if (realizedState && !realized)
            {
                //throw new InvalidOperationException("asked for realized state, not realized");
                RainMeadow.Error($"asked for realized state, not realized: {this} in resource {resource}");
                realizedState = false;
            }
            return new PhysicalObjectEntityState(this, tick, realizedState);
        }

        public override string ToString()
        {
            return apo.ToString() + base.ToString();
        }
    }
}
