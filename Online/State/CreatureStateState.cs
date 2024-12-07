namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.FollowsContainer)]
    public class CreatureStateState : OnlineState
    {
        // main part of AbstractCreatureState
        [OnlineField]
        public bool alive;
        [OnlineField]
        public byte meatLeft;

        public CreatureStateState() { }
        public CreatureStateState(OnlineCreature onlineEntity)
        {
            var abstractCreature = (AbstractCreature)onlineEntity.apo;
            alive = abstractCreature.state.alive;
            meatLeft = (byte)abstractCreature.state.meatLeft;
        }

        public virtual void ReadTo(AbstractCreature abstractCreature)
        {
            abstractCreature.state.meatLeft = this.meatLeft;
            abstractCreature.state.alive = this.alive;
            if (abstractCreature.realizedCreature is Creature creature)
            {
                if (alive && creature.dead)
                {
                    if (creature is BigSpider spoder && spoder.CanIBeRevived) spoder.Revive();
                    else
                    {
                        creature.dead = false;
                        abstractCreature.state.alive = this.alive;
                    }
                }
                if (!alive && !creature.dead)
                {
                    creature.Die();
                    creature.dead = !this.alive;
                }
            }
        }
    }
}