using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;
using static Rewired.Controller;

namespace RainMeadow
{
    // Based on the RuntimeUnityEditor profiler
    internal class MeadowProfiler
    {
        public static MeadowProfiler Instance;

        public static readonly Dictionary<long, ProfilerInfo> data = new();
        public static readonly List<ProfilerInfo> snapshot = new();

        private static readonly Harmony harmony = new("meadow-profiler");

        public static bool patched;

        public float gameUpdateTiming;
        public static int executionCount;

        public static int currentDepth;
        public static List<StackPosition> currentStackTree = new();

        private static int mainThread;

        public bool open;

        // Code dealing with shaders seems to completely expode so we'll just avoid it
        private static readonly string[] avoid =
        [
            "RippleCameraData",
            "Snow",
            "Menu.Remix",
            "DevInterface",
            "ConfigAcceptable",
            "Configurable",
            "InputPackage",
            "EnumExt",
            "ModManager",
            "SoundLoader.SoundData",
            "MenuSoundObject",
            "OptionInterface",
            "AudioSourcePoolItem",
            "SimplePool"
        ];

        public MeadowProfiler()
        {
            Instance = this;
            mainThread = Thread.CurrentThread.ManagedThreadId;
            RainMeadow.Debug($"MeadowProfiler started.");
        }

        public void Destroy()
        {
            RainMeadow.Debug($"MeadowProfiler stopped.");
            Instance = null;
        }

        private static bool IsStruct(Type source)
        {
            return source.IsValueType && !source.IsPrimitive && !source.IsEnum;
        }
        private static bool IsProperty(MethodBase method)
        {
            if (!method.IsSpecialName) return false;
            return method.Name.StartsWith("get_") || method.Name.StartsWith("set_");
        }

        public static void FullPatch()
        {
            data.Clear();

            harmony.UnpatchSelf();

            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(x => x.GetName().Name.Equals("Assembly-CSharp"));

            var hits = asm.GetTypesSafely()
                .Select(t =>
                {
                    if (t.ContainsGenericParameters)
                    {
                        try
                        {
                            var type = t.MakeGenericType(t.GetGenericArguments().Select(x => x.BaseType).ToArray());
                            return type;
                        }
                        catch (Exception ex)
                        {
                            RainMeadow.Error($"MeadowProfiler failed to hook in class {t.FullName}");
                            return null;
                        }
                    }
                    return t;
                })
                .SelectMany(x => x?.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? new MethodBase[0])
                .Where(x =>
                {
                    return x.Name.Contains("Update") && !avoid.Any(x.Name.Contains) && !avoid.Any(x.DeclaringType.FullName.Contains) &&
                    !x.IsVirtual && !IsStruct(x.DeclaringType) && !IsProperty(x);
                })
                .ToList();

            foreach(var hit in hits)
            {
                try
                {
                    harmony.Patch(original: hit,
                        prefix: new HarmonyMethod(typeof(MeadowProfiler), nameof(Push)) { priority = int.MaxValue },
                        postfix: new HarmonyMethod(typeof(MeadowProfiler), nameof(Pop)) { priority = int.MinValue });
                }
                catch(Exception ex)
                {
                    RainMeadow.Error($"MeadowProfiler failed to hook {hit.FullDescription()}");
                }
            }

            patched = true;
            RainMeadow.Debug("Advanced Profiling Enabled");
        }

        public void Update()
        {
            snapshot.Clear();

            snapshot.AddRange(data.Values.OrderBy(x => -x.ticksSpent));
        }

        private string FormatedName(MethodBase method)
        {
            return $"{method.DeclaringType.FullName}.{method.Name}";
        }

        public class ProfilerInfo
        {
            public readonly string name;
            public readonly MethodBase method;

            public readonly Stopwatch timer;

            public StackPosition[] tree;

            public long ticksSpent;
            public long hash;
            
            private int depth;
            private int highestDepth;

            public int Depth
            {
                get => depth;
                set
                {
                    depth = value;
                    if (highestDepth < value)
                    {
                        highestDepth = value;
                    }
                }
            }
            public ProfilerInfo(MethodBase method)
            {
                this.method = method;
                this.name = $"{method.DeclaringType.FullName}.{method.Name}";

                this.timer = new Stopwatch();
            }
        }

        public struct StackPosition
        {
            public long hash;
            public string name;
            public StackPosition(long hash, string name)
            {
                this.hash = hash;
                this.name = name;
            }
        }

        public static long GetKeyHash(MethodBase __originalMethod, object __instance)
        {
            return __instance.GetHashCode() + ((long)__originalMethod.GetHashCode() << 32);
        }

        public static bool Push(MethodBase __originalMethod, object __instance, ref ProfilerInfo __state)
        {
            // For now I'm only worried about the main thread
            if (Thread.CurrentThread.ManagedThreadId != mainThread) return true;
            var hash = GetKeyHash(__originalMethod, __instance);

            if (!data.TryGetValue(hash, out var info))
            {
                info = new ProfilerInfo(__originalMethod);
                data.Add(hash, info);
            }

            info.Depth = currentDepth++;

            if (currentStackTree.Count > 0)
            {
                info.tree = currentStackTree.ToArray();
            }
            currentStackTree.Add(new StackPosition(hash, info.name));

            info.hash = hash;

            info.timer.Reset();
            info.timer.Start();

            __state = info;

            return true;
        }

        public static void Pop(bool __runOriginal, ProfilerInfo __state)
        {
            if (Thread.CurrentThread.ManagedThreadId != mainThread) return;
            if (__state == null) return;

            var info = __state;

            currentDepth = Math.Max(0, currentDepth - 1);
            if (currentStackTree.Count > 0)
            {
                currentStackTree.RemoveAt(currentStackTree.FindLastIndex(x => x.hash == info.hash && x.name == info.name));
            }

            info.ticksSpent = info.timer.ElapsedTicks;
        }
    }
}
