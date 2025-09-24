namespace RainMeadow
{
    // 
    public class RealizedVultureGrubState : RealizedPhysicalObjectState
    {
        [OnlineField]
        byte bites = 3;
        [OnlineField]
        byte callingMode;
        [OnlineField]
        bool vultureCalled;
        public RealizedVultureGrubState() { }

        public RealizedVultureGrubState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var grub = (VultureGrub)onlineEntity.apo.realizedObject;

            this.bites = (byte)grub.bites;
            this.callingMode = (byte)grub.callingMode;
            this.vultureCalled = grub.vultureCalled;

        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var grub = (VultureGrub)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            grub.bites = bites;
            grub.callingMode = callingMode;
            grub.vultureCalled = vultureCalled;
        }
    }
}
