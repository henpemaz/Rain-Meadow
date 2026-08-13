using Watcher;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class RealizedFireSpriteLarva : RealizedPhysicalObjectState
    {
        [OnlineField]
        byte bites = 3;
        [OnlineField]
        bool edible;

        [OnlineField(nullable = true)]

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

        public override bool ShouldPosBeLenient(PhysicalObject po)
        {
            var larva = (BoxWorm.Larva)po.abstractPhysicalObject.realizedObject;
            if (!larva.CollideWithTerrain) return true;
            return base.ShouldPosBeLenient(po);
        }
    }
}