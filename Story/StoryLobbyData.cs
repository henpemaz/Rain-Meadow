using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using static RainMeadow.OnlineResource;

namespace RainMeadow
{
    //playerSessionData - passage
    internal class StoryLobbyData : OnlineResource.ResourceData
    {
        public StoryLobbyData(OnlineResource resource) : base(resource) { }

        internal override ResourceDataState MakeState()
        {
            return new State(this, resource);
        }

        public class State : ResourceDataState
        {
            [OnlineField(nullable=true)]
            public string? defaultDenPos;
            [OnlineField]
            public bool didStartGame;
            [OnlineField]
            public SlugcatStats.Name currentCampaign;
            [OnlineField]
            public bool didStartCycle;
            [OnlineField]
            public bool reinforcedKarma;
            [OnlineField]
            public int karma;
            [OnlineField]
            public bool theGlow;
            [OnlineField]
            public int food;
            [OnlineField]
            public int quarterfood;
            [OnlineField]
            public int mushroomCounter;
            [OnlineField]
            public Dictionary<string, bool> storyBoolRemixSettings;
            [OnlineField]
            public Dictionary<string, float> storyFloatRemixSettings;
            [OnlineField]
            public Dictionary<string, int> storyIntRemixSettings;


            public State() {}

            public State(StoryLobbyData storyLobbyData, OnlineResource onlineResource)
            {
                StoryGameMode storyGameMode = (onlineResource as Lobby).gameMode as StoryGameMode;
                RainWorldGame currentGameState = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;

                currentCampaign = storyGameMode.currentCampaign;
                storyBoolRemixSettings = storyGameMode.storyBoolRemixSettings;
                storyFloatRemixSettings = storyGameMode.storyFloatRemixSettings;
                storyIntRemixSettings = storyGameMode.storyIntRemixSettings;

                didStartGame = storyGameMode.didStartGame;
                didStartCycle = storyGameMode.didStartCycle;
                if (currentGameState?.session is StoryGameSession storySession)
                {
                    karma = storySession.saveState.deathPersistentSaveData.karma;
                    theGlow = storySession.saveState.theGlow;
                    defaultDenPos = storySession.saveState.denPosition;
                    reinforcedKarma = storySession.saveState.deathPersistentSaveData.reinforcedKarma;
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
                var lobby = (data.resource as Lobby);

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
                    storySession.saveState.deathPersistentSaveData.reinforcedKarma = reinforcedKarma;
                    storySession.saveState.theGlow = theGlow;
                    (lobby.gameMode as StoryGameMode).defaultDenPos = defaultDenPos;
                }
                (lobby.gameMode as StoryGameMode).currentCampaign = currentCampaign;
                (lobby.gameMode as StoryGameMode).storyBoolRemixSettings = storyBoolRemixSettings;
                (lobby.gameMode as StoryGameMode).storyFloatRemixSettings = storyFloatRemixSettings;
                (lobby.gameMode as StoryGameMode).storyIntRemixSettings = storyIntRemixSettings;

                (lobby.gameMode as StoryGameMode).didStartGame = didStartGame;
                (lobby.gameMode as StoryGameMode).didStartCycle = didStartCycle;
            }
        }
    }
}
