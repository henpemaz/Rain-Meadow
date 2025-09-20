using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static RainMeadow.StateProfiler;
using Random = UnityEngine.Random;

namespace RainMeadow
{
    internal class StateProfiler
    {
        public static StateProfiler? Instance;

        public Dictionary<Type, StateRep> data = new();

        public StateProfiler()
        {
            Instance = this;
        }

        public void Push(Type type)
        {
            if (!data.TryGetValue(type, out var stateRep))
            {
                stateRep = new StateRep(type);
                data.Add(type, stateRep);
            }

            stateRep.timer.Reset();
            stateRep.timer.Start();
        }

        public void Pop(Type type)
        {
            if (!data.TryGetValue(type, out var stateRed)) return;
            stateRed.ticksSpent = stateRed.timer.ElapsedTicks;
        }

        public enum Category
        {

        }

        public class StateRep
        {
            public readonly Type type;
            public readonly Color color;
            public readonly Category category;

            public readonly Stopwatch timer;

            public long ticksSpent;
            public StateRep(Type type)
            {
                this.type = type;
                this.color = new Color(Random.Range(0.2f, 0.8f), Random.Range(0.2f, 0.8f), Random.Range(0.2f, 0.8f));
                this.timer = new();
            }
        }
    }
}
