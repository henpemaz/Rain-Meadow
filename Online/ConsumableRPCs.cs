using UnityEngine;

namespace RainMeadow
{
    public static class ConsumableRPCs
    {
        [RPCMethod]
        public static void pacifySporePlant(OnlinePhysicalObject onlineSporePlant) {
            (onlineSporePlant.apo.realizedObject as SporePlant).Pacify(); 
        }

        [RPCMethod]
        public static void explodePuffBall(RoomSession onlineRoom, Vector2 pos, Color sporeColor, Color puffballColor) 
        {
            var room = onlineRoom.absroom.realizedRoom;
            InsectCoordinator smallInsects = null;
            for (int i = 0; i < room.updateList.Count; i++)
            {
                if (room.updateList[i] is InsectCoordinator)
                {
                    smallInsects = (room.updateList[i] as InsectCoordinator);
                    break;
                }
            }
            for (int j = 0; j < 70; j++)
            {
                room.AddObject(new SporeCloud(pos, RWCustom.Custom.RNV() * Random.value * 10f, sporeColor, 1f, null, j % 20, smallInsects));
            }
            room.AddObject(new SporePuffVisionObscurer(pos));
            for (int k = 0; k < 7; k++)
            {
                room.AddObject(new PuffBallSkin(pos, RWCustom.Custom.RNV() * Random.value * 16f, puffballColor, Color.Lerp(puffballColor, sporeColor, 0.5f)));
            }
            room.PlaySound(SoundID.Puffball_Eplode, pos);
        }

        [RPCMethod]
        public static void enableTheGlow() {
            RainWorldGame currentGameState = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;
            if (currentGameState?.session is StoryGameSession storySession)
            {
                storySession.saveState.theGlow = true;
                (currentGameState.Players[0].realizedCreature as Player).glowing = true;
            }
        }
    }
}
