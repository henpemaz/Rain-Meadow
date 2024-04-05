namespace RainMeadow
{
    // todo support serialization so can be sent around by definitions
    // apofs much
    public class AbstractMeadowCollectible : AbstractPhysicalObject
    {
        bool collected;
        bool collectedLocally;
        int collectedAt;
        TickReference collectedTR;
        const int duration = 40 * 30;
        public bool placed;

        internal bool Expired => collected && world.game.clock > collectedAt + duration;

        public AbstractMeadowCollectible(World world, AbstractObjectType type, WorldCoordinate pos, EntityID ID, bool placed) : base(world, type, null, pos, ID)
        {
            this.placed = placed;
        }

        public override void Update(int time)
        {
            base.Update(time);
            
            if (Expired && OnlinePhysicalObject.map.TryGetValue(this, out var entity) && entity.isMine)
            {
                this.Destroy();
            }
        }

        public void Collect()
        {
            collectedLocally = true;
            if(collected) { return; }
            collected = true;
            collectedAt = world.game.clock;
            collectedTR = world.GetResource().owner.MakeTickReference();
        }

        public override void Realize()
        {
            base.Realize(); // important for hooks!
            if (this.realizedObject != null)
            {
                return;
            }
            if (type == RainMeadow.Ext_PhysicalObjectType.MeadowPlant)
            {
                this.realizedObject = new MeadowPlant(this);
            }
            if (type == RainMeadow.Ext_PhysicalObjectType.MeadowToken)
            {
                this.realizedObject = new MeadowCollectToken(this);
            }
        }
    }
}
