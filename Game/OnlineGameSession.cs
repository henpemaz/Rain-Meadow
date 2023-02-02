using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RainMeadow
{
    // OnlineGameSession is tightly coupled to a lobby, and the highest ownership level
    public partial class OnlineGameSession : StoryGameSession
    {
        public OnlineGameSession(RainWorldGame game) : base(RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer, game)
        {
            RainMeadow.sLogger.LogInfo("OnlineGameSession created");
        }

        internal void FilterItems(Room room)
        {
            foreach (var item in room.roomSettings.placedObjects)
            {
                if(item.active && !AllowedInMode(item))
                {
                    item.active = false;
                }
            }
        }

        private bool AllowedInMode(PlacedObject item)
        {
            return cosmeticItems.Contains(item.type);

        }

        internal bool ShouldSpawnRoomItems(RainWorldGame game)
        {
            return false;
        }

        internal bool ShouldLoadCreatures(RainWorldGame game)
        {
            return false;
        }
    }
}
