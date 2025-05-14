using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Watcher;
using static RainMeadow.MeadowProgression;

namespace RainMeadow.Game
{
    /// <summary>
	/// Largely based on a code snippet from EtiTheSpirit's Dreams Of Infinite Glass mod.
    /// https://github.com/EtiTheSpirit/DreamsOfInfiniteGlass/blob/master/Character/DeathContextualizer.cs#L17
    /// 
    /// Add any methods you want to call a Player Death Event. Events are called before Creature.Die
	/// </summary>
    public static class DeathContextualizer
    {

        public static ConditionalWeakTable<Player, ViolentAction> lastViolence = new();

        public static void CreateBindings()
        {
            // NOT PRETTY BUT IT WORKS
            // Doing things like getting DeclaringType and IsStatic through ILContext is really funky so we'll instead get it straight from the source.

            // DIE
            IL.ZapCoil.Update += (il) => Bind(il, typeof(ZapCoil).GetMethod(nameof(ZapCoil.Update)));
            //IL.WormGrass.WormGrassPatch.InteractWithCreature += (il) => Bind(il, typeof(WormGrass.WormGrassPatch).GetMethod(nameof(WormGrass.WormGrassPatch.Update)));
            IL.SSOracleBehavior.Update += (il) => Bind(il, typeof(SSOracleBehavior).GetMethod(nameof(SSOracleBehavior.Update)));
            IL.SSOracleBehavior.ThrowOutBehavior.Update += (il) => Bind(il, typeof(SSOracleBehavior.ThrowOutBehavior).GetMethod(nameof(SSOracleBehavior.ThrowOutBehavior.Update)));
            IL.DaddyCorruption.EatenCreature.Update += (il) => Bind(il, typeof(DaddyCorruption.EatenCreature).GetMethod(nameof(DaddyCorruption.EatenCreature.Update)));
            IL.Player.Tongue.Update += (il) => Bind(il, typeof(Player.Tongue).GetMethod(nameof(Player.Tongue.Update)));
            IL.LocustSystem.Swarm.Update += (il) => Bind(il, typeof(LocustSystem.Swarm).GetMethod(nameof(LocustSystem.Swarm.Update)));
            IL.Player.RippleSpawnInteractions += (il) => Bind(il, typeof(Player).GetMethod(nameof(Player.RippleSpawnInteractions)));

            // VIOLENCE
            IL.ElectricDeath.Update += (il) => BindViolence(il, typeof(ElectricDeath).GetMethod(nameof(ElectricDeath.Update)));
            IL.Explosion.Update += (il) => BindViolence(il, typeof(Explosion).GetMethod(nameof(Explosion.Update)));
            IL.KingTusks.Tusk.ShootUpdate += (il) => BindViolence(il, typeof(KingTusks.Tusk).GetMethod(nameof(KingTusks.Tusk.ShootUpdate)));
            IL.Pomegranate.Collide += (il) => BindViolence(il, typeof(Pomegranate).GetMethod(nameof(Pomegranate.Collide)));
            IL.UnderwaterShock.Update += (il) => BindViolence(il, typeof(UnderwaterShock).GetMethod(nameof(UnderwaterShock.Update)));
            IL.Watcher.FlameJet.Update += (il) => BindViolence(il, typeof(FlameJet).GetMethod(nameof(FlameJet.Update)));
            IL.Watcher.LightningMaker.StrikeAOE.Update += (il) => BindViolence(il, typeof(LightningMaker.StrikeAOE).GetMethod(nameof(LightningMaker.StrikeAOE.Update)));
        }

        /// <summary>
        /// ILHook into the provided method to call DeathContextualizer.TryDeathEvent before any occourances of Creature.Die
        /// </summary>
        /// <param name="il"> 
        /// Your ILContext
        /// </param>
        /// <param name="original"> 
        /// Your MethodBase from System.Reflection, use typeof(T).GetMethod(nameof(YourMethod))
        /// </param>
        private static void Bind(ILContext il, MethodBase original)
        {
            var c = new ILCursor(il);

            try
            {
                while (c.TryGotoNext(MoveType.Before,
                    i => i.MatchCallvirt<Creature>(nameof(Creature.Die))))
                {
                    // Dupe creature being killed
                    c.Emit(OpCodes.Dup);

                    // Get caller type
                    c.Emit(OpCodes.Ldtoken, original.DeclaringType);
                    c.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));

                    // Load null onto stack if method is static, otherwise load the type
                    if (original.IsStatic)
                    {
                        c.Emit(OpCodes.Ldnull);
                    }
                    else
                    {
                        c.Emit(OpCodes.Ldarg_0);
                    }

                    // Lastly call the event method
                    c.Emit(OpCodes.Call, typeof(DeathContextualizer).GetMethod(nameof(TryDeathEvent), BindingFlags.Static | BindingFlags.NonPublic));
                    Custom.Log("DeathContextualizer binded with " + original.DeclaringType);

                    // Move after Creature.Die once we're done
                    c.GotoNext(MoveType.After, i => i.MatchCallvirt<Creature>(nameof(Creature.Die)));
                }

            }
            catch (Exception e)
            {
                Custom.LogWarning("DeathContextualizer hooked with errors. - Type: " + original.DeclaringType + " - " + e);
            }
        }

        /// <summary>
        /// ILHook into the provided method to call DeathContextualizer.TryViolenceEvent before any occourances of Creature.Violence
        /// </summary>
        /// <param name="il"> 
        /// Your ILContext
        /// </param>
        /// <param name="original"> 
        /// Your MethodBase from System.Reflection, use typeof(T).GetMethod(nameof(YourMethod))
        /// </param>
        private static void BindViolence(ILContext il, MethodBase original)
        {
            var c = new ILCursor(il);

            try
            {
                while (c.TryGotoNext(MoveType.Before,
                    i => i.MatchCallOrCallvirt<Creature>(nameof(Creature.Violence))))
                {
                    // Make a skip label
                    var skip = il.DefineLabel();

                    // Get caller type
                    c.Emit(OpCodes.Ldtoken, original.DeclaringType);
                    c.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));

                    // Load null onto stack if method is static, otherwise load the type
                    if (original.IsStatic)
                    {
                        c.Emit(OpCodes.Ldnull);
                    }
                    else
                    {
                        c.Emit(OpCodes.Ldarg_0);
                    }

                    // Replace creature.Violence with a delegate that calls our event first.
                    c.EmitDelegate((Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus, Type callerType, object caller) => {
                        TryViolentEvent(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus, callerType, caller);
                        self.Violence(source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
                        if (self is Player player)
                        {
                            lastViolence.TryGetValue(player, out var violence);
                            if (violence != null) violence.sameTick = false;
                        }
                    });
                    c.Emit(OpCodes.Br, skip);
                    c.GotoNext(moveType: MoveType.After,
                        i => i.MatchCallOrCallvirt<Creature>(nameof(Creature.Violence))
                    );
                    c.MarkLabel(skip);

                    Custom.Log("DeathContextualizer binded with " + original.DeclaringType);
                }

            }
            catch (Exception e)
            {
                Custom.LogWarning("DeathContextualizer hooked with errors. - Type: " + original.DeclaringType + " - " + e);
            }
        }

        private static void TryViolentEvent(Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus, Type callerType, object caller)
        {
            if (self is Player player)
            {
                if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is MeadowGameMode) return;
                if (player.dead) return;
                if (lastViolence.TryGetValue(player, out var _))
                {
                    lastViolence.Remove(player);
                }
                lastViolence.Add(player, new ViolentAction(source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus, callerType, caller));
            }
        }

        private static void TryDeathEvent(object creatureObj, Type callerType, object instance)
        {
            if (creatureObj is Creature creature && creature is Player player)
            {
                DeathMessage.PlayerDeathEvent(player, callerType, instance);
            }
        }

        public class ViolentAction
        {
            public Type callerType;
            public object caller;
            public bool sameTick;

            public BodyChunk source;
            public Vector2? directionAndMomentum;
            public BodyChunk hitChunk;
            public PhysicalObject.Appendage.Pos hitAppendage;
            public Creature.DamageType type;
            public float damage;
            public float stunBonus;

            public ViolentAction(BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus, Type callerType, object caller)
            {
                this.source = source;
                this.directionAndMomentum = directionAndMomentum;
                this.hitChunk = hitChunk;
                this.hitAppendage = hitAppendage;
                this.type = type;
                this.damage = damage;
                this.stunBonus = stunBonus;

                this.callerType = callerType;
                this.caller = caller;
                this.sameTick = true;
            }
        }
    }
}
