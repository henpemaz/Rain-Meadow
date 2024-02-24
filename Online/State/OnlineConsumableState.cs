namespace RainMeadow
{
    public class OnlineConsumableState : PhysicalObjectEntityState
    {
        [OnlineField]
        bool isConsumed;

        public OnlineConsumableState() { }

        public OnlineConsumableState(OnlineConsumable onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
        {
            isConsumed = onlineEntity.Consumable.isConsumed;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var onlineConsumable = onlineEntity as OnlineConsumable;
            if (!onlineConsumable.Consumable.isConsumed && isConsumed) onlineConsumable.Consumable.Consume();
        }
    }
}
