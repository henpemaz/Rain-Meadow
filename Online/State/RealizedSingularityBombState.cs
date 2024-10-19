namespace RainMeadow
{
    public class RealizedSingularityBombState : RealizedWeaponState
    {
        [OnlineField]
        bool ignited;

        [OnlineField]
        bool activateSingularity;

        [OnlineField]
        bool activateSucktion;

        [OnlineField]
        byte counter; // float is always used as an integer with range [0, 120)

        public RealizedSingularityBombState() { }
        public RealizedSingularityBombState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var bomb = (MoreSlugcats.SingularityBomb)onlineEntity.apo.realizedObject;
            ignited = bomb.ignited;
            activateSingularity = bomb.activateSingularity;
            activateSucktion = bomb.activateSucktion;
            counter = (byte)bomb.counter;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var bomb = (MoreSlugcats.SingularityBomb)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            bomb.ignited = ignited;
            bomb.activateSingularity = activateSingularity;
            bomb.activateSucktion = activateSucktion;
            bomb.counter = counter;
        }
    }
}
