namespace RainMeadow
{
    public class CreatureHealthStateState : CreatureStateState
    {
        public float health;

        public override CreatureStateState EmptyDelta() => new CreatureHealthStateState();
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
            if (!serializer.IsDelta || hasStateValue) // reuses same flag as parent
                serializer.Serialize(ref health);
        }


        public override CreatureStateState Delta(CreatureStateState _other)
        {
            var delta = (CreatureHealthStateState)base.Delta(_other);
            var other = (CreatureHealthStateState)_other;
            delta.health = health;
            delta.hasStateValue |= health != other.health;
            delta.IsEmptyDelta &= !delta.hasStateValue;
            return delta;
        }

        public override CreatureStateState ApplyDelta(CreatureStateState _other)
        {
            var result = (CreatureHealthStateState)base.ApplyDelta(_other);
            var other = (CreatureHealthStateState)_other;
            result.health = other.hasStateValue ? other.health : health;
            return result;
        }
    }
}