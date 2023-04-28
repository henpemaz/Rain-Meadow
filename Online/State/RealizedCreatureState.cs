namespace RainMeadow
{
    public class RealizedCreatureState : PhysicalObjectState
    {
        public RealizedCreatureState() { }
        public RealizedCreatureState(OnlineEntity onlineEntity) : base(onlineEntity)
        {
        }

        public override StateType stateType => StateType.RealizedCreatureState;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
        }
    }
}