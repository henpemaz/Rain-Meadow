namespace RainMeadow
{
    // 
    public class RealizedDangleFruitState : RealizedPhysicalObjectState
    {
        [OnlineField]
        byte bites = 3;
        [OnlineField]
        bool hasStalk = true;
        public RealizedDangleFruitState() { }

        public RealizedDangleFruitState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var fruit = (DangleFruit)onlineEntity.apo.realizedObject;

            this.bites = (byte)fruit.bites;
            this.hasStalk = fruit.stalk != null;

        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var fruit = (DangleFruit)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            fruit.bites = bites;

            if (!hasStalk && fruit.stalk != null)
            {
                fruit.stalk.fruit = null;
                fruit.stalk.Destroy();
                fruit.stalk = null;
            }
        }
    }
}
