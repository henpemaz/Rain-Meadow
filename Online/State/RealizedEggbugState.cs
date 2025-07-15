namespace RainMeadow
{
    public class RealizedEggBugState : RealizedCreatureState
    {
        [OnlineField]
        public int eggsLeft;

        public RealizedEggBugState() { }
        public RealizedEggBugState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            var bug = (EggBug)onlineEntity.apo.realizedObject;
            eggsLeft = bug.eggsLeft;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if ((onlineEntity as OnlineCreature).apo.realizedObject is not EggBug bug)
            {
                RainMeadow.Error("target not realized: " + onlineEntity);
                return;
            }
            bug.eggsLeft = eggsLeft;
        }
    }
}
