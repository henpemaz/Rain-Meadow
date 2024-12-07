namespace RainMeadow
{
    public class RealizedDropBugState : RealizedCreatureState
    {
        [OnlineField]
        WorldCoordinate ceilingPos;
        [OnlineField]
        WorldCoordinate stayAwayFromPos;

        public RealizedDropBugState() { }
        public RealizedDropBugState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            DropBug bug = (DropBug)onlineEntity.apo.realizedObject;
            ceilingPos = bug.AI.ceilingModule.ceilingPos;
            stayAwayFromPos = bug.AI.ceilingModule.stayAwayFromPos;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if ((onlineEntity as OnlineCreature).apo.realizedObject is not DropBug bug) { RainMeadow.Error("target not realized: " + onlineEntity); return; }

            bug.AI.ceilingModule.ceilingPos = ceilingPos;
            bug.AI.ceilingModule.stayAwayFromPos = stayAwayFromPos;
        }
    }
}

