using Mono.Cecil;
using System;
using System.ComponentModel;
using System.Text;

namespace RainMeadow
{
    public class PhysicalObjectEntityState : EntityState
    {
        [OnlineField]
        public WorldCoordinate pos;
        [OnlineField]
        public bool realized;
        [OnlineField(group = "realized", nullable = true, polymorphic = true)]
        public RealizedPhysicalObjectState realizedObjectState;

        public PhysicalObjectEntityState() : base() { }
        public PhysicalObjectEntityState(OnlinePhysicalObject onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
        {
            if (inResource is WorldSession ws && !OnlineManager.lobby.gameMode.ShouldSyncObjectInWorld(ws, onlineEntity.apo)) throw new InvalidOperationException("asked for world state, not synched");
            if (inResource is RoomSession rs && !OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(rs, onlineEntity.apo)) throw new InvalidOperationException("asked for room state, not synched");
            var realizedState = inResource is RoomSession;
            if (realizedState && onlineEntity.isMine && onlineEntity.apo.realizedObject != null && !realized) { RainMeadow.Error($"have realized object, but not entity not marked as realized??: {this} in resource {inResource}"); }
            if (realizedState && onlineEntity.isMine && !realized)
            {
                //RainMeadow.Error($"asked for realized state, not realized: {this} in resource {inResource}");
                realizedState = false;
            }

            this.pos = onlineEntity.apo.pos;
            this.realized = onlineEntity.realized; // now now, oe.realized means its realized in the owners world
                                                   // not necessarily whether we're getting a real state or not
            if (realizedState) this.realizedObjectState = GetRealizedState(onlineEntity);
        }

        protected virtual RealizedPhysicalObjectState GetRealizedState(OnlinePhysicalObject onlineObject)
        {
            if (onlineObject.apo.realizedObject == null) throw new InvalidOperationException("not realized");
            if (onlineObject.apo.realizedObject is Spear) return new RealizedSpearState(onlineObject);
            if (onlineObject.apo.realizedObject is Weapon) return new RealizedWeaponState(onlineObject);
            if (onlineObject.apo.realizedObject is DangleFruit) { return new RealizedDangleFruitState(onlineObject); }
            return new RealizedPhysicalObjectState(onlineObject);
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            var onlineObject = onlineEntity as OnlinePhysicalObject;
            onlineObject.beingMoved = true;
            var wasPos = onlineObject.apo.pos;
            try
            {
                onlineObject.apo.Move(pos);
            }
            catch (Exception e)
            {
                RainMeadow.Error($"failed to move from {wasPos} to {pos}: " + e);
                //throw;
            }
            
            if(!pos.NodeDefined && !wasPos.CompareDisregardingNode(pos))onlineObject.apo.pos = pos; // pos isn't updated if compareDisregardingTile, but please, do
            onlineObject.beingMoved = false;
            onlineObject.realized = this.realized;
            if (onlineObject.apo.realizedObject != null)
            {
                realizedObjectState?.ReadTo(onlineEntity);
            }
        }
    }
}
