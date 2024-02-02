using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RainMeadow
{
    // Lobby is tightly bound to a SteamMatchmaking Lobby, also the top-level resource
    public class Lobby : OnlineResource
    {
        public OnlineGameMode gameMode;
        public OnlineGameMode.OnlineGameModeType gameModeType;
        public Dictionary<string, WorldSession> worldSessions = new();

        public Dictionary<ushort, OnlineCreature> playerAvatars = new(); //key:lobbyID | Value:slugcat AbstractCreature

        public string[] mods = ModManager.ActiveMods.Where(mod => Directory.Exists(Path.Combine(mod.path, "modify", "world"))).ToList().ConvertAll(mod => mod.id.ToString()).ToArray();
        public static bool checkingMods;

        public override World World => throw new NotSupportedException(); // Lobby can't add world entities

        public event Action OnLobbyAvailable; // for menus

        public Lobby(OnlineGameMode.OnlineGameModeType mode, OnlinePlayer owner)
        {
            this.super = this;
            OnlineManager.lobby = this; // needed for early entity processing

            this.gameMode = OnlineGameMode.FromType(mode, this);
            this.gameModeType = mode;
            if (gameMode == null) throw new Exception($"Invalid game mode {mode}");

            if (owner == null) throw new Exception("No lobby owner");
            NewOwner(owner);

            activateOnAvailable = true;
            if (isOwner)
            {
                Available();
            }
            else
            {
                Request(); // Everyone auto-subscribes this resource
            }
        }

        protected override void ActivateImpl()
        {
            if (gameModeType == OnlineGameMode.OnlineGameModeType.ArenaCompetitive) // Arena
            {
                var nr = new Region("arena", 0, -1, null);
                var ns = new WorldSession(nr, this);
                worldSessions.Add(nr.name, ns);
                subresources.Add(ns);
            }
            else // story mode
            {
                foreach (var r in Region.LoadAllRegions(RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer))
                {
                    RainMeadow.Debug(r.name);
                    var ws = new WorldSession(r, this);
                    worldSessions.Add(r.name, ws);
                    subresources.Add(ws);
                }
                RainMeadow.Debug(subresources.Count);
            }
        }

        protected override void AvailableImpl()
        {
            OnLobbyAvailable?.Invoke();
            OnLobbyAvailable = null;
        }

        protected override void DeactivateImpl()
        {
            throw new InvalidOperationException("cant deactivate");
        }

        public override void OnPlayerDisconnect(OnlinePlayer player)
        {
            base.OnPlayerDisconnect(player);

            // todo move this to gamemode api
            if(isOwner) playerAvatars.Remove(player.inLobbyId);
            if(player == owner && gameMode is StoryGameMode)
            {
                OnlineManager.instance.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
            }
        }

        protected override ResourceState MakeState(uint ts)
        {
            return new LobbyState(this, ts);
        }

        public override string Id()
        {
            return ".";
        }

        public override ushort ShortId()
        {
            throw new NotImplementedException(); // Lobby cannot be a subresource
        }

        public override OnlineResource SubresourceFromShortId(ushort shortId)
        {
            return this.subresources[shortId];
        }

        protected ushort nextId = 1;

        public class LobbyState : ResourceWithSubresourcesState
        {
            [OnlineField]
            public ushort nextId;
            [OnlineField(nullable = true)]
            public Generics.AddRemoveSortedPlayerIDs players;
            [OnlineField(nullable = true)]
            public Generics.AddRemoveSortedEntityIDs avatars;
            [OnlineField(nullable = true)]
            public Generics.AddRemoveSortedUshorts winReadyPlayers;

            [OnlineField(nullable = true)]
            public Generics.AddRemoveSortedUshorts inLobbyIds;
            [OnlineField]
            public bool readyForNextCycle;
            [OnlineField]
            public bool didStartGame;

            [OnlineField]
            public int food;
            [OnlineField]
            public int quarterfood;
            [OnlineField]
            public string[] mods;
            [OnlineField(nullable = true)]
            public string? playerProgressSaveState;
            public LobbyState() : base() { }
            public LobbyState(Lobby lobby, uint ts) : base(lobby, ts)
            {
                nextId = lobby.nextId;
                players = new(lobby.participants.Keys.Select(p => p.id).ToList());
                inLobbyIds = new(lobby.participants.Keys.Select(p => p.inLobbyId).ToList());
                avatars = new(lobby.participants.Keys.Select(p => lobby.playerAvatars.TryGetValue(p.inLobbyId, out var c) ? c.id : new OnlineEntity.EntityId(p.inLobbyId, OnlineEntity.EntityId.IdType.none, 0)).ToList());
                mods = lobby.mods;
                if (lobby.gameModeType == OnlineGameMode.OnlineGameModeType.Story)
                {
                    winReadyPlayers = new((lobby.gameMode as StoryGameMode).readyForWinPlayers.ToList());
                    playerProgressSaveState = (lobby.gameMode as StoryGameMode)?.saveStateProgressString;
                    food = ((RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.Players[0].state as PlayerState)?.foodInStomach ?? 0;
                    quarterfood = ((RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.Players[0].state as PlayerState)?.quarterFoodPoints ?? 0;
                }
            }

            public override void ReadTo(OnlineResource resource)
            {
                var lobby = (Lobby)resource;
                lobby.nextId = nextId;
                lobby.isReadyForNextCycle = readyForNextCycle;
                lobby.didStartGame = didStartGame;

                for (int i = 0; i < players.list.Count; i++)
                {
                    if (MatchmakingManager.instance.GetPlayer(players.list[i]) is OnlinePlayer p)
                    {
                        if (p.inLobbyId != inLobbyIds.list[i]) RainMeadow.Debug($"Setting player {p} to lobbyId {inLobbyIds.list[i]}");
                        p.inLobbyId = inLobbyIds.list[i];
                    }
                    else
                    {
                        RainMeadow.Error("Player not found! " + players.list[i]);
                    }
                }
                lobby.playerAvatars.Clear();
                for (int i = 0; i < inLobbyIds.list.Count; i++)
                {
                    lobby.playerAvatars[inLobbyIds.list[i]] = avatars.list[i].FindEntity(quiet: true) as OnlineCreature;
                }
                lobby.UpdateParticipants(players.list.Select(MatchmakingManager.instance.GetPlayer).Where(p => p != null).ToList());
                if (lobby.gameModeType == OnlineGameMode.OnlineGameModeType.Story)
                {
                    var playerstate = ((RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.Players[0].state as PlayerState);
                    if (playerstate != null)
                    {
                        playerstate.foodInStomach = food;
                        playerstate.quarterFoodPoints = quarterfood;
                    }
                    (lobby.gameMode as StoryGameMode).saveStateProgressString = playerProgressSaveState;
                    (lobby.gameMode as StoryGameMode).readyForWinPlayers = winReadyPlayers.list;
                }
                lobby.readyForWinPlayers = winReadyPlayers.list;


                Menu.Menu? menu = RWCustom.Custom.rainWorld.processManager.currentMainLoop as Menu.Menu;

                if (!checkingMods && (menu is MeadowMenu || menu is LobbyMenu || menu is ArenaLobbyMenu))
                {
                    checkingMods = true;
                    if (Enumerable.SequenceEqual(lobby.mods, this.mods))
                    {
                        RainMeadow.Debug("Same mod set !");
                    }
                    else
                    {
                        RainMeadow.Debug("Mismatching mod set");

                        var (MissingMods, ExcessiveMods) = CompareModSets(this.mods, lobby.mods);

                        bool[] mods = ModManager.InstalledMods.ConvertAll(mod => mod.enabled).ToArray();

                        List<int> loadOrder = ModManager.InstalledMods.ConvertAll(mod => mod.loadOrder);

                        List<string> unknownMods = new();
                        List<ModManager.Mod> modsToEnable = new();
                        List<ModManager.Mod> modsToDisable = new();

                        foreach (var id in MissingMods)
                        {
                            int index = ModManager.InstalledMods.FindIndex(_mod => _mod.id == id);

                            if (index >= 0)
                            {
                                mods[index] = true;
                                modsToEnable.Add(ModManager.InstalledMods[index]);
                            }
                            else
                            {
                                RainMeadow.Debug("Unknown mod: " + id);
                                unknownMods.Add(id);
                            }
                        }

                        foreach (var id in ExcessiveMods)
                        {
                            int index = ModManager.InstalledMods.FindIndex(_mod => _mod.id == id);

                            mods[index] = false;

                            modsToDisable.Add(ModManager.InstalledMods[index]);
                        }

                        ModApplyer modApplyer = new(RWCustom.Custom.rainWorld.processManager, mods.ToList(), loadOrder);

                        modApplyer.ShowConfirmation(modsToEnable, modsToDisable, unknownMods);

                        modApplyer.OnFinish += (ModApplyer modApplyer) =>
                        {
                            Utils.Restart($"+connect_lobby {MatchmakingManager.instance.GetLobbyID()}");
                        };
                    }

                }

                base.ReadTo(resource);
            }
        }

        private static (List<string> MissingMods, List<string> ExcessiveMods) CompareModSets(string[] arr1, string[] arr2)
        {
            // Find missing strings in arr2
            var missingStrings = arr1.Except(arr2).ToList();

            // Find excessive strings in arr2
            var excessiveStrings = arr2
                .GroupBy(item => item)
                .Where(group => group.Count() > arr1.Count(item => item == group.Key))
                .Select(group => group.Key)
                .ToList();

            return (missingStrings, excessiveStrings);
        }

        public override string ToString()
        {
            return "Lobby";
        }

        public OnlinePlayer PlayerFromId(ushort id)
        {
            if (id == 0) return null;
            return OnlineManager.players.FirstOrDefault(p => p.inLobbyId == id);
        }

        protected override void NewParticipantImpl(OnlinePlayer player)
        {
            base.NewParticipantImpl(player);
            if (isOwner)
            {
                player.inLobbyId = nextId;
                RainMeadow.Debug($"Assigned inLobbyId of {nextId} to player {player}");
                nextId++;
                // todo overflows and repeats (unrealistic but it's a ushort)
            }
        }
    }
}
