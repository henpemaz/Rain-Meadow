namespace RainMeadow
{
    public class PlayerStateState : CreatureStateState
    {
        [OnlineField]
        public int foodInStomach;
        [OnlineField]
        public int quarterFoodPoints;
        [OnlineField]
        private bool isPup;
        [OnlineFieldHalf]
        public float permanentDamageTracking;

        [OnlineField]
        public OnlineEntity.EntityId? objectInStomach;

        public PlayerStateState() { }

        public PlayerStateState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            var abstractCreature = (AbstractCreature)onlineEntity.apo;
            var playerState = (PlayerState)abstractCreature.state;

            foodInStomach = playerState.foodInStomach;
            quarterFoodPoints = playerState.quarterFoodPoints;
            permanentDamageTracking = (float)playerState.permanentDamageTracking;
            isPup = playerState.isPup;

            if ((abstractCreature.realizedCreature as Player)?.objectInStomach is AbstractPhysicalObject apo)
            {
                // signal not-in-a-room
                apo.InDen = true;
                apo.pos.WashNode();
                if (apo.realizedObject != null) apo.Abstractize(abstractCreature.pos);
                if (!OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
                {
                    apo.world.GetResource().ApoEnteringWorld(apo);
                    if (!OnlinePhysicalObject.map.TryGetValue(apo, out oe)) throw new System.InvalidOperationException("Stomach item doesn't exist in online space!");
                }
                objectInStomach = oe.id;
            }
            else
            {
                objectInStomach = null;
            }
        }

        public override void ReadTo(AbstractCreature abstractCreature)
        {
            base.ReadTo(abstractCreature);
            var playerState = (PlayerState)abstractCreature.state;

            playerState.foodInStomach = this.foodInStomach;
            playerState.quarterFoodPoints = this.quarterFoodPoints;
            playerState.permanentDamageTracking = (double)this.permanentDamageTracking;

            if (playerState.isPup != isPup)
                playerState.isPup = isPup;

            if (abstractCreature.realizedCreature is Player player)
            {
                player.objectInStomach = (this.objectInStomach?.FindEntity() as OnlinePhysicalObject)?.apo;
            }
        }
    }
}
