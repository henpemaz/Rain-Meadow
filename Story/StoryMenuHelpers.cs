using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RainMeadow
{
    public static class StoryMenuHelpers
    {
        #region Remix

        // TODO: make this per-gamemode?
       
        #endregion

        public static void RemoveMenuObjects(params MenuObject?[] objs)
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
