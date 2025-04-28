using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public class StoryGameMode : OnlineGameMode
    {
        // these are synced by StoryLobbyData
        public bool isInGame = false;
        public bool changedRegions = false;
        public bool readyForWin = false;
        public enum ReadyForTransition : byte
        {
            Closed,
            MeetRequirement,
            Opening,
            Crossed,
        }
        public ReadyForTransition readyForTransition = ReadyForTransition.Closed;
        public bool friendlyFire = false;
        public string? defaultDenPos;
        public string? region = null;
        public SlugcatStats.Name currentCampaign;
        public bool requireCampaignSlugcat;
        public string? saveStateString;
        public bool lastWarpIsEcho = false;

        // TODO: split these out for other gamemodes to reuse (see Story/StoryMenuHelpers for methods)
        public Dictionary<string, bool> storyBoolRemixSettings;
        public Dictionary<string, float> storyFloatRemixSettings;
        public Dictionary<string, int> storyIntRemixSettings;

        public SlugcatCustomization avatarSettings;
        public StoryClientSettingsData storyClientData;

        public Watcher.WarpPoint.WarpPointData? myLastWarp = null; //yeah watcher gonna watch
        public string? myLastDenPos = null;
        public bool hasSheltered = false;

        public List<AbstractCreature> pups;
        public void Sanitize()
        {
            hasSheltered = false;
            isInGame = false;
            changedRegions = false;
            readyForWin = false;
            readyForTransition = ReadyForTransition.Closed;
            defaultDenPos = null;
            myLastWarp = null;
            myLastDenPos = null;
            lastWarpIsEcho = false;
            region = null;
            saveStateString = null;
            pups = new();
            storyClientData?.Sanitize();
        }

        public bool canJoinGame => isInGame && !changedRegions && readyForTransition == ReadyForTransition.Closed && !readyForWin;

        public bool saveToDisk = false;

        public StoryGameMode(Lobby lobby) : base(lobby)
        {
            avatarSettings = new SlugcatCustomization() { nickname = OnlineManager.mePlayer.id.name };
        }

        public override ProcessManager.ProcessID MenuProcessId()
        {
            return RainMeadow.Ext_ProcessID.StoryMenu;
        }

        public HashSet<PlacedObject.Type> disallowedPlacedObjects = new()
        {
            PlacedObject.Type.SporePlant,  // crashes the game, ask Turtle
            PlacedObject.Type.HangingPearls,  // duplicates and needs to be synced, ask choc
            DLCSharedEnums.PlacedObjectType.Stowaway, //cause severe visual glitches and shaking when overlapped
            Watcher.WatcherEnums.PlacedObjectType.CosmeticRipple, //visual glitches and does not really hurt to exclude
        };

        public override bool AllowedInMode(PlacedObject item)
        {
            if (disallowedPlacedObjects.Contains(item.type)) return false;
            return true;  // base.AllowedInMode(item) || playerGrabbableItems.Contains(item.type) || creatureRelatedItems.Contains(item.type);
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

        public HashSet<AbstractPhysicalObject.AbstractObjectType> unsyncedAbstractObjectTypes = new()
        {
            AbstractPhysicalObject.AbstractObjectType.VoidSpawn,
            AbstractPhysicalObject.AbstractObjectType.BlinkingFlower,
            AbstractPhysicalObject.AbstractObjectType.AttachedBee,
            Watcher.WatcherEnums.AbstractObjectType.RippleSpawn, //does not need to be kept track of
        };

        public override bool ShouldSyncAPOInWorld(WorldSession ws, AbstractPhysicalObject apo)
        {
            if (unsyncedAbstractObjectTypes.Contains(apo.type)) return false;
            return true;
        }

        public override bool ShouldSyncAPOInRoom(RoomSession rs, AbstractPhysicalObject apo)
        {
            if (unsyncedAbstractObjectTypes.Contains(apo.type)) return false;
            return true;
        }

        public override bool ShouldRegisterAPO(OnlineResource resource, AbstractPhysicalObject apo)
        {
            if (unsyncedAbstractObjectTypes.Contains(apo.type)) return false;
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

        private string? gateRoom;
        public override void LobbyTick(uint tick)
        {
            base.LobbyTick(tick);

            // could switch this based on rules? any vs all
            storyClientData.isDead = avatars.All(a => a.abstractCreature.state is PlayerState state && (state.dead || state.permaDead));

            if (lobby.isOwner && lobby.clientSettings.Values.Where(cs => cs.inGame) is var inGameClients && inGameClients.Any())
            {
                var inGameClientsData = inGameClients.Select(cs => cs.GetData<StoryClientSettingsData>());
                var inGameAvatarOPOs = inGameClients.SelectMany(cs => cs.avatars.Select(id => id.FindEntity(true))).OfType<OnlinePhysicalObject>();

                if (!readyForWin && inGameClientsData.Any(scs => scs.readyForWin) && inGameClientsData.All(scs => scs.readyForWin || scs.isDead))
                {
                    RainMeadow.Debug("ready for win!");
                    readyForWin = true;
                }

                if (readyForTransition == ReadyForTransition.MeetRequirement)
                {
                    gateRoom = null;
                    if (inGameClientsData.All(scs => scs.readyForTransition))
                    {
                        // make sure they're at the same region gate
                        var rooms = inGameAvatarOPOs.Select(opo => opo.apo.pos.room);
                        if (rooms.Distinct().Count() == 1)
                        {
                            RainWorld.roomIndexToName.TryGetValue(rooms.First(), out gateRoom);
                            RainMeadow.Debug($"ready for gate {gateRoom}!");
                            readyForTransition = ReadyForTransition.Opening;
                        }
                    }
                }
                else if (readyForTransition == ReadyForTransition.Crossed)
                {
                    // wait for all players to pass through OR leave the gate room
                    if (inGameClientsData.All(scs => !scs.readyForTransition)
                        || (gateRoom is not null && !inGameAvatarOPOs.Select(opo => opo.apo.Room?.name).Contains(gateRoom))  // HACK: AllPlayersThroughToOtherSide may not get called if warp, which softlocks gates
                        )
                    {
                        RainMeadow.Debug($"all through gate {gateRoom}!");
                        readyForTransition = ReadyForTransition.Closed;
                    }
                }
            }
        }

        public override void PlayerLeftLobby(OnlinePlayer player)
        {
            base.PlayerLeftLobby(player);
            // XXX: does not work as expected, new lobby owner is assigned before we receive this
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
            readyForWin = false;
            readyForTransition = ReadyForTransition.Closed;
            storyClientData.Sanitize();
        }

        public override void PostGameStart(RainWorldGame game)
        {
            base.PostGameStart(game);
        }

        public override void GameShutDown(RainWorldGame game)
        {
            base.GameShutDown(game);
        }
    }

    public static class StoryModeExtensions
    {
        public static bool FriendlyFireSafetyCandidate(this PhysicalObject creature)
        {
            if (creature is Player p)
            {
                if (p.isNPC) return false;
                if (RainMeadow.isArenaMode(out var _) && p.room.game.IsArenaSession && p.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.spearsHitPlayers == false) {
                    return true; // you are a safety candidate
                };

            }
            else return false;

            if (RainMeadow.isStoryMode(out var story))
            {
                return !story.friendlyFire;
            }
            if (RainMeadow.isArenaMode(out var arena))
            {
                return arena.countdownInitiatedHoldFire;
            }

            return false;
        }
    }
}
