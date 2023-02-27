using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public partial class RoomSession : OnlineResource
    {
        public AbstractRoom absroom;
        public bool abstractOnDeactivate;

        public WorldSession worldSession => super as WorldSession;
        public RoomSession(WorldSession ws, AbstractRoom absroom)
        {
            super = ws;
            this.absroom = absroom;
            deactivateOnRelease = true;
        }

        protected override void ActivateImpl()
        {

        }

        protected override void DeactivateImpl()
        {
            if (abstractOnDeactivate)
            {
                absroom.Abstractize();
            }
        }

        protected override void AvailableImpl()
        {
            base.AvailableImpl();
            // Loaded: register entities, and from there on new entities are added automatically
            // asap, active = fullyloaded = too late
            // maybe I should just change what Active means instead?
            foreach (var ent in absroom.entities)
            {
                if(ent is AbstractPhysicalObject apo)
                {
                    EntityEnteringRoom(apo, apo.pos);
                }
            }
        }

        protected override void SubscribedImpl(OnlinePlayer player)
        {
            base.SubscribedImpl(player);
            foreach (var ent in entities)
            {
                if (player == ent.owner) continue;
                player.QueueEvent(new NewEntityEvent(this, ent));
            }
        }

        internal override string Identifier()
        {
            return super.Identifier() + absroom.name;
        }

        public override void ReadState(ResourceState newState, ulong ts)
        {
            if(newState is RoomState newRoomState)
            {
                foreach(var entityState in newRoomState.entityStates)
                {
                    if(entityState is OnlineEntity.EntityState es)
                    {
                        if (es.onlineEntity.owner.isMe) continue;
                        es.onlineEntity.ReadState(es, ts);
                    }
                    else
                    {
                        throw new InvalidCastException("not an EntityState");
                    }
                }
            }
            else
            {
                throw new InvalidCastException("not a RoomState");
            }
        }

        protected override ResourceState MakeState(ulong ts)
        {
            return new RoomState(this, ts);
        }

        public class RoomState : ResourceState
        {
            public OnlineState[] entityStates;

            public RoomState() : base() { }
            public RoomState(RoomSession resource, ulong ts) : base(resource, ts)
            {
                entityStates = resource.entities.Select(e => e.GetState(ts, resource)).ToArray();
            }

            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                serializer.Serialize(ref entityStates);
            }

            public override StateType stateType => StateType.RoomState;
        }
    }
}
