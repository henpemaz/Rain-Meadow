namespace RainMeadow
{
    public class RealizedHazerState : RealizedPhysicalObjectState
    {
        [OnlineField]
        byte bites = 3;
        [OnlineField]
        bool spraying;
        public RealizedHazerState() { }

        public RealizedHazerState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var hazer = (Hazer)onlineEntity.apo.realizedObject;

            this.bites = (byte)hazer.bites;
            this.spraying = hazer.spraying;

        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var hazer = (Hazer)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            hazer.bites = bites;
            hazer.spraying = spraying;
        }
    }
}
