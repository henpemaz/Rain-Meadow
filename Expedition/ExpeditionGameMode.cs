using System.Collections.Generic;

namespace RainMeadow
{
    public class ExpeditionGameMode : StoryGameMode//OnlineGameMode//
    {
        public List<string> activeUnlocks;
        public List<string> challengeListStrings;
        public ChallengeState[] currentChallengeList;

        public int slugcatCampaingSelected;
        public bool needSlugUpdate = false;
        public bool hasSaveState;

        public List<OnlineCreature> challengeKills;

        public int challengeIndex; // hack - see ExpeditionHooks GetChallengeIndex
        public bool[] isChallengeCompleted;
        public bool needUpdateChallenge;

        public List<OnlineCreature> pinChallenge_PinList;
        public int earnedPassages;

        public ExpeditionGameMode(Lobby lobby) : base(lobby)
        {
            avatarSettings = new SlugcatCustomization[4];
            for (int i = 0; i < avatarSettings.Length; i++)
            {
                avatarSettings[i] = new SlugcatCustomization() { nickname = OnlineManager.mePlayer.id.name };
            }
        }

        public override bool AllowedInMode(PlacedObject item)
        {
            return base.AllowedInMode(item);
        }

        public override bool ShouldLoadCreatures(RainWorldGame game, WorldSession worldSession)
        {
            return base.ShouldLoadCreatures(game, worldSession);
        }

        public override bool ShouldSpawnRoomItems(RainWorldGame game, RoomSession roomSession)
        {
            return base.ShouldSpawnRoomItems(game, roomSession);
        }

        public override bool ShouldRegisterAPO(OnlineResource resource, AbstractPhysicalObject apo)
        {
            return base.ShouldRegisterAPO(resource, apo);
        }

        public override bool ShouldSyncAPOInRoom(RoomSession rs, AbstractPhysicalObject apo)
        {
            return base.ShouldSyncAPOInRoom(rs, apo);
        }

        public override bool ShouldSyncAPOInWorld(WorldSession ws, AbstractPhysicalObject apo)
        {
            return base.ShouldSyncAPOInWorld(ws, apo);
        }

        public override SlugcatStats.Name GetStorySessionPlayer(RainWorldGame self)
        {
            return currentCampaign;
        }

        public override SlugcatStats.Name LoadWorldAs(RainWorldGame game)
        {
            return currentCampaign;
        }

        public override SlugcatStats.Timeline LoadWorldIn(RainWorldGame game)
        {
            return game?.GetStorySession.saveState.currentTimelinePosition ?? SlugcatStats.SlugcatToTimeline(currentCampaign);
        }

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

        public override void ConfigureAvatar(OnlineCreature onlineCreature)
        {
            int avatarsettings_index = 0;
            if (onlineCreature.abstractCreature.state is PlayerState state)
            {
                avatarsettings_index = state.playerNumber;
            }
            else RainMeadow.Error("No PlayerState for playerNumber");

            onlineCreature.AddData(avatarSettings[avatarsettings_index]);
            avatarSettings[avatarsettings_index].overlaySkin = AvatarData.ConfigureOverlay(onlineCreature);
        }

        public override void Customize(Creature creature, OnlineCreature oc)
        {
            base.Customize(creature, oc);
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
