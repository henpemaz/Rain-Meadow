using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.None)]
    //[DeltaSupport(level = StateHandler.DeltaSupport.FollowsContainer)]  // why does this zero out after a while?
    public class DaddyTentacleState : OnlineState, IEquatable<DaddyTentacleState>
    {
        [OnlineFieldHalf]
        public float health;
        [OnlineField]
        public Vector2 pos;
        [OnlineField(nullable = true)]
        public Vector2? floatGrabDest;
        [OnlineField]
        public DaddyTentacle.Task task;
        [OnlineField(nullable = true)]
        public BodyChunkRef? grabChunk;

        public DaddyTentacleState() { }
        public DaddyTentacleState(DaddyTentacle tentacle)
        {
            health = ((DaddyLongLegs.DaddyState)tentacle.daddy.State)?.tentacleHealth?[tentacle.tentacleNumber] ?? 1f;
            pos = tentacle.Tip.pos;
            floatGrabDest = tentacle.floatGrabDest;
            task = tentacle.task ?? DaddyTentacle.Task.Locomotion;
            grabChunk = BodyChunkRef.FromBodyChunk(tentacle.grabChunk);
            //RainMeadow.Debug($"daddytentacle[{tentacle.tentacleNumber}]: {health} {pos} {floatGrabDest} {task} {grabChunk}");
        }

        public void ReadTo(DaddyTentacle tentacle)
        {
            //RainMeadow.Debug($"daddytentacle[{tentacle.tentacleNumber}]: {health} {pos} {floatGrabDest} {task} {grabChunk}");
            ((DaddyLongLegs.DaddyState)tentacle.daddy.State).tentacleHealth[tentacle.tentacleNumber] = health;
            tentacle.Tip.pos = pos;
            tentacle.floatGrabDest = floatGrabDest;
            tentacle.task = task;
            tentacle.grabChunk = grabChunk?.ToBodyChunk();
        }

        public bool Equals(DaddyTentacleState other) => other is not null
            && Mathf.Abs(health - other.health) < Mathf.Abs(health * .0001f)
            && pos.CloseEnough(other.pos, 1/4f)
            && task == other.task
            && grabChunk == other.grabChunk;

        public override bool Equals(object obj) => obj is DaddyTentacleState other && Equals(other);

        public static bool operator ==(DaddyTentacleState lhs, DaddyTentacleState rhs) => lhs is not null && lhs.Equals(rhs);

        public static bool operator !=(DaddyTentacleState lhs, DaddyTentacleState rhs) => !(lhs == rhs);

        public override int GetHashCode() => health.GetHashCode() ^ pos.GetHashCode() ^ task.GetHashCode() ^ (grabChunk?.GetHashCode() ?? 0);
    }

    public class RealizedDaddyLongLegsState : RealizedCreatureState
    {
        [OnlineField]
        public DaddyTentacleState[] tentacles;
        [OnlineField]
        public Vector2 moveDirection;

        public RealizedDaddyLongLegsState() { }
        public RealizedDaddyLongLegsState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            DaddyLongLegs dll = (DaddyLongLegs)onlineEntity.realizedCreature;
            tentacles = dll.tentacles.Select(x => new DaddyTentacleState(x)).ToArray();
            moveDirection = dll.moveDirection;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if ((onlineEntity as OnlineCreature).apo.realizedObject is not DaddyLongLegs dll) { RainMeadow.Error("target not realized: " + onlineEntity); return; }

            for (var i = 0; i < tentacles.Length; i++) tentacles[i].ReadTo(dll.tentacles[i]);
            dll.moveDirection = moveDirection;
        }
    }
}

