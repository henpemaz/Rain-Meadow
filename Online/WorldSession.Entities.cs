using System;
using System.Linq;

namespace RainMeadow
{
    public partial class WorldSession
    {
        public static bool registeringRemoteEntity;


        // This happens for local entities, so we create their respective OnlineEntity
        internal void NewEntityInWorld(AbstractPhysicalObject entity)
        {
            RainMeadow.Debug(this);
            if (!isActive) { RainMeadow.Error("Not registering because not isActive"); return; } // throw new InvalidOperationException("not isActive"); }
            if (!registeringRemoteEntity) // A new entity, presumably mine
            {
                // todo stronger checks if my entity or a leftover
                RainMeadow.Debug("Registering new entity as owned by myself");
                var oe = new OnlineEntity(entity, OnlineManager.mePlayer, entity.ID.number, entity.pos);
                RainMeadow.Debug(oe);
                OnlineManager.mePlayer.recentEntities[oe.id] = oe;
                OnlineEntity.map.Add(entity, oe);
                EntityEnteredResource(oe);
            }
            else
            {
                RainMeadow.Debug("skipping remote entity");
            }
        }

        internal void EntityLeftWorld(AbstractPhysicalObject self)
        {
            throw new NotImplementedException();
        }

    }
}
