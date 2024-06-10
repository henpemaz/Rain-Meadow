using UnityEngine;
using RWCustom;
using MonoMod.Cil;
using System;
using Mono.Cecil.Cil;

namespace RainMeadow
{
    class NoodleController : AirCreatureController
    {
        private bool actLock; // act is hooked both at base and an override
        private bool forceMove;

        private NeedleWorm noodle => creature as NeedleWorm;

        public static void EnableNoodle()
        {
            On.BigNeedleWorm.Update += BigNeedleWorm_Update;
            On.NeedleWorm.Act += NeedleWorm_Act;
            On.BigNeedleWormAI.Update += BigNeedleWormAI_Update;

            On.SmallNeedleWorm.Update += SmallNeedleWorm_Update;
            On.SmallNeedleWorm.Act += SmallNeedleWorm_Act;
            On.SmallNeedleWormAI.Update += SmallNeedleWormAI_Update;

            IL.NeedleWorm.Fly += NeedleWorm_Fly;
        }

        private static void NeedleWorm_Fly(ILContext il)
        {
            var c = new ILCursor(il);
            int loc = 0;
            ILLabel after = null;
            c.GotoNext(MoveType.After, // set that one flag that cuts off the "at destination" branch
                i => i.MatchCall<MovementConnection>("op_Equality"),
                i => i.MatchStloc(out loc),
                i => i.MatchBr(out after)
                );
            c.GotoLabel(after);
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, loc);
            c.EmitDelegate<Func<NeedleWorm, bool, bool>>((self, orig) =>
            {
                if (creatureControllers.TryGetValue(self, out var p))
                {
                    return orig || (p as NoodleController).forceMove;
                }
                return orig;
            });
            c.Emit(OpCodes.Stloc, loc);

            c.GotoNext(MoveType.After, // num, first we find the loc then we patch its usage
                i => i.MatchCall<NeedleWorm>("get_SlowFlySpeed"),
                i => i.MatchLdcR4(1f),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<NeedleWorm>("extraMovementForce"),
                i => i.MatchCallOrCallvirt(out _),
                i => i.MatchMul(),
                i => i.MatchStloc(out loc)
                );

            // but also PLEASE don't just do this
            c.GotoNext(MoveType.After,
                i => i.MatchStfld<NeedleWorm>("extraMovementForce")
                );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<NeedleWorm>>((self) =>
            {
                if (creatureControllers.TryGetValue(self, out var p))
                {
                    self.extraMovementForce = 0f; // ZERO this weird guy out
                }
            });

            // insert before
            // if (followingConnection != default(MovementConnection))
            c.Index = il.Instrs.Count - 1;
            c.GotoPrev(MoveType.After,
                i => i.MatchCall<MovementConnection>("op_Inequality")
                );
            c.GotoPrev(MoveType.Before,
                i => i.MatchLdarg(1)
                );
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, loc);
            c.EmitDelegate<Func<NeedleWorm, float, float>>((self, orig) =>
            {
                if (creatureControllers.TryGetValue(self, out var p))
                {
                    return self.SlowFlySpeed; // use the original value not your weird compensated thing thanks
                }
                return orig;
            });
            c.Emit(OpCodes.Stloc, loc);
        }

        public override WorldCoordinate CurrentPathfindingPosition
        {
            get
            {
                if (!forceMove && Custom.DistLess(creature.coord, noodle.AI.pathFinder.destination, 3))
                {
                    return noodle.AI.pathFinder.destination;
                }
                return base.CurrentPathfindingPosition;
            }
        }

        internal override bool FindDestination(WorldCoordinate basecoord, out WorldCoordinate toPos, out float magnitude)
        {
            if (base.FindDestination(basecoord, out toPos, out magnitude)) return true;
            var basepos = creature.firstChunk.pos;
            var dest = basepos + this.inputDir * 56f;
            if (noodle.flying > 0)
            {
                dest += 1.4f * (creature.bodyChunks[0].pos - creature.bodyChunks[1].pos); // lookdir of the sorts, when it looks down going side-to-side it gets weird
                dest.y += noodle.small ? 8f : 4f; // purely empirical
            }
            if (Mathf.Abs(this.inputDir.y) < 0.1f) // trying to move horizontally, compensate for momentum a bit
            {
                dest.y -= creature.mainBodyChunk.vel.y * 2f;
            }
            toPos = creature.room.GetWorldCoordinate(dest);
            magnitude = inputDir.magnitude;
            return true;
        }

        private static void SmallNeedleWormAI_Update(On.SmallNeedleWormAI.orig_Update orig, SmallNeedleWormAI self)
        {
            if (creatureControllers.TryGetValue(self.creature.realizedCreature, out var p))
            {
                p.AIUpdate(self);
            }
            else
            {
                orig(self);
            }
        }

        private static void SmallNeedleWorm_Act(On.SmallNeedleWorm.orig_Act orig, SmallNeedleWorm self)
        {
            if (creatureControllers.TryGetValue(self, out var p))
            {
                (p as NoodleController).actLock = true;
                p.ConsciousUpdate();
                (p as NoodleController).actLock = false;
            }

            orig(self);
        }

        private static void SmallNeedleWorm_Update(On.SmallNeedleWorm.orig_Update orig, SmallNeedleWorm self, bool eu)
        {
            if (creatureControllers.TryGetValue(self, out var p))
            {
                p.Update(eu);
            }

            orig(self, eu);
        }

        private static void NeedleWorm_Act(On.NeedleWorm.orig_Act orig, NeedleWorm self)
        {
            if (creatureControllers.TryGetValue(self, out var p) && !(p as NoodleController).actLock)
            {
                p.ConsciousUpdate();
            }

            orig(self);
        }

        private static void BigNeedleWormAI_Update(On.BigNeedleWormAI.orig_Update orig, BigNeedleWormAI self)
        {
            if (creatureControllers.TryGetValue(self.creature.realizedCreature, out var p))
            {
                p.AIUpdate(self);
            }
            else
            {
                orig(self);
            }
        }

        private static void BigNeedleWorm_Update(On.BigNeedleWorm.orig_Update orig, BigNeedleWorm self, bool eu)
        {
            if (creatureControllers.TryGetValue(self, out var p))
            {
                p.Update(eu);
            }

            orig(self, eu);

            RainMeadow.Trace($"atDestThisFrame? {self.atDestThisFrame}");
        }

        public NoodleController(Creature creature, OnlineCreature oc, int playerNumber) : base(creature, oc, playerNumber)
        {
        }

        protected override void LookImpl(Vector2 pos)
        {
            noodle.lookDir = (pos - creature.DangerPos).normalized;
            noodle.getToLookDir = noodle.lookDir;
        }

        protected override void Moving(float magnitude)
        {
            noodle.AI.behavior = NeedleWormAI.Behavior.GetUnstuck;
            noodle.AI.flySpeed = Custom.LerpAndTick(noodle.AI.flySpeed, magnitude, 0.2f, 0.05f);
            forceMove = true;
        }

        protected override void Resting()
        {
            noodle.AI.behavior = NeedleWormAI.Behavior.Idle;
            noodle.AI.flySpeed = Custom.LerpAndTick(noodle.AI.flySpeed, 0, 0.4f, 0.1f);
            forceMove = false;
        }
    }
}
