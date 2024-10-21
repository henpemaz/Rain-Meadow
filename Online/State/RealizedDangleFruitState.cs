namespace RainMeadow
{
    // 
    public class RealizedDangleFruitState : RealizedPhysicalObjectState
    {
        [OnlineField]
        byte bites = 3;
        public RealizedDangleFruitState() { }

        public RealizedDangleFruitState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var fruit = (DangleFruit)onlineEntity.apo.realizedObject;

            this.bites = (byte)fruit.bites;

        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var fruit = (DangleFruit)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            fruit.bites = bites;
        }
    }
}
