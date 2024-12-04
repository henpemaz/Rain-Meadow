using System;
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
        public byte readyForGate = 0;
        public bool friendlyFire = false; // false until we manage it via UI
        public string? defaultDenPos;
        public string? region = null;
        public SlugcatStats.Name currentCampaign;
        public string? saveStateString;

        // TODO: split these out for other gamemodes to reuse (see Story/StoryMenuHelpers for methods)
        public Dictionary<string, bool> storyBoolRemixSettings;
        public Dictionary<string, float> storyFloatRemixSettings;
        public Dictionary<string, int> storyIntRemixSettings;

        public SlugcatCustomization avatarSettings;
        public StoryClientSettingsData storyClientData;

        public string? myLastDenPos = null;
        public string? swallowedItem = null;  // NOTE: does not spawn the item, simply identifies which to reclaim
        public bool hasSheltered = false;

        public void Sanitize()
        {
            hasSheltered = false;
            isInGame = false;
            changedRegions = false;
            readyForWin = false;
            readyForGate = 0;
            defaultDenPos = null;
            myLastDenPos = null;
            swallowedItem = null;
            region = null;
            saveStateString = null;
            storyClientData?.Sanitize();
        }

        public bool saveToDisk = false;

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

            if (lobby.isOwner && lobby.clientSettings.Values.Where(cs => cs.inGame) is var inGameClients && inGameClients.Any())
            {
                var inGameClientsData = inGameClients.Select(cs => cs.GetData<StoryClientSettingsData>());

                if (!readyForWin && inGameClientsData.Any(scs => scs.readyForWin) && inGameClientsData.All(scs => scs.readyForWin || scs.isDead))
                {
                    RainMeadow.Debug("ready for win!");
                    readyForWin = true;
                }

                if (readyForGate == 0)
                {
                    if (inGameClientsData.All(scs => scs.readyForGate))
                    {
                        // make sure they're at the same region gate
                        var rooms = inGameClients.SelectMany(cs => cs.avatars.Select(id => id.FindEntity(true)))
                            .OfType<OnlinePhysicalObject>().Select(opo => opo.apo.pos.room);
                        if (rooms.Distinct().Count() == 1)
                        {
                            RainMeadow.Debug($"ready for gate!");
                            readyForGate = 1;
                        }
                    }
                }
                else if (readyForGate > 0)
                {
                    // wait for all players to pass through
                    if (inGameClientsData.All(scs => !scs.readyForGate))
                    {
                        RainMeadow.Debug($"all through gate!");
                        readyForGate = 0;
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
            readyForGate = 0;
            storyClientData.Sanitize();
        }

        private void PlayerSwallow(Player player, AbstractPhysicalObject apo)
        {
            apo.realizedObject?.RemoveFromRoom();
            if (apo.GetOnlineObject(out var opo)) opo.realized = false;
            apo.Abstractize(player.abstractCreature.pos);
            apo.Room.RemoveEntity(apo);
            apo.InDen = true;
            apo.pos.WashNode();
            player.objectInStomach = apo;
        }

        public override void PostGameStart(RainWorldGame game)
        {
            base.PostGameStart(game);
            if (swallowedItem is not null && game.world.GetResource() is WorldSession ws && !ws.isOwner
                && game.FirstAnyPlayer?.realizedCreature is Player player && player.objectInStomach is null)
            {
                // compare object type and extradata
                var arr = swallowedItem.Split('<');
                foreach (var apo in game.FirstAlivePlayer.Room.entities.OfType<AbstractPhysicalObject>())
                {
                    var arr2 = apo.ToString().Split('<');
                    if (arr[1] == arr2[1] && arr.Skip(3).SequenceEqual(arr2.Skip(3)))
                    {
                        if (apo.GetOnlineObject(out var opo) && !opo.isMine)
                        {
                            try
                            {
                                opo.Request();
                                (opo.pendingRequest as RPCEvent).Then((result) =>
                                {
                                    if (result is GenericResult.Ok && player.objectInStomach is null)
                                    {
                                        PlayerSwallow(player, apo);
                                    }
                                });
                            }
                            catch (Exception e)
                            {
                                RainMeadow.Error($"couldn't claim swallowed item {opo}: {e}");
                            }
                        }
                        else
                        {
                            PlayerSwallow(player, apo);
                        }
                    }
                }
            }
        }

        public override void GameShutDown(RainWorldGame game)
        {
            base.GameShutDown(game);
        }
    }
}
