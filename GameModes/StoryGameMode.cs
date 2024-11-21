using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public class StoryGameMode : OnlineGameMode
    {
        // these are synced by StoryLobbyData
        public bool isInGame = false;
        public bool changedRegions = false;
        public bool didStartCycle = false;
        public bool friendlyFire = false; // false until we manage it via UI
        public string? defaultDenPos;
        public string? myLastDenPos = null;
        public string? region = null;

        public bool hasSheltered = false;
        public SlugcatStats.Name currentCampaign;
        public Dictionary<string, int> ghostsTalkedTo;
        public Dictionary<string, bool> storyBoolRemixSettings;
        public Dictionary<string, float> storyFloatRemixSettings;
        public Dictionary<string, int> storyIntRemixSettings;
        public Dictionary<ushort, ushort[]> consumedItems;
        public StoryClientSettingsData storyClientData;


        public bool saveToDisk = false;
      

        public SlugcatCustomization avatarSettings;

        public StoryGameMode(Lobby lobby) : base(lobby)
        {
            avatarSettings = new SlugcatCustomization() { nickname = OnlineManager.mePlayer.id.name };
        }
        public override ProcessManager.ProcessID MenuProcessId()
        {
            return RainMeadow.Ext_ProcessID.StoryMenu;
        }
        public override bool AllowedInMode(PlacedObject item)
        {
            return base.AllowedInMode(item) || OnlineGameModeHelpers.PlayerGrabbableItems.Contains(item.type) || OnlineGameModeHelpers.creatureRelatedItems.Contains(item.type);
        }
        public override bool ShouldLoadCreatures(RainWorldGame game, WorldSession worldSession)
        {
            if (OnlineManager.mePlayer.isActuallySpectating)
            {
                return false;
            }

            return worldSession.owner == null || worldSession.isOwner;
        }

        public override bool ShouldSpawnRoomItems(RainWorldGame game, RoomSession roomSession)
        {

            if (OnlineManager.mePlayer.isActuallySpectating)
            {
                return false;
            }

            return roomSession.owner == null || roomSession.isOwner;
            // todo if two join at once, this first check is faulty
        }

        public override bool ShouldSyncAPOInWorld(WorldSession ws, AbstractPhysicalObject apo)
        {

            return true;
        }

        public override SlugcatStats.Name GetStorySessionPlayer(RainWorldGame self)
        {
            return currentCampaign;
        }
        public override SlugcatStats.Name LoadWorldAs(RainWorldGame game)
        {
            return currentCampaign;
        }

        public override bool ShouldSpawnFly(FliesWorldAI self, int spawnRoom)
        {
            if (OnlineManager.mePlayer.isActuallySpectating)
            {
                return false;
            }
            return true;
        }

        public override bool PlayerCanOwnResource(OnlinePlayer from, OnlineResource onlineResource)
        {
            if (onlineResource is WorldSession)
            {
                return lobby.owner == from;
            }
            return true;
        }

        public override void AddClientData()
        {
            storyClientData = clientSettings.AddData(new StoryClientSettingsData());
        }

        public override void LobbyTick(uint tick)
        {
            base.LobbyTick(tick);
            // could switch this based on rules? any vs all
            storyClientData.isDead = avatars.All(a => a.abstractCreature.state is PlayerState state && (state.dead || state.permaDead));
        }

        public override void PlayerLeftLobby(OnlinePlayer player)
        {
            base.PlayerLeftLobby(player);
            if (player == lobby.owner)
            {
                OnlineManager.instance.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
            }
        }

        public override void ResourceAvailable(OnlineResource onlineResource)
        {
            base.ResourceAvailable(onlineResource);
            if (onlineResource is Lobby lobby)
            {
                lobby.AddData(new StoryLobbyData());
            }
        }

        public override void ResourceActive(OnlineResource onlineResource)
        {
            base.ResourceActive(onlineResource);
            if (onlineResource is WorldSession ws)
            {
                var regionState = ws.world.regionState;
                if (lobby.isOwner)
                {
                    ghostsTalkedTo = regionState.saveState.deathPersistentSaveData.ghostsTalkedTo.ToDictionary(kvp => kvp.Key.value, kvp => kvp.Value);
                    consumedItems = regionState.consumedItems
                        .Concat(regionState.saveState.deathPersistentSaveData.consumedFlowers) // HACK: group karma flowers with items, room:index shouldn't overlap
                        .GroupBy(x => x.originRoom)
                        .ToDictionary(x => (ushort)x.Key, x => x.Select(y => (ushort)y.placedObjectIndex).ToArray());
                }
                else
                {
                    regionState.consumedItems = consumedItems
                        .SelectMany(kvp => kvp.Value.Select(v => new RegionState.ConsumedItem(kvp.Key, v, 2))).ToList(); // must be >1
                    regionState.saveState.deathPersistentSaveData.consumedFlowers = regionState.consumedItems;
                }
            }
        }

        public override void ConfigureAvatar(OnlineCreature onlineCreature)
        {
            onlineCreature.AddData(avatarSettings);
        }

        public override void Customize(Creature creature, OnlineCreature oc)
        {
            if (oc.TryGetData<SlugcatCustomization>(out var data))
            {
                RainMeadow.Debug(oc);
                RainMeadow.creatureCustomizations.GetValue(creature, (c) => data);
            }
        }

        public override void PreGameStart()
        {
            base.PreGameStart();
            changedRegions = false;
            hasSheltered = false;
            storyClientData.isDead = false;
        }
    }
}
