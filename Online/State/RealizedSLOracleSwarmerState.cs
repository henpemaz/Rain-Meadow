using System.Linq;

namespace RainMeadow
{
    public class RealizedSLOracleSwarmerState : RealizedOracleSwarmerState
    {
        [OnlineField]
        bool blackMode = true;
        [OnlineField]
        bool hoverAtGrabablePos = true;

        public RealizedSLOracleSwarmerState() { }

        public RealizedSLOracleSwarmerState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var swarmer = (SLOracleSwarmer)onlineEntity.apo.realizedObject;

            this.blackMode = swarmer.blackMode > 0f;
            this.hoverAtGrabablePos = swarmer.hoverAtGrabablePos;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var swarmer = (SLOracleSwarmer)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            if (this.blackMode != swarmer.blackMode > 0f)
                swarmer.blackMode = this.blackMode ? 1f : 0f;
            swarmer.hoverAtGrabablePos = this.hoverAtGrabablePos;
        }
    }
}
