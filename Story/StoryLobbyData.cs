using Mono.Cecil;
using System;
using System.Linq;
using static RainMeadow.OnlineResource;

namespace RainMeadow
{
    internal class StoryLobbyData : OnlineResource.ResourceData
    {
        public StoryLobbyData(OnlineResource resource) : base(resource) { }

        internal override ResourceDataState MakeState()
        {
            return new State(this, resource);
        }

        public class State : ResourceDataState
        {
            [OnlineField]
            public bool didStartGame;
            [OnlineField]
            public bool didStartCycle;
            [OnlineField]
            public int karma;

            [OnlineField]
            public int food;
            [OnlineField]
            public int quarterfood;
            [OnlineField]
            public int mushroomCounter;

            public State() {}

            public State(StoryLobbyData storyLobbyData, OnlineResource onlineResource)
            {
                StoryGameMode storyGameMode = (onlineResource as Lobby).gameMode as StoryGameMode;
                RainWorldGame currentGameState = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;

                didStartGame = storyGameMode.didStartGame;
                didStartCycle = storyGameMode.didStartCycle;
                if (currentGameState?.session is StoryGameSession storySession)
                {
                    karma = storySession.saveState.deathPersistentSaveData.karma;
                }

                food = (currentGameState?.Players[0].state as PlayerState)?.foodInStomach ?? 0;
                quarterfood = (currentGameState?.Players[0].state as PlayerState)?.quarterFoodPoints ?? 0;
                mushroomCounter = (currentGameState?.Players[0].realizedCreature as Player)?.mushroomCounter ?? 0;
            }

            internal override Type GetDataType() => typeof(StoryLobbyData);

            internal override void ReadTo(ResourceData data)
            {
                RainWorldGame currentGameState = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;
                var playerstate = (currentGameState?.Players[0].state as PlayerState);
                if (playerstate != null)
                {
                    playerstate.foodInStomach = food;
                    playerstate.quarterFoodPoints = quarterfood;
                }
                if ((currentGameState?.Players[0].realizedCreature is Player player))
                {
                    player.mushroomCounter = mushroomCounter;
                }

                if (currentGameState?.session is StoryGameSession storySession)
                {
                    storySession.saveState.deathPersistentSaveData.karma = karma;
                }
                var lobby = (data.resource as Lobby);
                (lobby.gameMode as StoryGameMode).didStartGame = didStartGame;
                (lobby.gameMode as StoryGameMode).didStartCycle = didStartCycle;
            }
        }
    }
}
