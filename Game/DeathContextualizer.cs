using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
        public static void CreateBindings()
        {
            // NOT PRETTY BUT IT WORKS
            // Doing things like getting DeclaringType and IsStatic through ILContext is really funky so we'll instead get it straight from the source.
            IL.ZapCoil.Update += (il) => Bind(il, typeof(ZapCoil).GetMethod(nameof(ZapCoil.Update)));
            IL.WormGrass.WormGrassPatch.InteractWithCreature += (il) => Bind(il, typeof(WormGrass.WormGrassPatch).GetMethod(nameof(WormGrass.WormGrassPatch.Update)));
            IL.SSOracleBehavior.Update += (il) => Bind(il, typeof(SSOracleBehavior).GetMethod(nameof(SSOracleBehavior.Update)));
            IL.SSOracleBehavior.ThrowOutBehavior.Update += (il) => Bind(il, typeof(SSOracleBehavior.ThrowOutBehavior).GetMethod(nameof(SSOracleBehavior.ThrowOutBehavior.Update)));
            IL.DaddyCorruption.EatenCreature.Update += (il) => Bind(il, typeof(DaddyCorruption.EatenCreature).GetMethod(nameof(DaddyCorruption.EatenCreature.Update)));
            IL.Player.Tongue.Update += (il) => Bind(il, typeof(Player.Tongue).GetMethod(nameof(Player.Tongue.Update)));
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

        private static void TryDeathEvent(object creatureObj, Type callerType, object instance)
        {
            if (creatureObj is Creature creature && creature is Player player)
            {
                DeathMessage.PlayerDeathEvent(player, callerType, instance);
            }
        }
    }
}
