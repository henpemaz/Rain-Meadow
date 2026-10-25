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

        public override AbstractCreature SpawnAvatar(RainWorldGame self, WorldCoordinate location)
        {
            AbstractCreature creature;
            AbstractCreature mainAvatar = null;
            for (int i = 0; i < self.StoryPlayerCount; i++)
            {
                creature = new AbstractCreature(self.world, StaticWorld.GetCreatureTemplate("Slugcat"), null, location, new EntityID(-1, i));
                creature.state = new PlayerState(creature, i, avatarSettings[i].playingAs, false) { isPup = avatarSettings[i].fakePup }; // look at avatarSettings

                self.world.GetAbstractRoom(creature.pos.room).AddEntity(creature);
                self.session.AddPlayer(creature);

                if (i == 0) mainAvatar = creature;
            }
            if (mainAvatar is null) throw new InvalidProgrammerException("Main avatar is null");
            return mainAvatar;
        }
    }
}
