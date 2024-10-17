using System;
using System.Collections.Generic;
using static RainMeadow.OnlineResource;

namespace RainMeadow
{
    //playerSessionData - passage
    public class StoryLobbyData : OnlineResource.ResourceData
    {
        public override ResourceDataState MakeState(OnlineResource resource)
        {
            return new State(this, resource);
        }

        public class State : ResourceDataState
        {
            [OnlineField(nullable = true)]
            public string? defaultDenPos;
            [OnlineField(nullable = true)]
            public string? region;
            [OnlineField]
            public bool isInGame;
            [OnlineField]
            public bool changedRegions;
            [OnlineField]
            public byte readyForGate;
            [OnlineField]
            public bool friendlyFire;
            [OnlineField]
            public SlugcatStats.Name currentCampaign;
            [OnlineField]
            public int cycleNumber;
            [OnlineField]
            public bool didStartCycle;
            [OnlineField]
            public bool reinforcedKarma;
            [OnlineField]
            public int karmaCap;
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
            public Dictionary<string, int> ghostsTalkedTo;
            [OnlineField]
            public Dictionary<ushort, ushort[]> consumedItems;
            [OnlineField]
            public Dictionary<string, bool> storyBoolRemixSettings;
            [OnlineField]
            public Dictionary<string, float> storyFloatRemixSettings;
            [OnlineField]
            public Dictionary<string, int> storyIntRemixSettings;


            public State() { }

            public State(StoryLobbyData storyLobbyData, OnlineResource onlineResource)
            {
                StoryGameMode storyGameMode = (onlineResource as Lobby).gameMode as StoryGameMode;
                RainWorldGame currentGameState = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;

                defaultDenPos = storyGameMode.defaultDenPos;
                currentCampaign = storyGameMode.currentCampaign;
                consumedItems = storyGameMode.consumedItems;
                storyFloatRemixSettings = storyGameMode.storyFloatRemixSettings;
                storyIntRemixSettings = storyGameMode.storyIntRemixSettings;

                ghostsTalkedTo = storyGameMode.ghostsTalkedTo;

                isInGame = RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame;
                changedRegions = storyGameMode.changedRegions;
                didStartCycle = storyGameMode.didStartCycle;
                readyForGate = storyGameMode.readyForGate;
                if (currentGameState?.session is StoryGameSession storySession)
                {
                    cycleNumber = storySession.saveState.cycleNumber;
                    karma = storySession.saveState.deathPersistentSaveData.karma;
                    karmaCap = storySession.saveState.deathPersistentSaveData.karmaCap;
                    theGlow = storySession.saveState.theGlow;
                    reinforcedKarma = storySession.saveState.deathPersistentSaveData.reinforcedKarma;
                }

                food = (currentGameState?.Players[0].state as PlayerState)?.foodInStomach ?? 0;
                quarterfood = (currentGameState?.Players[0].state as PlayerState)?.quarterFoodPoints ?? 0;
                mushroomCounter = (currentGameState?.Players[0].realizedCreature as Player)?.mushroomCounter ?? 0;
                friendlyFire = storyGameMode.friendlyFire;
                region = storyGameMode.region;
            }

            public override Type GetDataType() => typeof(StoryLobbyData);

            public override void ReadTo(ResourceData data, OnlineResource resource)
            {
                RainWorldGame currentGameState = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;
                var playerstate = (currentGameState?.Players[0].state as PlayerState);
                var lobby = (resource as Lobby);

                (lobby.gameMode as StoryGameMode).defaultDenPos = defaultDenPos;

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
                    storySession.saveState.cycleNumber = cycleNumber;
                    storySession.saveState.deathPersistentSaveData.karma = karma;
                    storySession.saveState.deathPersistentSaveData.karmaCap = karmaCap;
                    storySession.saveState.deathPersistentSaveData.reinforcedKarma = reinforcedKarma;
                    storySession.saveState.theGlow = theGlow;
                    if ((RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame rainWorldGame))
                    {
                        if (rainWorldGame.Players[0].realizedCreature != null)
                            (rainWorldGame.Players[0].realizedCreature as Player).glowing = theGlow;
                    }
                }
                (lobby.gameMode as StoryGameMode).currentCampaign = currentCampaign;
                (lobby.gameMode as StoryGameMode).ghostsTalkedTo = ghostsTalkedTo;
                (lobby.gameMode as StoryGameMode).consumedItems = consumedItems;
                (lobby.gameMode as StoryGameMode).storyBoolRemixSettings = storyBoolRemixSettings;
                (lobby.gameMode as StoryGameMode).storyFloatRemixSettings = storyFloatRemixSettings;
                (lobby.gameMode as StoryGameMode).storyIntRemixSettings = storyIntRemixSettings;

                (lobby.gameMode as StoryGameMode).isInGame = isInGame;
                (lobby.gameMode as StoryGameMode).changedRegions = changedRegions;
                (lobby.gameMode as StoryGameMode).didStartCycle = didStartCycle;
                (lobby.gameMode as StoryGameMode).readyForGate = readyForGate;
                (lobby.gameMode as StoryGameMode).friendlyFire = friendlyFire;
                (lobby.gameMode as StoryGameMode).region = region;



            }
        }
    }
}
