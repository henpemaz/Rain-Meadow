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
            public bool readyForWin;
            [OnlineField]
            public byte readyForGate, difficultyMode;
            [OnlineField]
            public bool friendlyFire;
            [OnlineField]
            public SlugcatStats.Name currentCampaign;
            [OnlineField]
            public int cycleNumber;
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
            [OnlineField(nullable = true)]
            public string? saveStateString;
            [OnlineField]
            public bool requireCampaignSlugcat;
            [OnlineField]
            public List<OnlineEntity.EntityId> pups;
            public State() { }

            public State(StoryLobbyData storyLobbyData, OnlineResource onlineResource)
            {
                StoryGameMode storyGameMode = (onlineResource as Lobby).gameMode as StoryGameMode;
                RainWorldGame currentGameState = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;

                defaultDenPos = storyGameMode.defaultDenPos;
                currentCampaign = storyGameMode.currentCampaign;
                requireCampaignSlugcat = storyGameMode.requireCampaignSlugcat;
               
                isInGame = RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame && RWCustom.Custom.rainWorld.processManager.upcomingProcess is null;
                changedRegions = storyGameMode.changedRegions;
                readyForWin = storyGameMode.readyForWin;
                readyForGate = (byte)storyGameMode.readyForGate;
                difficultyMode = (byte)storyGameMode.difficultyMode;
                saveStateString = storyGameMode.saveStateString;
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

                pups = new();
                foreach (AbstractCreature apo in storyGameMode.pups) {
                    if (OnlinePhysicalObject.map.TryGetValue(apo, out var oe)) {
                        pups.Add(oe.id);
                    }
                }
                friendlyFire = storyGameMode.friendlyFire;
                region = storyGameMode.region;
            }

            public override Type GetDataType() => typeof(StoryLobbyData);

            public override void ReadTo(ResourceData data, OnlineResource resource)
            {
                RainWorldGame currentGameState = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;
                var playerstate = (currentGameState?.Players[0].state as PlayerState);
                var lobby = (resource as Lobby);
                StoryGameMode storyGameMode = (lobby!.gameMode as StoryGameMode)!;
                storyGameMode.defaultDenPos = defaultDenPos;

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
                storyGameMode.currentCampaign = currentCampaign;

                storyGameMode.requireCampaignSlugcat = requireCampaignSlugcat;
                storyGameMode.isInGame = isInGame;
                storyGameMode.changedRegions = changedRegions;
                storyGameMode.readyForWin = readyForWin;
                storyGameMode.readyForGate = (StoryGameMode.ReadyForGate)readyForGate;
                storyGameMode.difficultyMode = (StoryGameMode.DifficultyMode)difficultyMode;
                storyGameMode.friendlyFire = friendlyFire;
                storyGameMode.region = region;

                storyGameMode.saveStateString = saveStateString;


                foreach (OnlineEntity.EntityId pupid in pups) {
                    if ((pupid.FindEntity() as OnlineCreature)?.apo is AbstractCreature apo) {
                        storyGameMode.pups.Add(apo);
                    }
                }
            }
        }
    }
}
