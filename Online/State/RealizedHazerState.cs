namespace RainMeadow
{
    public class RealizedHazerState : RealizedPhysicalObjectState
    {
        [OnlineField]
        byte bites = 3;
        public RealizedHazerState() { }

        public RealizedHazerState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var grub = (Hazer)onlineEntity.apo.realizedObject;

            this.bites = (byte)grub.bites;

        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var grub = (Hazer)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            grub.bites = bites;
        }
    }
}
