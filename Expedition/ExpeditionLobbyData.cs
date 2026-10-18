using Expedition;
using System;
using System.Collections.Generic;
using System.Linq;

using static RainMeadow.OnlineResource;
using static RainMeadow.StoryLobbyData;

namespace RainMeadow;

public class ExpeditionLobbyData : OnlineResource.ResourceData
{
    public override ResourceDataState MakeState(OnlineResource resource)
    {
        return new State(this, resource);
    }

    public class State : ResourceDataState
    {
        [OnlineField]
        public bool isInGame;
        
        [OnlineField]
        public bool readyForWin;

        [OnlineField]
        public bool changedRegions;
        
        [OnlineField]
        public bool hasSaveState;

        [OnlineField]
        public bool malnourished;

        [OnlineField]
        public bool lastMalnourished;

        [OnlineField]
        public bool reinforcedKarma;

        [OnlineField]
        public bool theGlow;
        
        [OnlineField]
        public byte readyForTransition;

        [OnlineField]
        public int food;

        [OnlineField]
        public int quarterfood;

        [OnlineField]
        public int mushroomCounter;

        [OnlineField]
        public int earnedPassages;

        [OnlineFieldHalf]
        public float slowTimeCooldown;

        [OnlineField(nullable = true)]
        public string? region;

        [OnlineField]
        public List<OnlineEntity.EntityId> pups;


        [OnlineField(group = "saveString", nullable = true)]
        public string? saveStateString;
        
        [OnlineField(group = "menusave", nullable = true)]
        public MenuSaveStateState? currentMenuSaveState;


        [OnlineField(group = "expeditiondChallenge")]
        bool[] isChallengeCompleted;

        [OnlineField(group = "expeditiondChallenge", polymorphic = true)]
        ChallengeState[] currentChallengeList;


        [OnlineField(group = "expeditionSetup")]
        public bool friendlyFire;

        [OnlineField(group = "expeditionSetup")]
        public bool requireCampaignSlugcat;

        [OnlineField(group = "expeditionSetup")]
        public bool itemSteal;

        [OnlineField(group = "expeditionSetup")]
        public int selectedSlugcat;

        [OnlineField(group = "expeditionSetup", nullable = true)]
        public string? activeMission;
        
        [OnlineField(group = "expeditionSetup", nullable = true)]
        public string? startingDen;

        [OnlineField(group = "expeditionSetup")]
        SlugcatStats.Name currentCampaign;

        [OnlineField(group = "expeditionSetup", nullable = true)]
        public List<string> activeUnlocks = new List<string>();

        public State() { }
        public State(ExpeditionLobbyData expeditionLobbyData, OnlineResource onlineResource)
        {
            ExpeditionGameMode expeditionGameMode = (ExpeditionGameMode)((Lobby)onlineResource).gameMode;
            RainWorldGame currentGameState = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;

            PlayerState ps = currentGameState?.Players[0].state as PlayerState;

            food = (currentGameState?.Players[0].state as PlayerState)?.foodInStomach ?? 0;
            quarterfood = (currentGameState?.Players[0].state as PlayerState)?.quarterFoodPoints ?? 0;
            mushroomCounter = (currentGameState?.Players[0].realizedCreature as Player)?.mushroomCounter ?? 0;

            for (int i = 0; i < ExpeditionGame.unlockTrackers.Count; i++)
            {
                if (ExpeditionGame.unlockTrackers[i] is ExpeditionGame.SlowTimeTracker stt)
                    slowTimeCooldown = stt.cooldown;
            }

            isInGame = RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame && RWCustom.Custom.rainWorld.processManager.upcomingProcess is null;

            readyForWin = expeditionGameMode.readyForWin;
            readyForTransition = (byte)expeditionGameMode.readyForTransition;
            saveStateString = expeditionGameMode.saveStateString;
            selectedSlugcat = expeditionGameMode.slugcatCampaingSelected;
            currentCampaign = expeditionGameMode.currentCampaign;
            requireCampaignSlugcat = expeditionGameMode.requireCampaignSlugcat;
            friendlyFire = expeditionGameMode.friendlyFire;
            changedRegions = expeditionGameMode.changedRegions;
            region = expeditionGameMode.region;
            itemSteal = expeditionGameMode.itemSteal;

            if (currentCampaign is not null)
            {
                if (expeditionGameMode.challengeListStrings is null ||
                    expeditionGameMode.challengeListStrings.Count != ExpeditionData.allChallengeLists[currentCampaign].Count ||
                    !expeditionGameMode.challengeListStrings.SequenceEqual(ExpeditionData.allChallengeLists[currentCampaign].Select(x => x.ToString())))
                {
                    expeditionGameMode.challengeListStrings = ExpeditionData.allChallengeLists[currentCampaign].Select(x => x.ToString()).ToList();

                    List<ChallengeState> challengeStateList = new List<ChallengeState>();
                    foreach (Challenge challenge in ExpeditionData.allChallengeLists[currentCampaign])
                    {
                        challengeStateList.Add(GetChallengeState(challenge));
                    }
                    expeditionGameMode.currentChallengeList = challengeStateList.ToArray();
                }
            }

            if (expeditionGameMode.activeUnlocks is null || !ExpeditionGame.activeUnlocks.SequenceEqual(expeditionGameMode.activeUnlocks))
            {
                expeditionGameMode.activeUnlocks = new List<string>(ExpeditionGame.activeUnlocks);
            }

            currentChallengeList = expeditionGameMode.currentChallengeList;
            activeUnlocks = expeditionGameMode.activeUnlocks;
            activeMission = ExpeditionData.activeMission;
            startingDen = ExpeditionData.startingDen;
            earnedPassages = ExpeditionData.earnedPassages;

            if (currentGameState?.session is StoryGameSession storySession)
            {                
                theGlow = storySession.saveState.theGlow;
                reinforcedKarma = storySession.saveState.deathPersistentSaveData.reinforcedKarma;
                malnourished = storySession.saveState.malnourished;
                lastMalnourished = storySession.saveState.lastMalnourished;
                
                expeditionGameMode.menuSaveState = new MenuSaveStateState(storySession.saveState);
                currentMenuSaveState = expeditionGameMode.menuSaveState;
                hasSaveState = saveStateString != null;
            }
            else
            {
                currentMenuSaveState = expeditionGameMode.menuSaveState;
                hasSaveState = expeditionGameMode.hasSaveState;
            }
           
            int cclCount = currentChallengeList.Length;

            if (expeditionGameMode.isChallengeCompleted == null || expeditionGameMode.isChallengeCompleted.Length != cclCount)
            {
                expeditionGameMode.isChallengeCompleted = new bool[cclCount];
            }

            isChallengeCompleted = new bool[cclCount];

            for (int i = 0; i < cclCount; i++)
            {
                isChallengeCompleted[i] = expeditionGameMode.isChallengeCompleted[i] = currentChallengeList[i].completed;
            }

            pups = new();
            foreach (AbstractCreature apo in expeditionGameMode.pups)
            {
                if (OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
                {
                    pups.Add(oe.id);
                }
            }
        }

        public override void ReadTo(ResourceData data, OnlineResource resource)
        {
            Lobby lobby = (Lobby)resource;
            ExpeditionGameMode expedition = (ExpeditionGameMode)lobby.gameMode;
            expedition.currentCampaign = currentCampaign;

            if (expedition.menuSaveState != currentMenuSaveState)
            {
                expedition.menuSaveState = currentMenuSaveState;
                expedition.menuSaveGameData = expedition.menuSaveState?.CreateSaveData();
                expedition.needMenuSaveUpdate = true;
            }

            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame currentGameState)
            {
                for (int i = 0; i < currentGameState.StoryPlayerCount; i++)
                {
                    PlayerState ps = (PlayerState)currentGameState.Players[i].state;

                    ps.foodInStomach = food;
                    ps.quarterFoodPoints = quarterfood;

                    if ((currentGameState?.Players[i].realizedCreature is Player player))
                    {
                        player.mushroomCounter = mushroomCounter;
                        player.AddFood(0);
                    }
                }

                for (int i = 0; i < ExpeditionGame.unlockTrackers.Count; i++)
                {
                    if (ExpeditionGame.unlockTrackers[i] is ExpeditionGame.SlowTimeTracker stt)
                        stt.cooldown = slowTimeCooldown;
                }

                if (currentGameState.session is StoryGameSession storySession)
                {                    
                    storySession.saveState.deathPersistentSaveData.reinforcedKarma = reinforcedKarma;
                    storySession.saveState.theGlow = theGlow;
                    storySession.saveState.lastMalnourished = lastMalnourished;
                    storySession.saveState.malnourished = malnourished;

                    for (int i = 0; i < currentGameState.StoryPlayerCount; i++)
                    {
                        if (currentGameState.Players[i].realizedCreature != null)
                            (currentGameState.Players[i].realizedCreature as Player).glowing = theGlow;
                    }
                }
            }

            expedition.isInGame = isInGame;
            expedition.readyForWin = readyForWin;
            expedition.readyForTransition = (StoryGameMode.ReadyForTransition)readyForTransition;
            expedition.saveStateString = saveStateString;
            expedition.requireCampaignSlugcat = requireCampaignSlugcat;
            expedition.friendlyFire = friendlyFire;
            expedition.changedRegions = changedRegions;
            expedition.region = region;
            expedition.itemSteal = itemSteal;           

            expedition.hasSaveState = hasSaveState;

            if (expedition.slugcatCampaingSelected != selectedSlugcat)
            {
                expedition.slugcatCampaingSelected = selectedSlugcat;
                expedition.needSlugUpdate = true;
            }

            ExpeditionData.activeMission = activeMission;
            ExpeditionData.startingDen = startingDen;

            expedition.activeUnlocks = activeUnlocks;                
            expedition.earnedPassages = earnedPassages;

            if (!ExpeditionGame.expeditionComplete)
            {
                if (!ExpeditionData.allChallengeLists.ContainsKey(currentCampaign) || ExpeditionData.allChallengeLists[currentCampaign].Count != currentChallengeList.Length)
                {
                    List<Challenge> challengeList = new List<Challenge>();
                    foreach (ChallengeState challengeState in currentChallengeList)
                    {
                        var _challenge = challengeState.GetChallenge;                            
                        challengeList.Add(_challenge);
                    }
                    ExpeditionData.allChallengeLists[currentCampaign] = challengeList;
                }

                List<Challenge> challenge = ExpeditionData.allChallengeLists[currentCampaign];
  
                if (expedition.challengeListStrings is null || expedition.challengeListStrings.Count != currentChallengeList.Length)
                {
                    string[] challengeStrings = new string[currentChallengeList.Length];                   
                    expedition.challengeListStrings = new List<string>(challengeStrings);
                }

                bool needUpdateeChallengePreview = false;

                for (int i = 0; i < currentChallengeList.Length; i++)
                {
                    if (challenge[i].GetType() != currentChallengeList[i].ChallengeType)
                    {
                        challenge[i] = currentChallengeList[i].GetChallenge;
                    }

                    bool oldCompleteState = challenge[i].completed;
                    bool needUpdate = false;

                    currentChallengeList[i].ReadTo(challenge[i]);

                    if (expedition.challengeListStrings[i] != challenge[i].ToString())
                    {
                        expedition.challengeListStrings[i] = challenge[i].ToString();
                        needUpdate = true;
                        needUpdateeChallengePreview = true;
                    }

                    if (needUpdate)
                    {
                        needUpdate = false;
                        if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame)
                        {
                            bool newCompleteState = challenge[i].completed;

                            if (oldCompleteState != newCompleteState && newCompleteState)
                            {
                                challenge[i].completed = oldCompleteState;
                                challenge[i].CompleteChallenge();
                            }

                            challenge[i].UpdateDescription();
                        }
                    }
                }
                
                if (needUpdateeChallengePreview && RWCustom.Custom.rainWorld.processManager.currentMainLoop is ExpeditionOnlineMenu eom)
                {
                    eom.characterSelect.UpdateChallengePreview();
                }
            }                

            expedition.isChallengeCompleted = isChallengeCompleted;

            foreach (OnlineEntity.EntityId pupid in pups)
            {
                if ((pupid.FindEntity() as OnlineCreature)?.apo is AbstractCreature apo)
                {
                    (lobby.gameMode as ExpeditionGameMode).pups.Add(apo);
                }
            }
        }

        public ChallengeState GetChallengeState(Challenge challenge) // modders can hook this to custom challenges
        {
            if (challenge is AchievementChallenge) return new AchievementChallengeState(challenge);
            if (challenge is CycleScoreChallenge) return new CycleScoreChallengeState(challenge);
            if (challenge is EchoChallenge) return new EchoChallengeState(challenge);
            if (challenge is GlobalScoreChallenge) return new GlobalScoreChallengeState(challenge);
            if (challenge is HuntChallenge) return new HuntChallengeState(challenge);
            if (challenge is ItemHoardChallenge) return new ItemHoardChallengeState(challenge);
            if (challenge is NeuronDeliveryChallenge) return new NeuronDeliveryChallengeState(challenge);
            if (challenge is PearlDeliveryChallenge) return new PearlDeliveryChallengeState(challenge);
            if (challenge is PearlHoardChallenge) return new PearlHoardChallengeState(challenge);
            if (challenge is PinChallenge) return new PinChallengeState(challenge);
            if (challenge is VistaChallenge) return new VistaChallengeState(challenge);

            throw new NotImplementedException($"challenge type: {challenge.GetType()} not implemented");
        }

        public override Type GetDataType()
        {
            return typeof(ExpeditionLobbyData);
        }
    }
}
