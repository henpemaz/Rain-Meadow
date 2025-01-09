using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RainMeadow
{
    internal static class StoryMenuHelpers
    {

        internal static void RemoveMenuObjects(params MenuObject?[] objs)
        {
            foreach (var obj in objs)
            {
                if (obj is not null)
                {
                    obj.RemoveSprites();
                    obj.owner.RemoveSubObject(obj);
                }
            }
        }
    }
}
