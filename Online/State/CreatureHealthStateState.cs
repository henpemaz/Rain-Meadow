namespace RainMeadow
{
    public class CreatureHealthStateState : CreatureStateState
    {
        public float health;
        
        public CreatureHealthStateState() { }
        public CreatureHealthStateState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            var abstractCreature = (AbstractCreature)onlineEntity.apo;
            var healthState = (HealthState)abstractCreature.state;
            health = healthState.health;
        }
        
        public override StateType stateType => StateType.CreatureHealthState;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref health);
        }
    }
}