using Expedition;
using System;
using System.Collections.Generic;
using System.Linq;
using RainMeadow.Generics;

namespace RainMeadow;

public class ExpeditionLobbyData : StoryLobbyData
{
    public override ResourceDataState MakeState(OnlineResource resource)
    {
        return new ExpeditionState(this, resource);
    }

    public class ExpeditionState : StoryLobbyData.State
    {
        [OnlineField]
        public bool hasSaveState;

        [OnlineField]
        public int earnedPassages;

        [OnlineFieldHalf]
        public float slowTimeCooldown;


        [OnlineField(group = "expeditionChallenge", polymorphic = true)]
        DynamicOrderedStates<ChallengeState> currentChallengeList;


        [OnlineField(group = "expeditionSetup")]
        public int selectedSlugcat;

        [OnlineField(group = "expeditionSetup", nullable = true)]
        public string? activeMission;

        [OnlineField(group = "expeditionSetup", nullable = true)]
        public List<string> activeUnlocks = new List<string>();

        public ExpeditionState() { }
        public ExpeditionState(ExpeditionLobbyData expeditionLobbyData, OnlineResource onlineResource) : base(expeditionLobbyData, onlineResource)
        {
            ExpeditionGameMode expeditionGameMode = (ExpeditionGameMode)((Lobby)onlineResource).gameMode;
            RainWorldGame currentGameState = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;

            for (int i = 0; i < ExpeditionGame.unlockTrackers.Count; i++)
            {
                if (ExpeditionGame.unlockTrackers[i] is ExpeditionGame.SlowTimeTracker stt)
                    slowTimeCooldown = stt.cooldown;
            }

            if (currentCampaign is not null)
            {
                if (expeditionGameMode.currentChallengeList is null || expeditionGameMode.currentChallengeList.Count != ExpeditionData.allChallengeLists[currentCampaign].Count)
                {
                    expeditionGameMode.challengeListStrings = ExpeditionData.allChallengeLists[currentCampaign].Select(x => x.ToString()).ToList();

                    List<ChallengeState> challengeStateList = new List<ChallengeState>();
                    foreach (Challenge challenge in ExpeditionData.allChallengeLists[currentCampaign])
                    {
                        challengeStateList.Add(GetChallengeState(challenge));
                    }
                    expeditionGameMode.currentChallengeList = new(challengeStateList);
                }
                else
                {
                    for (int i = 0; i < expeditionGameMode.challengeListStrings.Count; i++)
                    {
                        var challenge = ExpeditionData.allChallengeLists[currentCampaign][i];
                        if (expeditionGameMode.challengeListStrings[i] == challenge.ToString()) continue;
                        
                        expeditionGameMode.currentChallengeList[i] = GetChallengeState(challenge);
                        expeditionGameMode.challengeListStrings[i] = challenge.ToString();
                        
                    }
                }
            }

            if (expeditionGameMode.activeUnlocks is null || !ExpeditionGame.activeUnlocks.SequenceEqual(expeditionGameMode.activeUnlocks))
            {
                expeditionGameMode.activeUnlocks = new List<string>(ExpeditionGame.activeUnlocks);
            }

            selectedSlugcat = expeditionGameMode.slugcatCampaingSelected;
            currentChallengeList = new DynamicOrderedStates<ChallengeState>(expeditionGameMode.currentChallengeList.ToList());
            activeUnlocks = expeditionGameMode.activeUnlocks;
            activeMission = ExpeditionData.activeMission;
            earnedPassages = ExpeditionData.earnedPassages;
            hasSaveState = expeditionGameMode.hasSaveState;
        }

        public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
        {
            base.ReadTo(data, resource);

            Lobby lobby = (Lobby)resource;
            ExpeditionGameMode expedition = (ExpeditionGameMode)lobby.gameMode;

            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame currentGameState)
            {
                for (int i = 0; i < ExpeditionGame.unlockTrackers.Count; i++)
                {
                    if (ExpeditionGame.unlockTrackers[i] is ExpeditionGame.SlowTimeTracker stt)
                        stt.cooldown = slowTimeCooldown;
                }
            }

            if (expedition.slugcatCampaingSelected != selectedSlugcat)
            {
                expedition.slugcatCampaingSelected = selectedSlugcat;
                expedition.needSlugUpdate = true;
            }

            expedition.hasSaveState = hasSaveState;
            expedition.activeUnlocks = activeUnlocks;
            expedition.earnedPassages = earnedPassages;
            expedition.activeMission = activeMission ?? "";

            if (!ExpeditionGame.expeditionComplete)
            {
                if (!ExpeditionData.allChallengeLists.ContainsKey(currentCampaign) || ExpeditionData.allChallengeLists[currentCampaign].Count != currentChallengeList.list.Count)
                {
                    List<Challenge> challengeList = new List<Challenge>();
                    foreach (ChallengeState challengeState in currentChallengeList.list)
                    {
                        var _challenge = challengeState.CreateChallenge();                 
                        challengeList.Add(_challenge);
                    }

                    ExpeditionData.allChallengeLists[currentCampaign] = challengeList;
                    expedition.isChallengeCompleted = new bool[challengeList.Count];
                }

                if (expedition.challengeListStrings is null || expedition.challengeListStrings.Count != currentChallengeList.list.Count)
                {
                    string[] challengeStrings = new string[currentChallengeList.list.Count];
                    expedition.challengeListStrings = new List<string>(challengeStrings);
                }

                List<Challenge> challenges = ExpeditionData.allChallengeLists[currentCampaign];

                for (int i = 0; i < currentChallengeList.list.Count; i++)
                {
                    Challenge challenge = challenges[i];
                    bool oldCompleteState = challenge.completed;

                    currentChallengeList.list[i].ReadTo(ref challenge);
                    challenges[i] = challenge;

                    if (expedition.challengeListStrings[i] == challenge.ToString()) continue;

                    expedition.challengeListStrings[i] = challenge.ToString();
                    expedition.needUpdateeChallengePreview = true;

                    if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame)
                    {
                        bool newCompleteState = challenge.completed;

                        if (!oldCompleteState && newCompleteState)
                        {
                            challenge.completed = false;
                            expedition.isChallengeCompleted[i] = true;

                            expedition.challengeIndex = i;
                            challenge.CompleteChallenge();
                            expedition.challengeIndex = -1;
                        }
                        challenge.UpdateDescription();
                    }
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
