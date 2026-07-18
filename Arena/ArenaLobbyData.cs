using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    internal class ArenaLobbyData : OnlineResource.ResourceData
    {
        public ArenaLobbyData() { }

        public override ResourceDataState MakeState(OnlineResource resource)
        {
            return new State(this, resource);
        }

        internal class State : ResourceDataState
        {
            // Group: arenaLobby
            [OnlineField(group = "arenaLobby")]
            public bool isInGame;

            [OnlineField(group = "arenaLobby")]
            public bool hasPermissionToRejoin;

            [OnlineField(group = "arenaLobby")]
            public bool allPlayersReadyLockLobby;

            [OnlineField(group = "arenaLobby")]
            public bool returnToLobby;

            [OnlineField(group = "arenaLobby")]
            public Dictionary<string, int> onlineArenaSettingsInterfaceMultiChoice;

            [OnlineField(group = "arenaLobby")]
            public Dictionary<string, bool> onlineArenaSettingsInterfaceBool;

            [OnlineField(group = "arenaLobby")]
            public Dictionary<string, int> playersChoosingSlugs;

            [OnlineField(group = "arenaLobby")]
            public Dictionary<string, int> playerResultColors;

            [OnlineField(nullable = true, group = "arenaLobby")]
            public Generics.DynamicOrderedPlayerIDs playersReadiedUp;

            [OnlineField(group = "arenaLobby")]
            public List<int> bannedSlugs;

            // Group: arenaSetup
            [OnlineField(group = "arenaSetup")]
            public List<string> playList;

            [OnlineField(group = "arenaSetup")]
            public bool shufflePlayList;

            [OnlineField(group = "arenaSetup")]
            public int totalLevels;

            [OnlineField(group = "arenaSetup")]
            public int arenaSetupTime;

            [OnlineField(group = "arenaSetup")]
            public int lobbyCountDown;

            [OnlineField(group = "arenaSetup")]
            public bool initiatedLobbyCountDown;

            [OnlineField(group = "arenaSetup")]
            public int saintAscendanceTimer;

            [OnlineField(group = "arenaSetup")]
            public int watcherCamoLimit;

            [OnlineField(group = "arenaSetup")]
            public int watcherRippleLevel;

            [OnlineField(group = "arenaSetup")]
            public bool sainot;

            [OnlineField(group = "arenaSetup")]
            public bool painCatEgg;

            [OnlineField(group = "arenaSetup")]
            public bool painCatThrows;

            [OnlineField(group = "arenaSetup")]
            public bool painCatLizard;

            [OnlineField(group = "arenaSetup")]
            public bool disableMaul;

            [OnlineField(group = "arenaSetup")]
            public bool disableArtiStun;

            [OnlineField(group = "arenaSetup")]
            public string currentGameMode; // maybe not use string

            [OnlineField(group = "arenaSetup")]
            public bool arenaItemSteal;

            [OnlineField(group = "arenaSetup")]
            public bool allowJoiningMidRound;

            [OnlineField(group = "arenaSetup")]
            public bool voidMasterEnabled;

            [OnlineField(group = "arenaSetup")]
            public int amoebaDuration;

            [OnlineField(group = "aremaSetup")]
            public bool amoebaControl;

            [OnlineField(group = "arenaSetup")]
            public bool weaponCollisionFix;

            [OnlineField(group = "arenaSetup")]
            public bool enableBombs;

            [OnlineField(group = "arenaSetup")]
            public bool enableBees;

            [OnlineField(group = "arenaSetup")]
            public bool enableCorpseGrab;

            [OnlineField(group = "arenaSetup")]
            public bool piggyBack;

            [OnlineField(group = "arenaSetup")]
            public bool friendlyFire;

            [OnlineField(group = "arenaSetup")]
            public bool enableOverseer;

            [OnlineField(group = "arenaSetup")]
            public int foodScore;

            [OnlineField(group = "arenaSetup")]
            public int spearHitScore;

            [OnlineField(group = "arenaSetup")]
            public int killScore;

            [OnlineField(group = "arenaSetup")]
            public int aliveScore;
            [OnlineField(group = "arenaSetup")]
            public int denScore;

            [OnlineField(group = "arenaSetup", nullable = true)]
            public ArenaSetup.GameTypeSetup.DenEntryRule denRule;


            [OnlineField(group = "arenaSetup")]
            public int emptyKillScore;


            [OnlineField(group = "arenaSetup")]
            public bool challengeDenEjection;

            [OnlineField(group = "arenaSetup")]
            public int artiExplosionCapacity;

            // Group: arenaGameplay
            [OnlineField(group = "arenaGameplay")]
            public List<ushort> arenaSittingOnlineOrder;

            [OnlineField(group = "arenaGameplay")]
            public List<ushort> playersLateWaitingInLobby;

            [OnlineField(nullable = true, group = "arenaGameplay")]
            public Generics.DynamicOrderedPlayerIDs reigningChamps;

            [OnlineField(group = "arenaGameplay")]
            public int currentLevel;
            [OnlineField(group = "arenaGameplay")]
            public int leaveToRestart;


            [OnlineField(group = "arenaScore")]
            public Dictionary<int, int> playerNumberWithDeaths;

            [OnlineField(group = "arenaScore")]
            public Dictionary<int, int> playerNumberWithWins;

            [OnlineField(group = "arenaScore")]
            public Dictionary<int, int> playerTotScore;

            [OnlineField(group = "arenaScore")]
            public Dictionary<int, List<string>> playerNumberWithTrophies;
            [OnlineField(group = "arenaScore")]
            public Dictionary<int, int> playerNumberWithScore;


            [OnlineField(group = "arenaScore")]
            public Dictionary<int, List<string>> playerNumberWithTrophiesPerRound;

            [OnlineField(group = "arenaGameplay")]
            public bool countdownInitiatedHoldFire;

            [OnlineField]
            public int playerEnteredGame;

            [OnlineField]
            public bool playersEqualToOnlineSitting;


            [OnlineField]
            public bool hostLoadedOverlay;
            public State() { }

            public State(ArenaLobbyData arenaLobbyData, OnlineResource onlineResource)
            {
                ArenaOnlineGameMode arena =
                    (onlineResource as Lobby).gameMode as ArenaOnlineGameMode;
                isInGame =
                    RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame;
                playList = new(arena.playList);
                shufflePlayList = arena.shufflePlayList;
                arenaSittingOnlineOrder = new(arena.arenaSittingOnlineOrder);
                allPlayersReadyLockLobby = arena.allPlayersReadyLockLobby;
                returnToLobby = arena.returnToLobby;
                onlineArenaSettingsInterfaceMultiChoice =
                    new(arena.onlineArenaSettingsInterfaceMultiChoice);
                onlineArenaSettingsInterfaceBool = new(arena.onlineArenaSettingsInterfaceeBool);
                playersReadiedUp = new(arena.playersReadiedUp.list.ToList());
                reigningChamps = new(arena.reigningChamps.list.ToList());
                playerNumberWithDeaths = new(arena.playerNumberWithDeaths);
                playerTotScore = new(arena.playerTotScore);
                playerNumberWithWins = new(arena.playerNumberWithWins);
                playerNumberWithTrophies = new(arena.playerNumberWithTrophies);
                playerNumberWithTrophiesPerRound = new(arena.playerNumberWithTrophiesPerRound);
                playerNumberWithScore = new(arena.playerNumberWithScore);

                playersLateWaitingInLobby = new(arena.playersLateWaitingInLobbyForNextRound);

                playersChoosingSlugs = new(
                    arena.playersInLobbyChoosingSlugs.ToDictionary<string, int>()
                );
                countdownInitiatedHoldFire = arena.countdownInitiatedHoldFire;
                playerResultColors = arena.playerResultColors;
                arenaSetupTime = arena.setupTime;
                lobbyCountDown = arena.lobbyCountDown;
                initiatedLobbyCountDown = arena.initiateLobbyCountdown;
                sainot = arena.sainot;
                saintAscendanceTimer = arena.arenaSaintAscendanceTimer;
                watcherCamoLimit = arena.watcherCamoTimer;
                watcherRippleLevel = arena.watcherRippleLevel;
                currentGameMode = arena.currentGameMode;
                currentLevel = arena.currentLevel;
                totalLevels = arena.totalLevelCount;
                painCatEgg = arena.painCatEgg;
                painCatThrows = arena.painCatThrows;
                painCatLizard = arena.painCatLizard;
                disableMaul = arena.disableMaul;
                disableArtiStun = arena.disableArtiStun;
                arenaItemSteal = arena.itemSteal;
                allowJoiningMidRound = arena.allowJoiningMidRound;
                weaponCollisionFix = arena.weaponCollisionFix;
                enableBombs = arena.enableBombs;
                enableBees = arena.enableBees;
                enableCorpseGrab = arena.enableCorpseGrab;
                hasPermissionToRejoin = arena.hasPermissionToRejoin;
                playersEqualToOnlineSitting = arena.playersEqualToOnlineSitting;
                piggyBack = arena.piggyBack;

                bannedSlugs = new(arena.bannedSlugs);
                voidMasterEnabled = arena.voidMasterEnabled;
                amoebaDuration = arena.amoebaDuration;
                amoebaControl = arena.amoebaControl;
                friendlyFire = arena.friendlyFire;
                enableOverseer = arena.enableOverseer;

                foodScore = arena.foodScore;

                spearHitScore = arena.spearHitScore;
                killScore = arena.killScore;
                aliveScore = arena.aliveScore;
                denRule = arena.denEntryRule;
                denScore = arena.denScore;
                hostLoadedOverlay = arena.hostLoadedOverlay;
                emptyKillScore = arena.emptyKillTagScore;
                challengeDenEjection = arena.challengeDenEjection;

                artiExplosionCapacity = arena.artiExplosionCount;

            }

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                if (resource is Lobby lobby && lobby.gameMode is ArenaOnlineGameMode gameMode)
                {
                    gameMode.isInGame = isInGame;
                    gameMode.playList = playList;
                    gameMode.shufflePlayList = shufflePlayList;
                    gameMode.arenaSittingOnlineOrder =
                        arenaSittingOnlineOrder;
                    gameMode.allPlayersReadyLockLobby =
                        allPlayersReadyLockLobby;
                    gameMode.returnToLobby = returnToLobby;
                    gameMode.onlineArenaSettingsInterfaceMultiChoice =
                        onlineArenaSettingsInterfaceMultiChoice;
                    gameMode.onlineArenaSettingsInterfaceeBool =
                        onlineArenaSettingsInterfaceBool;
                    gameMode.playersInLobbyChoosingSlugs =
                        playersChoosingSlugs;
                    gameMode.playersReadiedUp = playersReadiedUp;
                    gameMode.reigningChamps = reigningChamps;

                    gameMode.playerNumberWithDeaths =
                        playerNumberWithDeaths;
                    gameMode.playerNumberWithWins = playerNumberWithWins;

                    gameMode.playerNumberWithTrophies =
                        playerNumberWithTrophies;
                    gameMode.playerNumberWithTrophiesPerRound =
    playerNumberWithTrophiesPerRound;

                    gameMode.playerTotScore = playerTotScore;
                    gameMode.playerNumberWithScore =
                        playerNumberWithScore;
                    gameMode.playersLateWaitingInLobbyForNextRound =
                        playersLateWaitingInLobby;

                    gameMode.countdownInitiatedHoldFire =
                        countdownInitiatedHoldFire;
                    gameMode.playerResultColors = playerResultColors;
                    gameMode.setupTime = arenaSetupTime;
                    gameMode.lobbyCountDown = lobbyCountDown;
                    gameMode.initiateLobbyCountdown =
                        initiatedLobbyCountDown;

                    gameMode.sainot = sainot;
                    gameMode.arenaSaintAscendanceTimer =
                        saintAscendanceTimer;
                    gameMode.watcherCamoTimer = watcherCamoLimit;
                    gameMode.watcherRippleLevel = watcherRippleLevel;
                    gameMode.currentGameMode = currentGameMode;

                    if (gameMode.currentLevel != currentLevel)
                    {
                        RainMeadow.Debug($"1: {gameMode.currentLevel}");
                        gameMode.currentLevel = currentLevel;
                        var manager = RWCustom.Custom.rainWorld.processManager;
                        try
                        {
                            if (manager.arenaSitting != null) 
                            {
                                manager.arenaSitting.currentLevel = currentLevel;
                                manager.arenaSitting.NextLevel(manager);
                            }
                        }
                        catch (Exception except)
                        {
                            RainMeadow.Error(except);
                        }
                        RainMeadow.Debug($"1: {gameMode.currentLevel}");
                    }

                    if (gameMode.leaveToRestart != leaveToRestart)
                    {
                        gameMode.leaveToRestart = leaveToRestart;
                        gameMode.RestartGame();
                    }

                    gameMode.totalLevelCount = totalLevels;
                    gameMode.painCatEgg = painCatEgg;
                    gameMode.painCatThrows = painCatThrows;
                    gameMode.painCatLizard = painCatLizard;
                    gameMode.disableArtiStun = disableArtiStun;
                    gameMode.disableMaul = disableMaul;
                    gameMode.itemSteal = arenaItemSteal;
                    gameMode.allowJoiningMidRound = allowJoiningMidRound;
                    gameMode.weaponCollisionFix = weaponCollisionFix;

                    gameMode.enableBees = enableBees;
                    gameMode.enableBombs = enableBombs;
                    gameMode.enableCorpseGrab = enableCorpseGrab;
                    gameMode.hasPermissionToRejoin =
                        hasPermissionToRejoin;
                    gameMode.playersEqualToOnlineSitting =
                        playersEqualToOnlineSitting;

                    gameMode.bannedSlugs = bannedSlugs;
                    gameMode.piggyBack = piggyBack;
                    gameMode.voidMasterEnabled = voidMasterEnabled;
                    gameMode.amoebaDuration = amoebaDuration;
                    gameMode.amoebaControl = amoebaControl;
                    gameMode.friendlyFire = friendlyFire;
                    gameMode.enableOverseer = enableOverseer;

                    gameMode.foodScore = foodScore;
                    gameMode.spearHitScore = spearHitScore;
                    gameMode.killScore = killScore;
                    gameMode.aliveScore = aliveScore;
                    gameMode.denEntryRule = denRule;
                    gameMode.denScore = denScore;
                    gameMode.hostLoadedOverlay = hostLoadedOverlay;
                    gameMode.emptyKillTagScore = emptyKillScore;
                    gameMode.challengeDenEjection = challengeDenEjection;
                    gameMode.artiExplosionCount = artiExplosionCapacity;
                }
            }

            public override Type GetDataType() => typeof(ArenaLobbyData);
        }
    }
}
