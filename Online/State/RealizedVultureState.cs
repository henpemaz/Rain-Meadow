using UnityEngine;

namespace RainMeadow
{
    public class RealizedVultureState : RealizedCreatureState
    {

        [OnlineField]
        TentacleState neck;

        [OnlineField (nullable = true)]
        BodyChunkRef? snapAt;

        public RealizedVultureState() { }
        public RealizedVultureState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            Vulture vulture = (Vulture)onlineEntity.apo.realizedObject;

            neck = new(vulture.neck);
            snapAt = BodyChunkRef.FromBodyChunk(vulture.snapAt) ?? null;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            if (((OnlineCreature)onlineEntity).apo.realizedObject is not Vulture vulture) { RainMeadow.Error("target not realized: " + onlineEntity); return; }

            neck.ReadTo(vulture.neck);
            vulture.snapAt = snapAt?.ToBodyChunk();
        }
    }
}