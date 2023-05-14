using System;

namespace RainMeadow
{
    public partial class OnlineEntity
    {
        public EntityState latestState;

        public void ReadState(EntityState entityState, ulong tick)
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

        public EntityState GetState(ulong tick, OnlineResource resource)
        {
            if (resource is WorldSession ws && !OnlineManager.lobby.gameMode.ShouldSyncObjectInWorld(ws, entity)) throw new InvalidOperationException("asked for world state, not synched");
            if (resource is RoomSession rs && !OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(rs, entity)) throw new InvalidOperationException("asked for room state, not synched");
            var realizedState = resource is RoomSession;
            if (realizedState) { if (entity.realizedObject != null && !realized) RainMeadow.Error($"have realized object, but not entity not marked as realized??: {this} in resource {resource}"); }
            if (realizedState && !realized)
            {
                //throw new InvalidOperationException("asked for realized state, not realized");
                RainMeadow.Error($"asked for realized state, not realized: {this} in resource {resource}");
                realizedState = false;
            }
            if (entity is AbstractCreature)
            {
                return new AbstractCreatureState(this, tick, realizedState);
            }
            return new PhysicalObjectEntityState(this, tick, realizedState);
        }
    }
}
