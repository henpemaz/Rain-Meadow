using MoreSlugcats;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RainMeadow
{
    public class OnlineHangingPearlString : OnlineEntity
    {
        public readonly HangingPearlString hangingPearlString;

        public RoomSession? roomSession => this.currentlyJoinedResource as RoomSession; // shorthand

        public static ConditionalWeakTable<HangingPearlString, OnlineHangingPearlString> map = new();

        public class Definition : EntityDefinition
        {
            [OnlineField]
            protected int index;

            public Definition() { }
            public Definition(OnlineHangingPearlString oso, OnlineResource inResource) : base(oso, inResource)
            {
                index = 0;
                foreach (var hangingPearlString in oso.hangingPearlString.room.updateList.OfType<HangingPearlString>())
                {
                    if (hangingPearlString == oso.hangingPearlString) break;
                    index++;
                }
            }

            public override OnlineEntity MakeEntity(OnlineResource inResource, OnlineEntity.EntityState initialState)
            {
                return new OnlineHangingPearlString(this, inResource, (State)initialState);
            }

            public HangingPearlString GetHangingPearlString(Room room) => room.updateList.OfType<HangingPearlString>().ElementAtOrDefault(index);
        }

        public OnlineHangingPearlString(HangingPearlString hangingPearlString) : this(
            hangingPearlString,
            new OnlineEntity.EntityId(
                OnlineManager.mePlayer.inLobbyId,
                EntityId.IdType.uad,
                (hangingPearlString.room.abstractRoom.index << 16) | hangingPearlString.room.updateList.IndexOfOrLength(hangingPearlString)),
            OnlineManager.mePlayer,
            true)
        {
        }

        public OnlineHangingPearlString(HangingPearlString hangingPearlString, EntityId id, OnlinePlayer owner, bool isTransferable) : base(id, owner, isTransferable)
        {
            this.hangingPearlString = hangingPearlString;
            map.Add(hangingPearlString, this);
        }

        public OnlineHangingPearlString(Definition entityDefinition, OnlineResource inResource, State initialState) : base(entityDefinition, inResource, initialState)
        {
            this.hangingPearlString = entityDefinition.GetHangingPearlString((inResource as RoomSession).absroom.realizedRoom);
            map.Add(hangingPearlString, this);
        }

        internal override EntityDefinition MakeDefinition(OnlineResource onlineResource)
        {
            return new Definition(this, onlineResource);
        }

        public class State : EntityState
        {
            [OnlineField(nullable = true)]
            Generics.DynamicOrderedEntityIDs pearls;
            [OnlineField]
            List<bool> activeConnections;

            public State() { }
            public State(OnlineHangingPearlString onlineHangingPearlString, OnlineResource inResource, uint ts) : base(onlineHangingPearlString, inResource, ts)
            {
                var hangingPearlString = onlineHangingPearlString.hangingPearlString;
                pearls = new(hangingPearlString.pearls.Select(x => x.GetOnlineObject()?.id).OfType<OnlineEntity.EntityId>().ToList());
                activeConnections = hangingPearlString.activeConnections.ToList();
            }

            public void ReadTo(HangingPearlString hangingPearlString)
            {
                hangingPearlString.pearls = pearls.list.Select(x => (x.FindEntity() as OnlinePhysicalObject)?.apo as AbstractConsumable).ToList();
                hangingPearlString.activeConnections = activeConnections.ToList();
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
                if ((onlineEntity as OnlineHangingPearlString)?.hangingPearlString is not HangingPearlString hangingPearlString) { RainMeadow.Error("target is wrong type: " + onlineEntity); return; }

                ReadTo(hangingPearlString);
            }
        }

        public override void ReadState(EntityState entityState, OnlineResource inResource)
        {
            base.ReadState(entityState, inResource);
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new State(this, inResource, tick);
        }

        protected override void JoinImpl(OnlineResource inResource, EntityState initialState)
        {
            RainMeadow.Debug($"{this} joining {inResource}");
            base.JoinImpl(inResource, initialState);
        }

        protected override void LeaveImpl(OnlineResource inResource)
        {
            RainMeadow.Debug($"{this} leaving {inResource}");
            base.LeaveImpl(inResource);
        }

        public override void Deregister()
        {
            base.Deregister();
        }

        public override string ToString()
        {
            return $"{hangingPearlString} {base.ToString()}";
        }
    }
}
