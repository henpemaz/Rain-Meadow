using System;
using System.Linq;

namespace RainMeadow
{
    public partial class RoomSession
    {
        // called by hooks on absroom.addentity, as well as Available()
        // A game entity has entered the room, check for corresponding online entity to be added
        internal void EntityEnteringRoom(AbstractPhysicalObject entity, WorldCoordinate pos)
        {
            RainMeadow.Debug(this);
            if (!isActive) { RainMeadow.Error("Not registering because not isActive"); return; } // throw new InvalidOperationException("not isActive"); }
            if (OnlineEntity.map.TryGetValue(entity, out var oe))
            {
                if (oe.owner.isMe)
                {
                    EntityEnteredResource(oe);
                }
                else
                {
                    RainMeadow.Debug("externally controlled entity joining : " + oe);
                }
            }
            else
            {
                throw new InvalidOperationException("Unregistered entity has entered the room! " + entity);
            }
        }

        internal void EntityLeavingRoom(AbstractPhysicalObject entity)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug(entity);
            if (!isActive) { RainMeadow.Error("Not registering because not isActive"); return; }
            if (entities.FirstOrDefault(e=>e.entity == entity) is OnlineEntity oe)
            {
                if (oe.owner.isMe)
                {
                    EntityLeftResource(oe);
                }
                else
                {
                    RainMeadow.Debug("externally controlled entity leaving : " + oe);
                }
            }
            else
            {
                RainMeadow.Debug("untracked entity leaving! " + entity);
            }
        }

        protected override void EntityEnteredResource(OnlineEntity oe)
        {
            base.EntityEnteredResource(oe);
            oe.room = this;
            if (oe.entity is not AbstractCreature creature) { throw new InvalidOperationException("entity not a creature"); }
            if (!oe.owner.isMe)
            {
                RainMeadow.Debug("A remote creature entered, adding it to the room");
                if (absroom.realizedRoom is Room room && creature.AllowedToExistInRoom(room))
                {
                    RainMeadow.Debug("spawning creature " + creature);
                    if (oe.initialPos.TileDefined)
                    {
                        RainMeadow.Debug("added directly to the room");
                        absroom.AddEntity(creature);
                        creature.RealizeInRoom(); // places in room
                    }
                    else if (oe.initialPos.NodeDefined)
                    {
                        RainMeadow.Debug("added directly to shortcut system");
                        creature.Realize();
                        creature.realizedCreature.inShortcut = true;
                        // this calls MOVE on the next tick which remove-adds
                        absroom.world.game.shortcuts.CreatureEnterFromAbstractRoom(creature.realizedCreature, absroom, oe.initialPos.abstractNode);
                    }
                    else
                    {
                        RainMeadow.Debug("INVALID POS??" + oe.initialPos);
                        throw new InvalidOperationException("entity must have a vaild position");
                    }
                }
                else
                {
                    RainMeadow.Debug("not spawning creature " + creature);
                    RainMeadow.Debug($"reasons {absroom.realizedRoom is not null} {(absroom.realizedRoom != null && creature.AllowedToExistInRoom(absroom.realizedRoom))}");
                    if (creature.realizedCreature != null)
                    {
                        if (!oe.initialPos.TileDefined && oe.initialPos.NodeDefined && absroom.realizedRoom != null && absroom.realizedRoom.shortCutsReady)
                        {
                            RainMeadow.Debug("added realized creature to shortcut system");
                            creature.realizedCreature.inShortcut = true;
                            // this calls MOVE on the next tick which remove-adds, this could be bad?
                            absroom.world.game.shortcuts.CreatureEnterFromAbstractRoom(creature.realizedCreature, absroom, oe.initialPos.abstractNode);
                        }
                        else
                        {
                            // can't abstractize properly because previous location is lost
                            RainMeadow.Debug("cleared realized creature and added to absroom as abstract entity");
                            creature.realizedCreature = null;
                            absroom.AddEntity(creature);
                        }
                    }
                    else
                    {
                        RainMeadow.Debug("added to absroom as abstract entity");
                        absroom.AddEntity(creature);
                    }
                }
            }
            else
            {
                RainMeadow.Debug("My own entity has entered the room, not doing anything about it");
            }
        }

        protected override void EntityLeftResource(OnlineEntity oe)
        {
            base.EntityLeftResource(oe);
            if (!oe.owner.isMe && oe.room == this)
            {
                RainMeadow.Debug("Removing entity from room: " + oe);
                absroom.RemoveEntity(oe.entity);
                oe.entity.slatedForDeletion = true;
                if (oe.entity.realizedObject is PhysicalObject po)
                {
                    po.slatedForDeletetion = true;
                    if (absroom.realizedRoom is Room room) room.RemoveObject(po);
                    if(po is Creature c && c.inShortcut)
                    {
                        c.RemoveFromShortcuts();
                        c.inShortcut = false;
                    }
                }
            }
            else
            {
                RainMeadow.Debug("my own entity leaving");
            }
        }
    }
}
