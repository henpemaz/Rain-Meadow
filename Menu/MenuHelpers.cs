using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;

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
        public static bool IsAllRemixUINotHeld(this MenuObject owner) => owner.subObjects.OfType<UIelementWrapper>().All(x => !(x.thisElement is UIconfig config && config.held));
    }
}
