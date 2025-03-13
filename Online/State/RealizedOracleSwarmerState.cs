namespace RainMeadow
{
    public class RealizedOracleSwarmerState : RealizedPhysicalObjectState
    {
        [OnlineField]
        byte bites = 3;
        [OnlineField]
        bool floaty = true;

        public RealizedOracleSwarmerState() { }

        public RealizedOracleSwarmerState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var swarmer = (OracleSwarmer)onlineEntity.apo.realizedObject;

            this.bites = (byte)swarmer.bites;
            this.floaty = swarmer.affectedByGravity < 1f;
        }

        //neurons are half as important, simply because there are SO MANY of them in iterators
        public override float SendFrequency(Player? player) => base.SendFrequency(player) * 0.5f;

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var swarmer = (OracleSwarmer)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            swarmer.bites = bites;
            if (this.floaty != swarmer.affectedByGravity < 1f)
                swarmer.affectedByGravity = this.floaty ? 0f : 1f;
        }
    }
}
