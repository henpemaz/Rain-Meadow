using UnityEngine;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class RealizedVultureState : RealizedCreatureState
    {

        [OnlineField]
        TentacleState neck;

        [OnlineField(nullable = true)]
        BodyChunkRef? snapAt;

        // KING
        [OnlineField(nullable = true)]
        KingTusksState? kingTusks;

        public RealizedVultureState() { }
        public RealizedVultureState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            Vulture vulture = (Vulture)onlineEntity.apo.realizedObject;

            neck = new(vulture.neck);
            snapAt = BodyChunkRef.FromBodyChunk(vulture.snapAt) ?? null;

            if (vulture.kingTusks != null)
            {
                kingTusks = new(vulture.kingTusks);
            }
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            if (((OnlineCreature)onlineEntity).apo.realizedObject is not Vulture vulture) { RainMeadow.Error("target not realized: " + onlineEntity); return; }

            neck.ReadTo(vulture.neck);
            vulture.snapAt = snapAt?.ToBodyChunk();

            if (vulture.kingTusks != null) kingTusks?.ReadTo(vulture.kingTusks);
        }
    }

    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class KingTusksState : OnlineState
    {
        [OnlineField]
        TuskState[] tusks;
        public KingTusksState() { }
        public KingTusksState(KingTusks kingTusks)
        {
            tusks = new TuskState[2];
            tusks[0] = new(kingTusks.tusks[0]);
            tusks[1] = new(kingTusks.tusks[1]);
        }

        public void ReadTo(KingTusks kingTusks)
        {
            tusks[0].ReadTo(kingTusks.tusks[0]);
            tusks[1].ReadTo(kingTusks.tusks[1]);
        }

        
    }

    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class TuskState : OnlineState
    {
        [OnlineField]
        byte mode;

        [OnlineField(nullable = true)]
        BodyChunkRef? impaleChunk;
        [OnlineFieldHalf(nullable = true)]
        private Vector2? stuckInWallPos;

        public TuskState() { }
        public TuskState(KingTusks.Tusk tusk)
        {
            mode = Mode(tusk.mode);
            stuckInWallPos = tusk.stuckInWallPos;
            impaleChunk = BodyChunkRef.FromBodyChunk(tusk.impaleChunk) ?? null;
        }

        public void ReadTo(KingTusks.Tusk tusk)
        {
            tusk.SwitchMode(Mode(mode));
            tusk.stuckInWallPos = stuckInWallPos;
            tusk.impaleChunk = impaleChunk?.ToBodyChunk();
        }

        // there has to be a better way to do this. (p.-)
        private byte Mode(KingTusks.Tusk.Mode mode)
        {
            if (mode == KingTusks.Tusk.Mode.Attached) return 1;
            if (mode == KingTusks.Tusk.Mode.Charging) return 2;
            if (mode == KingTusks.Tusk.Mode.ShootingOut) return 3;
            if (mode == KingTusks.Tusk.Mode.StuckInCreature) return 4;
            if (mode == KingTusks.Tusk.Mode.StuckInWall) return 5;
            if (mode == KingTusks.Tusk.Mode.Dangling) return 6;
            if (mode == KingTusks.Tusk.Mode.Retracting) return 7;
            return 0;
        }

        private KingTusks.Tusk.Mode? Mode(byte mode)
        {
            switch (mode)
            {
                default: return KingTusks.Tusk.Mode.Attached;
                case 2: return KingTusks.Tusk.Mode.Charging;
                case 3: return KingTusks.Tusk.Mode.ShootingOut;
                case 4: return KingTusks.Tusk.Mode.StuckInCreature;
                case 5: return KingTusks.Tusk.Mode.StuckInWall;
                case 6: return KingTusks.Tusk.Mode.Dangling;
                case 7: return KingTusks.Tusk.Mode.Retracting;
            }
        }
    }
}