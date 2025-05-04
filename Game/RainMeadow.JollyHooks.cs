using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Globalization;
using System.Linq;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        private void JollyHooks() {
            new Hook(typeof(RainWorldGame).GetProperty(nameof(RainWorldGame.StoryPlayerCount)).GetGetMethod(), 
                RainWorldGame_get_PlayerNum);

        }

        private int RainWorldGame_get_PlayerNum(Func<RainWorldGame, int> orig, RainWorldGame self) {
            if (isStoryMode(out var story)) {
                return story.avatarCount;
            }

            return orig(self);
        }
    }
}
