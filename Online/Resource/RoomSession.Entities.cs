using System;
using System.Linq;

namespace RainMeadow
{
    public partial class RoomSession
    {
        // called by hooks on absroom.addentity, as well as Available()
        // A game entity has entered the room, check for corresponding online entity to be added
        public void ApoEnteringRoom(AbstractPhysicalObject entity, WorldCoordinate pos)
        {
            RainMeadow.Debug($"{this} - {entity}");
            //RainMeadow.Debug(System.Environment.StackTrace);
            if (!isActive) { if (isAvailable) RainMeadow.Error("Not registering because not isActive"); return; } // throw new InvalidOperationException("not isActive"); }
            if (OnlineEntity.map.TryGetValue(entity, out var oe))
            {
                if (oe.owner.isMe)
                {
                    oe.enterPos = pos;
                    //if (entity.realizedObject is Creature c && c.inShortcut) oe.enterPos.WashTileData();
                    EntityEnteredResource(oe);
                }
                else
                {
                    // we've just added it
                    RainMeadow.Debug("externally controlled entity joining : " + oe);
                }
            }
            else if (!WorldSession.registeringRemoteEntity)
            {
                throw new InvalidOperationException("Unregistered entity has entered the room! HOW!?? " + entity);
            }
        }

        public void ApoLeavingRoom(AbstractPhysicalObject entity)
        {
            RainMeadow.Debug($"{this} - {entity}");
            //RainMeadow.Debug(System.Environment.StackTrace);
            if (!isActive) { if (isAvailable) RainMeadow.Error("Not registering because not isActive"); return; } // only log if relevant?
            if (entities.FirstOrDefault(e=>e.entity == entity) is OnlineEntity oe)
            {
                if (oe.owner.isMe)
                {
                    EntityLeftResource(oe);
                }
                else
                {
                    // are you sure this is a throw? could let it leave and hold it hostage in pipes
                    throw new InvalidOperationException("remote entity trying to leave without being removed");
                }
            }
            else
            {
                // we are removing it from the room
            }
        }

        public override void EntityEnteredResource(OnlineEntity oe)
        {
            base.EntityEnteredResource(oe);
            oe.EnteredRoom(this);
        }

        public override void EntityLeftResource(OnlineEntity oe)
        {
            base.EntityLeftResource(oe);
            oe.LeftRoom(this);
        }
    }
}
