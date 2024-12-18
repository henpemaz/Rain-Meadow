namespace RainMeadow
{
    public class RealizedScavengerBombState : RealizedWeaponState
    {

        [OnlineField]
        bool explosionIsForShow;
        [OnlineField]
        bool ignited;

        public RealizedScavengerBombState() { }
        public RealizedScavengerBombState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var scavBomb = (ScavengerBomb)onlineEntity.apo.realizedObject;
            explosionIsForShow = scavBomb.explosionIsForShow;
            ignited = scavBomb.ignited;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var scavBomb = (ScavengerBomb)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            scavBomb.explosionIsForShow = explosionIsForShow;
            scavBomb.ignited = ignited;
        }
    }
}
