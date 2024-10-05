using UnityEngine;

namespace RainMeadow
{
    public class RealizedTubeWormState : RealizedCreatureState
    {
        [OnlineField]
        TongueState tongue0;
        [OnlineField]
        TongueState tongue1;
        [OnlineFieldHalf]
        float goalOnRopePos;
        [OnlineFieldHalf]
        float onRopePos;
        [OnlineField]
        bool sleeping;

        public RealizedTubeWormState() { }
        public RealizedTubeWormState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            TubeWorm tubeWorm = onlineEntity.apo.realizedObject as TubeWorm;
            tongue0 = new TongueState(tubeWorm.tongues[0]);
            tongue1 = new TongueState(tubeWorm.tongues[1]);
            goalOnRopePos = tubeWorm.goalOnRopePos;
            onRopePos = tubeWorm.onRopePos;
            sleeping = tubeWorm.sleeping;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if ((onlineEntity as OnlineCreature).apo.realizedObject is TubeWorm tubeWorm)
            {
                tongue0.ReadTo(tubeWorm.tongues[0]);
                tongue1.ReadTo(tubeWorm.tongues[1]);
                tubeWorm.goalOnRopePos = goalOnRopePos;
                tubeWorm.onRopePos = onRopePos;
                tubeWorm.sleeping = sleeping;
            }
            else
            {
                RainMeadow.Error("target not realized: " + onlineEntity);
            }
        }
    }
}

