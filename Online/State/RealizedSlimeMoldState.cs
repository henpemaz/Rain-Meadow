namespace RainMeadow
{
    // 
    public class RealizedSlimeMoldState : RealizedState<SlimeMold>
    {
        [OnlineField]
        byte bites = 3;
        public RealizedSlimeMoldState() { }

        public RealizedSlimeMoldState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var slime = (SlimeMold)onlineEntity.apo.realizedObject;

            this.bites = (byte)slime.bites;

        }

        public override void ReadTo(SlimeMold slime, OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            slime.bites = bites;
        }
    }
}
