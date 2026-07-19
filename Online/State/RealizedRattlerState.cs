using System.Linq;
using UnityEngine;
using Watcher;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class RealizedRattlerState : RealizedCreatureState
    {
        [OnlineField]
        RattlerArmState[] arms;
        public RealizedRattlerState() { }
        public RealizedRattlerState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            var rattler = (Rattler)onlineEntity.apo.realizedObject;

            arms = rattler.arms.Select(x => new RattlerArmState(x)).ToArray();
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var rattler = (Rattler)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;

            for(int i = 0; i < arms.Length; i++)
            {
                arms[i].ReadTo(rattler.arms[i]);
            }
        }
    }

    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class RattlerArmState : OnlineState
    {
        [OnlineFieldHalf]
        Vector2 pos;
        [OnlineField]
        Rattler.Arm.Task task;
        [OnlineField(group = "counters")]
        int taskTimer;
        [OnlineFieldHalf]
        float pullIntoShell;
        public RattlerArmState() { }
        public RattlerArmState(Rattler.Arm arm)
        {
            pos = arm.pos;
            task = arm.CurrentTask ?? Rattler.Arm.Task.None;
            taskTimer = arm.taskTimer;
            pullIntoShell = arm.pullIntoShell;
        }

        public void ReadTo(Rattler.Arm arm)
        {
            arm.pos = pos;
            arm.taskTimer = taskTimer;
            if (arm.CurrentTask != task)
            {
                arm.SwitchTask(task);
            }
            arm.pullIntoShell = pullIntoShell;
        }
    }
}
