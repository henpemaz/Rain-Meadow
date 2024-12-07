
namespace RainMeadow
{
    // this class basically exists to avoid a switch in opo.makestate. worth the trouble? idk
    public class OnlineMeadowCollectible : OnlinePhysicalObject
    {
        public class Definition : OnlinePhysicalObjectDefinition
        {
            public Definition() { }

            public Definition(OnlineMeadowCollectible onlineMeadowCollectible, OnlineResource inResource) : base(onlineMeadowCollectible, inResource) { }

            public override OnlineEntity MakeEntity(OnlineResource inResource, EntityState initialState)
            {
                return new OnlineMeadowCollectible(this, inResource, (MeadowCollectibleState)initialState);
            }
        }

        public OnlineMeadowCollectible(AbstractPhysicalObject apo, EntityId id, OnlinePlayer owner, bool isTransferable) : base(apo, id, owner, isTransferable)
        {

        }

        public OnlineMeadowCollectible(Definition definition, OnlineResource inResource, MeadowCollectibleState initialState) : base(definition, inResource, initialState)
        {

        }

        internal override EntityDefinition MakeDefinition(OnlineResource onlineResource)
        {
            return new Definition(this, onlineResource);
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            if (apo.type == RainMeadow.Ext_PhysicalObjectType.MeadowGhost)
            {
                return new MeadowGhostState(this, inResource, tick);
            }
            return new MeadowCollectibleState(this, inResource, tick);
        }

        public class MeadowCollectibleState : AbstractPhysicalObjectState
        {
            [OnlineField]
            private bool placed;
            [OnlineField]
            private bool collected;
            [OnlineField(nullable = true)]
            private TickReference collectedTR;

            public MeadowCollectibleState() : base() { }
            public MeadowCollectibleState(OnlinePhysicalObject onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
            {
                var collectible = onlineEntity.apo as AbstractMeadowCollectible;
                placed = collectible.placed;
                collected = collectible.collected;
                collectedTR = collectible.collectedTR;
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
                var collectible = (onlineEntity as OnlinePhysicalObject).apo as AbstractMeadowCollectible;

                collectible.placed = this.placed;
                if (!collectible.collected && this.collected)
                {
                    collectible.collected = this.collected;
                    collectible.collectedTR = this.collectedTR;
                    collectible.collectedAt = collectible.world.game.clock - (int)(40 * collectedTR.TimeSinceTick());
                }
            }
        }

        public class MeadowGhostState : MeadowCollectibleState
        {
            [OnlineField]
            private byte currentCount;

            public MeadowGhostState() : base() { }
            public MeadowGhostState(OnlinePhysicalObject onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
            {
                var ghost = onlineEntity.apo as AbstractMeadowGhost;
                currentCount = (byte)ghost.currentCount;
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
                var ghost = (onlineEntity as OnlinePhysicalObject).apo as AbstractMeadowGhost;
                ghost.currentCount = currentCount;
            }
        }
    }
}
