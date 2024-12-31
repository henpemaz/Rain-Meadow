using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RainMeadow
{
    public class OnlineScavengerOutpost : OnlineEntity
    {
        public readonly ScavengerOutpost scavengerOutpost;

        public RoomSession? roomSession => this.currentlyJoinedResource as RoomSession; // shorthand

        public static ConditionalWeakTable<ScavengerOutpost, OnlineScavengerOutpost> map = new();

        public class Definition : EntityDefinition
        {
            [OnlineField]
            protected int index;

            public Definition() { }
            public Definition(OnlineScavengerOutpost oso, OnlineResource inResource) : base(oso, inResource)
            {
                index = 0;
                foreach (var scavengerOutpost in oso.scavengerOutpost.room.updateList.OfType<ScavengerOutpost>())
                {
                    if (scavengerOutpost == oso.scavengerOutpost) break;
                    index++;
                }
            }

            public override OnlineEntity MakeEntity(OnlineResource inResource, OnlineEntity.EntityState initialState)
            {
                return new OnlineScavengerOutpost(this, inResource, (State)initialState);
            }

            public ScavengerOutpost GetScavengerOutpost(Room room) => room.updateList.OfType<ScavengerOutpost>().ElementAtOrDefault(index);
        }

        public OnlineScavengerOutpost(ScavengerOutpost scavengerOutpost) : this(
            scavengerOutpost,
            new OnlineEntity.EntityId(
                OnlineManager.mePlayer.inLobbyId,
                EntityId.IdType.uad,
                (scavengerOutpost.room.abstractRoom.index << 16) | scavengerOutpost.room.updateList.IndexOfOrLength(scavengerOutpost)),
            OnlineManager.mePlayer,
            true)
        {
        }

        public OnlineScavengerOutpost(ScavengerOutpost scavengerOutpost, EntityId id, OnlinePlayer owner, bool isTransferable) : base(id, owner, isTransferable)
        {
            this.scavengerOutpost = scavengerOutpost;
            map.Add(scavengerOutpost, this);
        }

        public OnlineScavengerOutpost(Definition entityDefinition, OnlineResource inResource, State initialState) : base(entityDefinition, inResource, initialState)
        {
            this.scavengerOutpost = entityDefinition.GetScavengerOutpost((inResource as RoomSession).absroom.realizedRoom);
            map.Add(scavengerOutpost, this);
        }

        internal override EntityDefinition MakeDefinition(OnlineResource onlineResource)
        {
            return new Definition(this, onlineResource);
        }

        [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
        public class PearlStringState : OnlineState
        {
            [OnlineField(nullable = true)]
            Generics.DynamicOrderedEntityIDs pearls;
            [OnlineField]
            List<bool> activeConnections;

            public PearlStringState() { }
            public PearlStringState(ScavengerOutpost.PearlString pearlString)
            {
                pearls = new(pearlString.pearls.Select(x => x.GetOnlineObject()?.id).OfType<OnlineEntity.EntityId>().ToList());
                activeConnections = pearlString.activeConnections.ToList();
            }

            public void ReadTo(ScavengerOutpost.PearlString pearlString)
            {
                pearlString.pearls = pearls.list.Select(x => (x.FindEntity() as OnlinePhysicalObject)?.apo as AbstractConsumable).ToList();
                pearlString.activeConnections = activeConnections.ToList();
            }
        }

        public class State : EntityState
        {
            [OnlineField]
            PearlStringState[] pearlStrings;

            public State() { }
            public State(OnlineScavengerOutpost onlineScavengerOutpost, OnlineResource inResource, uint ts) : base(onlineScavengerOutpost, inResource, ts)
            {
                var scavengerOutpost = onlineScavengerOutpost.scavengerOutpost;
                pearlStrings = scavengerOutpost.pearlStrings.Select(x => new PearlStringState(x)).ToArray();
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
                if ((onlineEntity as OnlineScavengerOutpost)?.scavengerOutpost is not ScavengerOutpost scavengerOutpost) { RainMeadow.Error("target is wrong type: " + onlineEntity); return; }

                for (var i = 0; i < pearlStrings.Length; i++) pearlStrings[i].ReadTo(scavengerOutpost.pearlStrings[i]);
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
            return $"{scavengerOutpost} {base.ToString()}";
        }
    }
}
