using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public partial class WorldSession
    {
        public static bool registeringRemoteEntity;
        private List<AbstractPhysicalObject> earlyEntities = new();

        // At a world level, entities "exist" and "have a .worldposition" and that's about it, just enough to show up on the map

        // This happens for local entities, so we create their respective OnlineEntity
        internal void NewEntityInWorld(AbstractPhysicalObject entity)
        {
            RainMeadow.Debug(this);
            // world population generates before this can be activated
            if (!isActive) { 
                RainMeadow.Debug("Queuing up entity for registering later");
                this.earlyEntities.Add(entity);
                return; } // throw new InvalidOperationException("not isActive"); }
            if (!registeringRemoteEntity) // A new entity, presumably mine
            {
                RainMeadow.Debug("Registering new entity as owned by myself");
                var oe = new OnlineEntity(entity, OnlineManager.mePlayer, new OnlineEntity.EntityId(OnlineManager.mePlayer.id.m_SteamID, entity.ID.number), entity.ID.RandomSeed, entity.pos, !RainMeadow.sSpawningPersonas);
                RainMeadow.Debug(oe);
                OnlineManager.recentEntities[oe.id] = oe;
                OnlineEntity.map.Add(entity, oe);
                EntityEnteredResource(oe);
            }
            else
            {
                RainMeadow.Debug("skipping remote entity");
            }
        }

        protected override void EntityEnteredResource(OnlineEntity oe)
        {
            base.EntityEnteredResource(oe);
            oe.worldSession = this;

            // not sure how "correct" this is because on the host side it might be different?
            if (!oe.owner.isMe) // kinda wanted a .isRemote helper at this point
            {
                this.world.GetAbstractRoom(oe.enterPos).AddEntity(oe.entity);
            }
        }

        internal void EntityLeftWorld(AbstractPhysicalObject self)
        {
            throw new NotImplementedException();
        }
    }
}
