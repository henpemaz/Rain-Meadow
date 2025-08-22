namespace RainMeadow
{
    public class RealizedHazerState : RealizedPhysicalObjectState
    {
        [OnlineField]
        byte bites = 3;
        [OnlineField]
        bool spraying;
        [OnlineField]
        bool tossed;
        public RealizedHazerState() { }

        public RealizedHazerState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var hazer = (Hazer)onlineEntity.apo.realizedObject;

            this.bites = (byte)hazer.bites;
            this.spraying = hazer.spraying;
            this.tossed = hazer.tossed;

        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var hazer = (Hazer)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            hazer.bites = bites;
            hazer.tossed = tossed;
            if (hazer.spraying != spraying)
            {
                hazer.spraying = spraying;
                if (spraying)
                {
                    hazer.hasSprayed = spraying;
                }
            }
        }
    }
}
