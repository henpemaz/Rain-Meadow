using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static RainMeadow.OnlineState;

namespace RainMeadow;

[DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
public class RealizedBigNeedleWormState : RealizedCreatureState
{
    [OnlineField(nullable = true)]
    BodyChunkRef? impaleChunk;
    [OnlineFieldHalf(nullable = true)]
    Vector2? stuckInWallPos;
    [OnlineFieldHalf]
    Vector2 stuckDir;
    [OnlineFieldHalf(group = "counters")]
    float attackReady;
    [OnlineFieldHalf(group = "counters")]
    float chargingAttack;
    public RealizedBigNeedleWormState() { }

    public RealizedBigNeedleWormState(OnlineCreature onlineEntity) : base (onlineEntity)
    {
        BigNeedleWorm bigNeedle = (BigNeedleWorm)onlineEntity.realizedCreature;

        impaleChunk = BodyChunkRef.FromBodyChunk(bigNeedle.impaleChunk);
        stuckInWallPos = bigNeedle.stuckInWallPos;
        stuckDir = bigNeedle.stuckDir;

        attackReady = bigNeedle.attackReady;
        chargingAttack = bigNeedle.chargingAttack;
    }

    public override void ReadTo(OnlineEntity onlineEntity)
    {
        base.ReadTo(onlineEntity);

        if ((onlineEntity as OnlinePhysicalObject).apo.realizedObject is not BigNeedleWorm bigNeedle) return;

        bigNeedle.impaleChunk = impaleChunk?.ToBodyChunk();
        bigNeedle.stuckInWallPos = stuckInWallPos;
        bigNeedle.stuckDir = stuckDir;

        bigNeedle.attackReady = attackReady;
        bigNeedle.chargingAttack = chargingAttack;
    }
}
[DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
public class RealizedSmallNeedleWormState : RealizedCreatureState
{
    [OnlineField]
    int bites = 3;
    public RealizedSmallNeedleWormState() { }
    public RealizedSmallNeedleWormState(OnlineCreature onlineEntity) : base(onlineEntity)
    {
        SmallNeedleWorm smallNeedle = (SmallNeedleWorm)onlineEntity.realizedCreature;

        bites = smallNeedle.bites;
    }

    public override void ReadTo(OnlineEntity onlineEntity)
    {
        base.ReadTo(onlineEntity);

        if ((onlineEntity as OnlinePhysicalObject).apo.realizedObject is not SmallNeedleWorm smallNeedle) return;

        smallNeedle.bites = bites;
    }
}
