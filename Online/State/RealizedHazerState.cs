namespace RainMeadow
{
    public class RealizedHazerState : RealizedPhysicalObjectState
    {
        [OnlineField]
        byte bites = 3;
        public RealizedHazerState() { }

        public RealizedHazerState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var hazer = (Hazer)onlineEntity.apo.realizedObject;

            this.bites = (byte)hazer.bites;

        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var hazer = (Hazer)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            hazer.bites = bites;
        }
    }
}
