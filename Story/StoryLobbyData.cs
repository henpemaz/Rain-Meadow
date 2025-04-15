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
            [OnlineField(nullable = true)]
            public string? defaultWarpPos; //this is mainly so clients do not brick saves when activating echoes
            [OnlineField]
            public bool isInGame;
            [OnlineField]
            public bool changedRegions;
            [OnlineField]
            public bool readyForWin;
            [OnlineField]
            public byte readyForGate;
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
            //watcher stuff
            [OnlineFieldHalf]
            public float rippleLevel;
            [OnlineFieldHalf]
            public float minimumRippleLevel;
            [OnlineFieldHalf]
            public float maximumRippleLevel;

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
                saveStateString = storyGameMode.saveStateString;
                if (currentGameState?.session is StoryGameSession storySession)
                {
                    cycleNumber = storySession.saveState.cycleNumber;
                    karma = storySession.saveState.deathPersistentSaveData.karma;
                    karmaCap = storySession.saveState.deathPersistentSaveData.karmaCap;
                    rippleLevel = storySession.saveState.deathPersistentSaveData.rippleLevel;
                    minimumRippleLevel = storySession.saveState.deathPersistentSaveData.minimumRippleLevel;
                    maximumRippleLevel = storySession.saveState.deathPersistentSaveData.maximumRippleLevel;
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
                    storySession.saveState.deathPersistentSaveData.rippleLevel = rippleLevel;
                    storySession.saveState.deathPersistentSaveData.minimumRippleLevel = minimumRippleLevel;
                    storySession.saveState.deathPersistentSaveData.maximumRippleLevel = maximumRippleLevel;
                    storySession.saveState.deathPersistentSaveData.reinforcedKarma = reinforcedKarma;
                    storySession.saveState.theGlow = theGlow;
                    if ((RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame rainWorldGame))
                    {
                        if (rainWorldGame.Players[0].realizedCreature != null)
                            (rainWorldGame.Players[0].realizedCreature as Player).glowing = theGlow;
                    }
                }
                (lobby.gameMode as StoryGameMode).currentCampaign = currentCampaign;

                (lobby.gameMode as StoryGameMode).requireCampaignSlugcat = requireCampaignSlugcat;
                (lobby.gameMode as StoryGameMode).isInGame = isInGame;
                (lobby.gameMode as StoryGameMode).changedRegions = changedRegions;
                (lobby.gameMode as StoryGameMode).readyForWin = readyForWin;
                (lobby.gameMode as StoryGameMode).readyForGate = (StoryGameMode.ReadyForGate)readyForGate;
                (lobby.gameMode as StoryGameMode).friendlyFire = friendlyFire;
                (lobby.gameMode as StoryGameMode).region = region;

                (lobby.gameMode as StoryGameMode).saveStateString = saveStateString;


                foreach (OnlineEntity.EntityId pupid in pups) {
                    if ((pupid.FindEntity() as OnlineCreature)?.apo is AbstractCreature apo) {
                        (lobby.gameMode as StoryGameMode).pups.Add(apo);
                    }
                }
            }
        }
    }
}
