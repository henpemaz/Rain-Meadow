using Watcher;

namespace RainMeadow
{
    public class RealizedFireSpriteLarva : RealizedPhysicalObjectState
    {
        [OnlineField]
        byte bites = 3;
        [OnlineField]
        bool edible;
        public RealizedFireSpriteLarva() { }
        public RealizedFireSpriteLarva(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var larva = (BoxWorm.Larva)onlineEntity.apo.realizedObject;

            bites = (byte)larva.bites;
            edible = larva.edible;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var larva = (BoxWorm.Larva)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;

            larva.bites = bites;
            larva.edible = edible;
        }
    }
}
