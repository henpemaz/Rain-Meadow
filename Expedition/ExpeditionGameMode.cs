using System.Collections.Generic;

namespace RainMeadow
{
    public class ExpeditionGameMode : StoryGameMode
    {
        public List<string> activeUnlocks;
        public List<string> challengeListStrings;
        public List<ChallengeState> currentChallengeList;

        public int slugcatCampaingSelected;
        public bool needSlugUpdate = false;
        public bool hasSaveState;

        public List<OnlineCreature> challengeKills;

        public int challengeIndex; // hack - see ExpeditionHooks GetChallengeIndex
        public bool[] isChallengeCompleted;
        public bool needUpdateChallenge;

        public List<OnlineCreature> pinChallenge_PinList;
        public int earnedPassages;
        public bool needUpdateeChallengePreview;
        public string activeMission;

        public ExpeditionGameMode(Lobby lobby) : base(lobby) { }

        public override ProcessManager.ProcessID MenuProcessId()
        {
            return RainMeadow.Ext_ProcessID.ExpeditionMenu;
        }

        public override void NewResourceOwner(OnlineResource resource, OnlinePlayer? oldOwner, OnlinePlayer? newOwner)
        {
            if (resource is Lobby)
            {
                if (OnlineManager.instance.manager.currentMainLoop is RainWorldGame)
                {
                    OnlineManager.instance.manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.ExpeditionMenu);
                }
            }

            if (lobby.isOwner)
            {
                needMenuSaveUpdate = true;
                needSlugUpdate = true;
            }
        }

        public override void ResourceAvailable(OnlineResource onlineResource)
        {
            RainMeadow.Debug(onlineResource);
            if (onlineResource is Lobby lobby)
            {
                lobby.AddData(new ExpeditionLobbyData());
            }
        }
    }
}
