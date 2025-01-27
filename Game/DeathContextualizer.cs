using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace RainMeadow.Game
{
    /// <summary>
	/// Largely based on a code snippet from EtiTheSpirit's Dreams Of Infinite Glass mod.
    /// https://github.com/EtiTheSpirit/DreamsOfInfiniteGlass/blob/master/Character/DeathContextualizer.cs#L17
    /// 
    /// We can perform a harmony modification to any method calling Creature.Die to call our method beforehand and handle
    /// death messages that are pretty tricky to ILHook into. This way is also good for future proofing.
	/// </summary>
    public static class DeathContextualizer
    {

        public static void CreateBindings()
        {
            Bind(RainMeadow.harmony, GetMethod<ZapCoil>(nameof(ZapCoil.Update)));
            Bind(RainMeadow.harmony, GetMethod<WormGrass.WormGrassPatch>(nameof(WormGrass.WormGrassPatch.InteractWithCreature)));
            Bind(RainMeadow.harmony, GetMethod<SSOracleBehavior>(nameof(SSOracleBehavior.Update)));
            Bind(RainMeadow.harmony, GetMethod<SSOracleBehavior.ThrowOutBehavior>(nameof(SSOracleBehavior.ThrowOutBehavior.Update)));
            Bind(RainMeadow.harmony, GetMethod<DaddyCorruption.EatenCreature>(nameof(DaddyCorruption.EatenCreature.Update)));
            Bind(RainMeadow.harmony, GetMethod<Player.Tongue>(nameof(Player.Tongue.Update)));
        }

        private static void TryDeathEvent(object creatureObj, Type callerType, object instance)
        {
            if (creatureObj is Creature creature && creature is Player player)
            {
                DeathMessage.PlayerDeathEvent(player, callerType, instance);
            }
        }

        private static void Bind(Harmony harmony, MethodBase target)
        {
            PatchProcessor processor = harmony.CreateProcessor(target);
            processor.AddTranspiler(new HarmonyMethod(typeof(DeathContextualizer).GetMethod(nameof(TranspileProcedure), BindingFlags.Static | BindingFlags.NonPublic)));
            processor.Patch();
        }

        public static MethodBase GetMethod<T>(string name)
        {
            return typeof(T).GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        }

        private static IEnumerable<CodeInstruction> TranspileProcedure(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Callvirt)
                {
                    if (instruction.operand != null && instruction.operand is MethodBase method && ValidDieMethod(method))
                    {
                        // Dupe the creature being killed
                        yield return new CodeInstruction(OpCodes.Dup);

                        // Get the caller type
                        yield return new CodeInstruction(OpCodes.Ldtoken, original.DeclaringType);
                        yield return new CodeInstruction(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));

                        if (original.IsStatic)
                        {
                            yield return new CodeInstruction(OpCodes.Ldnull);
                        }
                        else
                        {
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                        }

                        // Call the event
                        yield return new CodeInstruction(OpCodes.Call, typeof(DeathContextualizer).GetMethod(nameof(TryDeathEvent), BindingFlags.Static | BindingFlags.NonPublic));
                    }
                }
                yield return instruction;
            }
        }

        private static bool ValidDieMethod(MethodBase method)
        {
            return !method.IsStatic &&
                    method.Name == "Die" &&
                    method.GetParameters().Length == 0 &&
                    method.DeclaringType.IsAssignableFrom(typeof(Creature));
        }
    }
}
