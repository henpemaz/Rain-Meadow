using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class RealizedOracleState : RealizedPhysicalObjectState
    {
        [OnlineField(nullable = true)]
        public MoreSlugcats.STOracleBehavior.Phase phase;

        [OnlineField]
        public Vector2 lookPoint;

        [OnlineField(nullable = true)]
        public Generics.DynamicOrderedEntityIDs mySwarmers;

        public RealizedOracleState() { }

        public RealizedOracleState(OnlinePhysicalObject onlineEntity)
            : base(onlineEntity)
        {
            var oracle = (Oracle)onlineEntity.apo.realizedObject;
            this.lookPoint = oracle.oracleBehavior.lookPoint;
            if (oracle.ID == MoreSlugcats.MoreSlugcatsEnums.OracleID.ST)
            {
                phase = (oracle.oracleBehavior as MoreSlugcats.STOracleBehavior).curPhase;
            }

            mySwarmers = new(
                oracle
                    .mySwarmers.Select(x => x?.abstractPhysicalObject?.GetOnlineObject()?.id)
                    .OfType<OnlineEntity.EntityId>()
                    .ToList()
            );
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var oracle = (Oracle)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            if (oracle == null)
                return;
            if (oracle.ID == MoreSlugcats.MoreSlugcatsEnums.OracleID.ST)
            {
                (oracle.oracleBehavior as MoreSlugcats.STOracleBehavior).curPhase =
                    phase;
            }
            oracle.oracleBehavior.lookPoint = this.lookPoint;

            oracle.mySwarmers = this
                .mySwarmers.list.Select(x =>
                    (x.FindEntity() as OnlinePhysicalObject)?.apo.realizedObject as OracleSwarmer
                )
                .Where(x => x is not null)
                .ToList();
        }
    }
}
