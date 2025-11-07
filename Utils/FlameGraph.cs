using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMeadow
{
    internal static class FlameGraph
    {

        public static void OutputFlameGraph()
        {

            var snapshot = new List<MeadowProfiler.ProfilerInfo>(MeadowProfiler.snapshot);

            List<string> folded = new();

            foreach (var info in snapshot)
            {
                var folding = "root;";

                if (info.tree != null)
                {
                    for (int i = 0; i < info.tree.Length; i++)
                    {
                        folding += info.tree[i].name + ";";
                    }
                }
                if (folding.EndsWith(";"))
                {
                    folding = folding.TrimEnd(';');
                }
                folding += $" {info.ticksSpent}";

                folded.Add(folding);
            }
            File.WriteAllText("collapsed-stack.txt", ""); // Overwrite before writing
            File.AppendAllLines("collapsed-stack.txt", folded.ToArray());
        }

    }
}
