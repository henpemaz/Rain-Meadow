using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

namespace RainMeadow
{
    internal class MeadowProfiler
    {
        public static MeadowProfiler Instance;

        public Dictionary<string, float[]> data = new();
        public float gameUpdateTiming;

        public bool open;

        private Stopwatch stopwatch;
        private int timer;

        public MeadowProfiler()
        {
            Instance = this;
            RainMeadow.Debug($"MeadowProfiler started.");
        }

        public void Destroy()
        {
            RainMeadow.Debug($"MeadowProfiler stopped.");
            Instance = null;
        }

        public void Update()
        {
            timer++;
            if (timer >= 40)
            {
                timer = 0;
                open = true;
            }
        }

        public void Pop()
        {
            if (stopwatch is null) return;
            stopwatch.Stop();
            long ticks = stopwatch.ElapsedTicks;
            gameUpdateTiming = (float)ticks / Stopwatch.Frequency * 1000f;
        }

        public void Push()
        {
            stopwatch = Stopwatch.StartNew();
        }

        private string FormatedName(MethodBase method)
        {
            return $"{method.DeclaringType.FullName}.{method.Name}";
        }
    }
}
