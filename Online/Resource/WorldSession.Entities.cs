using System.Collections.Generic;

namespace RainMeadow
{
    public partial class WorldSession
    {
        public static bool registeringRemoteEntity;
        private List<AbstractPhysicalObject> earlyEntities = new(); // stuff that gets added during world loading

        // This happens for local entities, so we create their respective OnlineEntity
        public void EntityEnteringWorld(AbstractPhysicalObject entity)
        {
            if (!OnlineEntity.map.TryGetValue(entity, out var oe))
            {
                RainMeadow.Debug(this);
                if (!registeringRemoteEntity) // A new entity, presumably mine
                {
                    if (!isActive) // world population generates before this can be activated
                    {
                        RainMeadow.Debug("Queuing up entity for registering later");
                        this.earlyEntities.Add(entity);
                        return;
                    }
                    RainMeadow.Debug("Registering new entity as owned by myself");
                    var newOe = new OnlineEntity(entity, PlayersManager.mePlayer, new OnlineEntity.EntityId(PlayersManager.mePlayer.id.m_SteamID, entity.ID.number), entity.ID.RandomSeed, entity.pos, !RainMeadow.sSpawningPersonas);
                    RainMeadow.Debug(newOe);
                    OnlineManager.recentEntities[newOe.id] = newOe;
                    OnlineEntity.map.Add(entity, newOe);
                    EntityEnteredResource(newOe);
                }
                else
                {
                    RainMeadow.Debug("skipping remote entity");
                }
            }
            else if (!entities.Contains(oe))
            {
                EntityEnteredResource(oe);
            }
        }

        public void EntityLeavingWorld(AbstractPhysicalObject entity)
        {
            RainMeadow.Debug(this);

            if (OnlineEntity.map.TryGetValue(entity, out var oe))
            {
                EntityLeftResource(oe);
            }
            else
            {
                RainMeadow.Error("Unregistered entity leaving");
            }
        }

        public override void EntityEnteredResource(OnlineEntity oe)
        {
            base.EntityEnteredResource(oe);
            oe.worldSession = this;

            // not sure how "correct" this is because on the host side it might be different?
            if (!oe.owner.isMe) // kinda wanted a .isRemote helper at this point
            {
                this.world.GetAbstractRoom(oe.enterPos).AddEntity(oe.entity);
            }
        }

        public override void EntityLeftResource(OnlineEntity oe)
        {
            base.EntityLeftResource(oe);
            if (oe.worldSession == this) oe.worldSession = null;
        }
    }
}
