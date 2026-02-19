using System;
using System.Collections.Generic;
using System.Linq;
using RainMeadow.Generics;
using UnityEngine;

namespace RainMeadow
{
    
    public class RealizedScavengerState : RealizedCreatureState
    {

        [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
        public abstract class AnimationState : OnlineState
        {
            public AnimationState() { }
            protected AnimationState(Scavenger.ScavengerAnimation animation) { }

            public abstract Scavenger.ScavengerAnimation? ConstructAnimation(Scavenger scavenger);
            public abstract Scavenger.ScavengerAnimation.ID GetAnimationID();
            public virtual void ReadTo(Scavenger scavenger, Scavenger.ScavengerAnimation animation) { }

            static public AnimationState? GetAnimationState(Scavenger.ScavengerAnimation animation)
            {
                switch (animation)
                {
                    case Scavenger.RummageAnimation rummage:
                        return new RummageAnimationState(rummage);
                    case Scavenger.ThrowAnimation throwAnim:
                        return new ThrowAnimationState(throwAnim);
                    case Scavenger.ThrowChargeAnimation throwCharge:
                        return new ThrowChargeAnimationState(throwCharge);
                    case Scavenger.LookAnimation look:
                        return new LookAnimationState(look);
                    case Scavenger.GeneralPointAnimation generalPoint:
                        return new GeneralPointAnimationState(generalPoint);
                    case Scavenger.BackOffAnimation backOff:
                        return new BackOffAnimationState(backOff);
                    case Scavenger.PlayerMayPassAnimation playerMayPass:
                        return new PlayerMayPassAnimationState(playerMayPass);
                    case Scavenger.WantToTradeAnimation wantToTrade:
                        return new WantToTradeAnimationState(wantToTrade);
                    case Scavenger.PrepareToJumpAnimation prepareToJump:
                        return new PrepareToJumpAnimationState(prepareToJump);
                    case Scavenger.JumpingAnimation jumping:
                        return new JumpingAnimationState(jumping);
                    default:
                        return null;
                }
            }

        }

        public class RummageAnimationState : AnimationState
        {
            [OnlineField(group = "rummage")]
            public WorldCoordinate sitPos;

            public RummageAnimationState() { }
            public RummageAnimationState(Scavenger.RummageAnimation animation) : base(animation)
            {
                sitPos = animation.sitPos;
            }

            public override Scavenger.ScavengerAnimation.ID GetAnimationID() => Scavenger.ScavengerAnimation.ID.Rummage;
            public override Scavenger.ScavengerAnimation? ConstructAnimation(Scavenger scavenger)
            {
                return new Scavenger.RummageAnimation(scavenger);
            }

            public override void ReadTo(Scavenger scavenger, Scavenger.ScavengerAnimation animation)
            {
                base.ReadTo(scavenger, animation);
                var anim = animation as Scavenger.RummageAnimation;
                anim.sitPos = sitPos;
            }
        }

        public class ThrowAnimationState : AnimationState
        {
            [OnlineFieldHalf(group = "throw")]
            public float flip;

            [OnlineField(group = "throw", nullable = true)]
            public OnlineEntity.EntityId? thrownID;

            public ThrowAnimationState() { }
            public ThrowAnimationState(Scavenger.ThrowAnimation animation) : base(animation)
            {
                flip = animation.flip;
                thrownID = animation.thrownObject?.abstractPhysicalObject.GetOnlineObject()?.id;
            }

            public override Scavenger.ScavengerAnimation.ID GetAnimationID() => Scavenger.ScavengerAnimation.ID.Throw;
            public override Scavenger.ScavengerAnimation? ConstructAnimation(Scavenger scavenger)
            {
                if (thrownID?.FindEntity() is OnlinePhysicalObject opo && opo.apo.realizedObject is not null)
                {
                    return new Scavenger.ThrowAnimation(scavenger, opo.apo.realizedObject, flip); // Fill in as needed
                }

                return null;
            }

            public override void ReadTo(Scavenger scavenger, Scavenger.ScavengerAnimation animation)
            {
                base.ReadTo(scavenger, animation);
                var anim = animation as Scavenger.ThrowAnimation;
                anim.flip = flip;
            }
        }

        public class ThrowChargeAnimationState : AnimationState
        {
            [OnlineField(group = "throwcharge")]
            public bool discontinue;

            [OnlineField(group = "throwcharge", nullable = true)]
            public BodyChunkRef? aimTargetChunk;
            [OnlineFieldHalf(group = "throwcharge")]
            public Vector2 aimTarget;

            public ThrowChargeAnimationState() { }
            public ThrowChargeAnimationState(Scavenger.ThrowChargeAnimation animation) : base(animation)
            {
                discontinue = !animation.Active;
                if (animation.target is BodyChunk && animation.target.owner.abstractPhysicalObject.GetOnlineObject() is OnlinePhysicalObject opo)
                {
                    aimTargetChunk = new BodyChunkRef(opo, animation.target.index);
                }
                else
                {
                    aimTarget = animation.UseTarget;
                }
            }

            public override Scavenger.ScavengerAnimation.ID GetAnimationID() => Scavenger.ScavengerAnimation.ID.ThrowCharge;
            public override Scavenger.ScavengerAnimation? ConstructAnimation(Scavenger scavenger)
            {
                return new Scavenger.ThrowChargeAnimation(scavenger, aimTargetChunk?.ToBodyChunk()); // Fill in as needed
            }

            public override void ReadTo(Scavenger scavenger, Scavenger.ScavengerAnimation animation)
            {
                if (animation is Scavenger.ThrowChargeAnimation throw_charge)
                {
                    if (discontinue)
                    {
                        throw_charge.discontinue = Mathf.Max(throw_charge.discontinue, 20);
                    }
                    else
                    {
                        throw_charge.discontinue = 0;
                    }

                }
            }
        }

        public abstract class AttentiveAnimationState : AnimationState
        {
            
            [OnlineField(group = "attentiveAnim", nullable = true)]
            public OnlineEntity.EntityId? creature;

            [OnlineFieldHalf(group = "attentiveAnim")]
            public Vector2 point;

            [OnlineField(group = "attentiveAnim")]
            public bool stop;

            public AttentiveAnimationState() { }
            public AttentiveAnimationState(Scavenger.AttentiveAnimation animation) : base(animation)
            {
                creature = animation.creatureRep?.representedCreature?.GetOnlineCreature()?.id;
                point = animation.point;
                stop = animation.stop;
            }

            public override Scavenger.ScavengerAnimation? ConstructAnimation(Scavenger scavenger)
            {
                // AttentiveAnimation is abstract, so you should use a derived type
                return null;
            }

            public override void ReadTo(Scavenger scavenger, Scavenger.ScavengerAnimation animation)
            {
                base.ReadTo(scavenger, animation);
                var anim = animation as Scavenger.AttentiveAnimation;
                anim.point = point;
                anim.stop = stop;
                if (creature?.FindEntity() is OnlineCreature critter)
                {
                    anim.creatureRep = scavenger.AI.tracker.RepresentationForCreature(critter.abstractCreature, true);
                }
                
            }
        }

        public class LookAnimationState : AttentiveAnimationState
        {
            [OnlineFieldHalf("look")]
            public float prio;

            public LookAnimationState() { }
            public LookAnimationState(Scavenger.LookAnimation animation) : base(animation)
            {
                prio = animation.prio;
            }

            public override Scavenger.ScavengerAnimation.ID GetAnimationID() => Scavenger.ScavengerAnimation.ID.Look;
            public override Scavenger.ScavengerAnimation? ConstructAnimation(Scavenger scavenger)
            {
                Tracker.CreatureRepresentation rep = null;
                if (creature?.FindEntity() is OnlineCreature critter)
                {
                    rep = scavenger.AI.tracker.RepresentationForCreature(critter.creature, true);
                }
                
                return new Scavenger.LookAnimation(scavenger, rep, point, prio, stop);
            }

            public override void ReadTo(Scavenger scavenger, Scavenger.ScavengerAnimation animation)
            {
                base.ReadTo(scavenger, animation);
                var anim = animation as Scavenger.LookAnimation;
                anim.prio = prio;
            }
        }

        public abstract class PointingAnimationState : AttentiveAnimationState
        {
            [OnlineField("point")]
            public byte pointingArm;

            public PointingAnimationState() { }
            public PointingAnimationState(Scavenger.PointingAnimation animation) : base(animation)
            {
                pointingArm = (byte)animation.pointingArm;
            }

            public override void ReadTo(Scavenger scavenger, Scavenger.ScavengerAnimation animation)
            {
                base.ReadTo(scavenger, animation);
                var anim = animation as Scavenger.PointingAnimation;
                anim.pointingArm = pointingArm;
            }
        }

        public class GeneralPointAnimationState : PointingAnimationState
        {
            [OnlineField(group = "generalPoint", nullable = true)]
            public DynamicOrderedEntityIDs group;

            [OnlineField(group = "generalPoint")]
            byte groupStartNum;

            public GeneralPointAnimationState() { }
            public GeneralPointAnimationState(Scavenger.GeneralPointAnimation animation) : base(animation)
            {
                groupStartNum = (byte)animation.groupStartNum;
                group = new(animation.group.Select(x => x.representedCreature?.GetOnlineCreature()?.id).OfType<OnlineEntity.EntityId>().ToList());
            }

            public override Scavenger.ScavengerAnimation.ID GetAnimationID() => Scavenger.ScavengerAnimation.ID.GeneralPoint;
            public override Scavenger.ScavengerAnimation? ConstructAnimation(Scavenger scavenger)
            {
                Tracker.CreatureRepresentation rep = null;
                if (creature?.FindEntity() is OnlineCreature critter)
                {
                    rep = scavenger.AI.tracker.RepresentationForCreature(critter.creature, true);
                }
                
                List<Tracker.CreatureRepresentation> groupRep = new(group.list.Select(x => x.FindEntity()).OfType<OnlineCreature>().Select(x => scavenger.AI.tracker.RepresentationForCreature(x.creature, true)));
                return new Scavenger.GeneralPointAnimation(scavenger, rep, point, groupRep);
            }

            public override void ReadTo(Scavenger scavenger, Scavenger.ScavengerAnimation animation)
            {
                base.ReadTo(scavenger, animation);
                var anim = animation as Scavenger.GeneralPointAnimation;
                anim.groupStartNum = groupStartNum;
            }
        }

        public abstract class CommunicationAnimationState : AttentiveAnimationState
        {
            [OnlineField(group = "commAnim")]
            public int gestureArm;
            [OnlineField(group = "commAnim")]
            public bool pointWithSpears;

            public CommunicationAnimationState() { }
            public CommunicationAnimationState(Scavenger.CommunicationAnimation animation) : base(animation)
            {
                gestureArm = animation.gestureArm;
                pointWithSpears = animation.pointWithSpears;
            }

            public override void ReadTo(Scavenger scavenger, Scavenger.ScavengerAnimation animation)
            {
                base.ReadTo(scavenger, animation);
                var anim = animation as Scavenger.CommunicationAnimation;
                anim.gestureArm = gestureArm;
                anim.pointWithSpears = pointWithSpears;
            }
        }

        public class BackOffAnimationState : CommunicationAnimationState
        {

            public BackOffAnimationState() { }
            public BackOffAnimationState(Scavenger.BackOffAnimation animation) : base(animation) { }
            public override Scavenger.ScavengerAnimation.ID GetAnimationID() => Scavenger.ScavengerAnimation.ID.BackOff;
            public override Scavenger.ScavengerAnimation? ConstructAnimation(Scavenger scavenger)
            {
                Tracker.CreatureRepresentation rep = null;
                if (creature?.FindEntity() is OnlineCreature critter)
                {
                    rep = scavenger.AI.tracker.RepresentationForCreature(critter.creature, true);
                }
                
                return new Scavenger.BackOffAnimation(scavenger, rep, point);
            }

            public override void ReadTo(Scavenger scavenger, Scavenger.ScavengerAnimation animation)
            {
                base.ReadTo(scavenger, animation);
                var anim = animation as Scavenger.BackOffAnimation;
            }
        }

        public class PlayerMayPassAnimationState : CommunicationAnimationState
        {
            public PlayerMayPassAnimationState() { }
            public PlayerMayPassAnimationState(Scavenger.PlayerMayPassAnimation animation) : base(animation) { }


            public override Scavenger.ScavengerAnimation.ID GetAnimationID() => Scavenger.ScavengerAnimation.ID.PlayerMayPass;
            public override Scavenger.ScavengerAnimation? ConstructAnimation(Scavenger scavenger)
            {
                Tracker.CreatureRepresentation rep = null;
                if (creature?.FindEntity() is OnlineCreature critter)
                {
                    rep = scavenger.AI.tracker.RepresentationForCreature(critter.creature, true);
                }
                return new Scavenger.PlayerMayPassAnimation(scavenger, rep, point, null); // TODO: figure out how to reference outposts.
            }

            public override void ReadTo(Scavenger scavenger, Scavenger.ScavengerAnimation animation) => base.ReadTo(scavenger, animation);
        }

        public class WantToTradeAnimationState : CommunicationAnimationState
        {

            [OnlineField(nullable = true)]
            public OnlineEntity.EntityId? wantID;

            public WantToTradeAnimationState() { }
            public WantToTradeAnimationState(Scavenger.WantToTradeAnimation animation) : base(animation)
            {
                wantID = animation.desiredItem?.abstractPhysicalObject.GetOnlineObject()?.id;
            }
            

            public override Scavenger.ScavengerAnimation.ID GetAnimationID() => Scavenger.ScavengerAnimation.ID.WantToTrade;
            public override Scavenger.ScavengerAnimation? ConstructAnimation(Scavenger scavenger)
            {

                Tracker.CreatureRepresentation rep = null;
                if (creature?.FindEntity() is OnlineCreature critter)
                {
                    rep = scavenger.AI.tracker.RepresentationForCreature(critter.creature, true);
                }

                if (wantID?.FindEntity() is OnlinePhysicalObject want && want.apo.realizedObject is not null)
                {
                    return new Scavenger.WantToTradeAnimation(scavenger, rep, point, want.apo.realizedObject);
                }

                return null;
            }

            public override void ReadTo(Scavenger scavenger, Scavenger.ScavengerAnimation animation) => base.ReadTo(scavenger, animation);
        }

        public class PrepareToJumpAnimationState : AnimationState
        {
            [OnlineField]
            byte filler = 0; 

            public PrepareToJumpAnimationState() { }
            public PrepareToJumpAnimationState(Scavenger.PrepareToJumpAnimation animation) : base(animation) { }

            public override Scavenger.ScavengerAnimation.ID GetAnimationID() => DLCSharedEnums.ScavengerAnimationID.PrepareToJump;
            public override Scavenger.ScavengerAnimation? ConstructAnimation(Scavenger scavenger)
            {
                return new Scavenger.PrepareToJumpAnimation(scavenger, 100); // animation shouldn't end until host says it did 
            }

            public override void ReadTo(Scavenger scavenger, Scavenger.ScavengerAnimation animation) => base.ReadTo(scavenger, animation);
        }

        public class JumpingAnimationState : AnimationState
        {
            [OnlineField]
            byte filler = 0; 

            public JumpingAnimationState() { }
            public JumpingAnimationState(Scavenger.JumpingAnimation animation) : base(animation)
            {
            }

            public override Scavenger.ScavengerAnimation.ID GetAnimationID() => DLCSharedEnums.ScavengerAnimationID.Jumping;
            public override Scavenger.ScavengerAnimation? ConstructAnimation(Scavenger scavenger)
            {
                return new Scavenger.JumpingAnimation(scavenger);
            }

            public override void ReadTo(Scavenger scavenger, Scavenger.ScavengerAnimation animation)
            {
                base.ReadTo(scavenger, animation);
                // No additional fields
            }
        }


        [OnlineField(nullable = true, group = "swing")]
        private Vector2? swingPos;
        [OnlineFieldHalf(group = "swing")]
        private float swingRadius;
        [OnlineFieldHalf]
        private float flip;
        [OnlineField(group = "swing")]
        private byte swingClimbCounter;
        [OnlineField(group = "swing")]
        private byte swingArm;

        [OnlineField(group = "animation")]
        bool kingWaiting;
        [OnlineField(group = "movement", nullable = true)]
        private Scavenger.MovementMode? movementMode;


        [OnlineField(polymorphic = true, nullable = true)]
        private AnimationState? animationState;

        [OnlineField(group = "look")]
        Vector2 lookPoint;

        [OnlineField(group = "look", nullable = true)]
        OnlineEntity.EntityId? lookAtEntity;

        public RealizedScavengerState() { }
        public RealizedScavengerState(OnlineCreature onlineCreature) : base(onlineCreature)
        {
            var scav = onlineCreature.apo.realizedObject as Scavenger;
            this.swingPos = scav.swingPos;
            this.swingRadius = scav.swingRadius;
            this.flip = scav.flip;
            this.kingWaiting = scav.kingWaiting;
            this.movementMode = scav.movMode;
            this.animationState = AnimationState.GetAnimationState(scav.animation);
            this.swingClimbCounter = (byte)scav.swingClimbCounter;
            this.swingArm = (byte)scav.swingArm;


            this.lookPoint = scav.lookPoint;
            if (scav.critLooker?.lookCreature?.representedCreature?.GetOnlineCreature() is OnlineCreature critter)
            {
                this.lookAtEntity = critter.id;
            }
            
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var scav = (onlineEntity as OnlineCreature).realizedCreature as Scavenger;
            if (scav != null)
            {
                scav.swingPos = this.swingPos;
                scav.swingRadius = this.swingRadius;
                scav.flip = this.flip;
                scav.swingClimbCounter = this.swingClimbCounter;
                scav.swingArm = this.swingArm;
                scav.kingWaiting = kingWaiting;

                // for some reason king's direct away from throne animation uses the Back off id despite being a different animation
                // thanks MSC
                if (animationState != null)
                {
                    if (!kingWaiting)
                    {
                        if (scav.animation is null || (scav.animation?.id != animationState.GetAnimationID()))
                        {
                            scav.animation = animationState.ConstructAnimation(scav);
                        }

                        if (scav.animation is not null)
                        {
                            animationState.ReadTo(scav, scav.animation);
                        }
                    }
                }
                else
                {
                    scav.animation = null;
                }

                if (scav.critLooker is not null)
                {
                    if (lookAtEntity?.FindEntity() is OnlineCreature creature)
                    {
                        scav.critLooker.lookCreature = scav.AI.tracker.RepresentationForCreature(creature.abstractCreature, true);
                        scav.critLooker.lookFocusDelay = 10;
                    }
                    else if (lookAtEntity is null)
                    {
                        scav.critLooker.lookCreature = null;
                        scav.critLooker.lookFocusDelay = 10;
                    }

                    scav.lookPoint = lookPoint;
                }

                scav.movMode = movementMode;
                scav.moveModeChangeCounter = 10;
            }
        }
    }
}