namespace RainMeadow
{
    public class PlayerStateState : CreatureStateState
    {
        [OnlineField]
        public int foodInStomach;
        [OnlineField]
        public int quarterFoodPoints;
        [OnlineField(nullable: true)]
        public string swallowedItem;

        public PlayerStateState() { }

        public PlayerStateState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            var abstractCreature = (AbstractCreature)onlineEntity.apo;
            var playerState = (PlayerState)abstractCreature.state;

            foodInStomach = playerState.foodInStomach;
            quarterFoodPoints = playerState.quarterFoodPoints;
            swallowedItem = playerState.swallowedItem;
        }

        public override void ReadTo(AbstractCreature abstractCreature)
        {
            base.ReadTo(abstractCreature);
            var playerState = (PlayerState)abstractCreature.state;

            playerState.foodInStomach = this.foodInStomach;
            playerState.quarterFoodPoints = this.quarterFoodPoints;
            playerState.swallowedItem = this.swallowedItem;
        }

    }
}