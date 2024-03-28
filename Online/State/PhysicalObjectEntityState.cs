using Mono.Cecil;
using RainMeadow.Online.State;
using System;
using System.ComponentModel;
using System.Text;

namespace RainMeadow
{
    public class PhysicalObjectEntityState : OnlineEntity.EntityState
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
            if (realizedState && onlineEntity.isMine && onlineEntity.apo.realizedObject != null && !onlineEntity.realized) { RainMeadow.Error($"have realized object, but not entity not marked as realized??: {onlineEntity} in resource {inResource}"); }
            if (realizedState && onlineEntity.isMine && !onlineEntity.realized)
            {
                //RainMeadow.Error($"asked for realized state, not realized: {onlineEntity} in resource {inResource}");
                realizedState = false;
            }
            RainMeadow.Trace($"{onlineEntity} sending realized state? {realizedState} entity realized ? {onlineEntity.realized}");

            this.pos = onlineEntity.apo.pos;
            this.realized = onlineEntity.realized; // now now, oe.realized means its realized in the owners world
                                                   // not necessarily whether we're getting a real state or not
            if (realizedState) this.realizedObjectState = GetRealizedState(onlineEntity);
        }

        protected virtual RealizedPhysicalObjectState GetRealizedState(OnlinePhysicalObject onlineObject)
        {
            if (onlineObject.apo.realizedObject == null) throw new InvalidOperationException("not realized");
            if (onlineObject.apo.realizedObject is Spear) return new RealizedSpearState(onlineObject);
            if (onlineObject.apo.realizedObject is SporePlant) return new RealizedSporePlantState(onlineObject);
            if (onlineObject.apo.realizedObject is Weapon) return new RealizedWeaponState(onlineObject);
            if (onlineObject.apo.realizedObject is SlimeMold) return new RealizedSlimeMoldState(onlineObject);
            if (onlineObject.apo.realizedObject is VultureGrub) return new RealizedVultureGrubState(onlineObject);
            if (onlineObject.apo.realizedObject is SeedCob) return new RealizedSeedCobState(onlineObject);
            if (onlineObject.apo.realizedObject is DangleFruit) return new RealizedDangleFruitState(onlineObject);
            if (onlineObject.apo.realizedObject is ExplosiveSpear) return new RealizedExplosiveSpearState(onlineObject);
            if (onlineObject.apo.realizedObject is ScavengerBomb) return new RealizedScavengerBombState(onlineObject);


            return new RealizedPhysicalObjectState(onlineObject);
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if (onlineEntity.owner.isMe || onlineEntity.isPending) { RainMeadow.Debug($"not syncing {this} because mine?{onlineEntity.owner.isMe} pending?{onlineEntity.isPending}"); return; }; // Don't sync if pending, reduces visibility and effect of lag

            var onlineObject = onlineEntity as OnlinePhysicalObject;
            RainMeadow.Trace($"{onlineEntity} received realized state? {realizedObjectState != null} entity realized?{onlineObject.realized}");

            onlineObject.beingMoved = true;
            var wasPos = onlineObject.apo.pos;
            try
            {
                onlineObject.apo.Move(pos);
            }
            catch (Exception e)
            {
                RainMeadow.Error($"{onlineEntity} failed to move from {wasPos} to {pos}, hard-setting position: " + e);
                onlineObject.apo.pos = pos;
                onlineObject.apo.world.GetAbstractRoom(pos).AddEntity(onlineObject.apo);
                //throw;
            }
            
            if(!pos.NodeDefined && !wasPos.CompareDisregardingNode(pos))onlineObject.apo.pos = pos; // pos isn't updated if compareDisregardingTile, but please, do
            onlineObject.beingMoved = false;
            onlineObject.realized = this.realized;
            if (onlineObject.apo.realizedObject != null)
            {
                RainMeadow.Trace($"{onlineEntity} realized target exists");
                realizedObjectState?.ReadTo(onlineEntity);
            }
        }
    }
}
