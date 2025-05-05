using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;

namespace RainMeadow
{
    public static class MenuHelpers
    {
        public static void SafeAddSubobjects(this MenuObject container, params MenuObject?[] subObjectsToAdd)
        {
            if (container == null || subObjectsToAdd == null)
            {
                return;
            }
            container.subObjects.AddRange(subObjectsToAdd.Where(x => x != null && !container.subObjects.Contains(x)));
        }
    }
}
