namespace RainMeadow
{
    public class CreatureHealthStateState : CreatureStateState
    {
        [OnlineField]
        public float health;

        public CreatureHealthStateState() { }
        public CreatureHealthStateState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            var abstractCreature = (AbstractCreature)onlineEntity.apo;
            var healthState = (HealthState)abstractCreature.state;
            health = healthState.health;
        }
    }
}