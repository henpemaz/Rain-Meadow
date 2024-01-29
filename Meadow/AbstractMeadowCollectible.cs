namespace RainMeadow
{
    public class AbstractMeadowCollectible : AbstractPhysicalObject
    {
        bool collectedLocally;
        bool collected;
        int collectedAt;
        TickReference collectedTR;
        public AbstractMeadowCollectible(World world, AbstractObjectType type, WorldCoordinate pos, EntityID ID) : base(world, type, null, pos, ID)
        {

        }

        public override void Update(int time)
        {
            base.Update(time);
            // todo expire
        }

        public void Collect()
        {
            if(collected) { return; }
            collected = true;
            collectedAt = world.game.clock;
            collectedTR = world.GetResource().owner.MakeTickReference();
        }

        public override void Realize()
        {
            if (this.realizedObject != null)
            {
                return;
            }
            if (type == RainMeadow.Ext_PhysicalObjectType.MeadowPlant)
            {
                this.realizedObject = new MeadowPlant(this);
            }
        }
    }
}
