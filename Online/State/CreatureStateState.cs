namespace RainMeadow
{
    public class CreatureStateState : OnlineState
    {
        public bool alive;
        public byte meatLeft;
        
        public CreatureStateState() { }
        public CreatureStateState(OnlineEntity onlineEntity)
        {
            var abstractCreature = (AbstractCreature)onlineEntity.entity;
            alive = abstractCreature.state.alive;
            meatLeft = (byte)abstractCreature.state.meatLeft;
        }
        
        public override StateType stateType => StateType.CreatureStateState;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref alive);
            serializer.Serialize(ref meatLeft);
        }
    }
}