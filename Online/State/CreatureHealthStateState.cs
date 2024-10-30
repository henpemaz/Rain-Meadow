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
        public override void ReadTo(AbstractCreature abstractCreature)
        {
            base.ReadTo(abstractCreature);
            var healthState = (HealthState)abstractCreature.state;
            if (healthState.health != this.health) RainMeadow.Debug($"health changed from {healthState.health} to {health}");
            healthState.health = this.health;
        }
    }
}