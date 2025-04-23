using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RainMeadow
{
    internal static class StoryMenuHelpers
    {
        public static bool AmIDead(this AbstractCreature? aC)
        {
            return aC != null && (aC.state?.dead == true || aC.realizedCreature?.dead == true);
        }
        public static bool CanRespawn(this StoryGameMode? storyMode, out WorldCoordinate? spawnArea) //output that is bool should indicate worldcoo is not null
        {
            spawnArea = null;
            if (OnlineManager.lobby?.playerAvatars != null && OnlineManager.lobby.playerAvatars.Any(x => x.Key.isMe && ((x.Value.FindEntity(true) as OnlinePhysicalObject)?.apo as AbstractCreature).AmIDead()))
            {
                if (storyMode?.difficultyMode == StoryGameMode.DifficultyMode.Easy)
                {
                    spawnArea = (OnlineManager.lobby.playerAvatars.Find(x => x.Key?.isMe == true).Value?.FindEntity(true) as OnlinePhysicalObject)?.apo.pos;
                }
                if (storyMode?.difficultyMode == StoryGameMode.DifficultyMode.Medium)
                {
                    spawnArea = OnlineManager.lobby.playerAvatars.Select(x => x.Value.FindEntity(true)).OfType<OnlinePhysicalObject>().Where(x => (x.apo as AbstractCreature)?.Room?.gate == true)?.FirstOrDefault()?.apo.pos;
                }
            }
            return spawnArea != null;
        }
        public static List<AbstractCreature> AbstractPlayerCreatures() //warning - some can be null
        {
            if (OnlineManager.lobby?.playerAvatars == null)
            {
                return [];
            }
            return [.. OnlineManager.lobby.playerAvatars.Select(x => x.Value).Select(kv => kv.FindEntity(true)).OfType<OnlinePhysicalObject>().Where(x => !(x?.isMine == true)).Select(x => x.apo as AbstractCreature)];
        }
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
