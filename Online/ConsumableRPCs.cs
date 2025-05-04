using UnityEngine;

namespace RainMeadow
{
    public static class ConsumableRPCs
    {
        [RPCMethod]
        public static void pacifySporePlant(OnlinePhysicalObject onlineSporePlant)
        {
            (onlineSporePlant.apo.realizedObject as SporePlant).Pacify();
        }

        [RPCMethod]
        public static void enableTheGlow()
        {
            RainWorldGame currentGameState = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;
            if (currentGameState?.session is StoryGameSession storySession)
            {
                storySession.saveState.theGlow = true;
                for (int i = 0; i < currentGameState.Players.Count; i++) {
                    (currentGameState.Players[i].realizedCreature as Player).glowing = true;
                }   
            }
        }

        [RPCMethod]
        public static void reportConsumedItem(bool karmaFlower, int originroom, int placedObjectIndex, int waitCycles)
        {
            RainWorldGame currentGameState = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;
            if (currentGameState?.session is StoryGameSession storySession)
            {
                storySession.saveState.ReportConsumedItem(currentGameState.world, karmaFlower, originroom, placedObjectIndex, waitCycles);
            }
        }
    }
}
