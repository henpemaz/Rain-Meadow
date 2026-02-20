namespace RainMeadow
{
    public class SandGrubStateState : CreatureStateState
    {
        [OnlineField]
        int origRoom;
        [OnlineField]
        int placedObjectIndex;

        public SandGrubStateState() {}

        public SandGrubStateState(OnlineCreature onlineCreature) : base(onlineCreature)
        {
            var abstractCreature = (AbstractCreature)onlineCreature.apo;
            var sandgrubState = (SandGrubState)abstractCreature.state;

            origRoom = sandgrubState.origRoom;
            placedObjectIndex = sandgrubState.placedObjectIndex;
        }

        public override void ReadTo(AbstractCreature abstractCreature)
        {
            base.ReadTo(abstractCreature);

            var sandgrubState = (SandGrubState)abstractCreature.state;

            sandgrubState.origRoom = origRoom;
            sandgrubState.placedObjectIndex = placedObjectIndex;
        }
    }
}
