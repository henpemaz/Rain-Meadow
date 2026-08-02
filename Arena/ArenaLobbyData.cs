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
            public int countdownSafetyCatchTimer;

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

            [OnlineFieldHalf(group = "arenaSetup")]
            public float artiStunDistance;

            [OnlineField(group = "arenaSetup")]
            public string currentGameMode; // maybe not use string

            [OnlineField(group = "arenaSetup")]
            public bool arenaItemSteal;

            [OnlineField(group = "arenaSetup")]
            public bool allowJoiningMidRound;

            [OnlineField(group = "arenaSetup")]
            public bool voidMasterEnabled;
            [OnlineFieldHalf(group = "arenaSetup")]
            public float voidSpawnLethalityFactor;

            [OnlineField(group = "arenaSetup")]
            public int amoebaDuration;

            [OnlineField(group = "arenaSetup")]
            public bool fullInvisInRippleSpace;

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
            public bool enableMeadowCosmetics;

            [OnlineField(group = "arenaSetup")]
            public int artiExplosionCapacity;

            
            [OnlineFieldHalf(group = "arenaSetup")]
            public float artiParryDistance;
            
            [OnlineField(group = "arenaSetup")]
            public bool artiParryLeniency;

            // Group: arenaGameplay
            [OnlineField(group = "arenaGameplay")]
            public List<ushort> arenaSittingOnlineOrder;

            [OnlineField(group = "arenaGameplay")]
            public List<ushort> playersLateWaitingInLobby;

            [OnlineField(nullable = true, group = "arenaGameplay")]
            public Generics.DynamicOrderedPlayerIDs reigningChamps;

            [OnlineField(group = "arenaGameplay")]
            public int currentLevel;

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

            [OnlineField(group = "arenaGameplay")]
            public bool leaveForNextLevel;

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
                countdownSafetyCatchTimer = arena.countdownSafetyCatchTimer;
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
                artiStunDistance = arena.artiStunDistanceMult;
                arenaItemSteal = arena.itemSteal;
                allowJoiningMidRound = arena.allowJoiningMidRound;
                weaponCollisionFix = arena.weaponCollisionFix;
                enableBombs = arena.enableBombs;
                enableBees = arena.enableBees;
                enableCorpseGrab = arena.enableCorpseGrab;
                leaveForNextLevel = arena.leaveForNextLevel;
                hasPermissionToRejoin = arena.hasPermissionToRejoin;
                playersEqualToOnlineSitting = arena.playersEqualToOnlineSitting;
                piggyBack = arena.piggyBack;

                bannedSlugs = new(arena.bannedSlugs);
                voidMasterEnabled = arena.voidMasterEnabled;
                voidSpawnLethalityFactor = arena.voidSpawnLethalityFactor;
                amoebaDuration = arena.amoebaDuration;
                fullInvisInRippleSpace = arena.fullInvisInRippleSpace;
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
                artiParryDistance = arena.artiParryDistanceMult;
                artiParryLeniency = arena.artiParryLeniency;
                enableMeadowCosmetics = arena.enableMeadowCosmetics;

            }

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                if ((resource as Lobby)?.gameMode is not ArenaOnlineGameMode arenaOnline) { RainMeadow.Error("Ressource is not an ArenaOnlineGameMode !"); return;}
                arenaOnline.isInGame = isInGame;
                arenaOnline.playList = playList;
                arenaOnline.shufflePlayList = shufflePlayList;
                arenaOnline.arenaSittingOnlineOrder = arenaSittingOnlineOrder;
                arenaOnline.allPlayersReadyLockLobby = allPlayersReadyLockLobby;
                arenaOnline.returnToLobby = returnToLobby;
                arenaOnline.onlineArenaSettingsInterfaceMultiChoice = onlineArenaSettingsInterfaceMultiChoice;
                arenaOnline.onlineArenaSettingsInterfaceeBool = onlineArenaSettingsInterfaceBool;
                arenaOnline.playersInLobbyChoosingSlugs = playersChoosingSlugs;
                arenaOnline.playersReadiedUp = playersReadiedUp;
                arenaOnline.reigningChamps = reigningChamps;

                arenaOnline.playerNumberWithDeaths = playerNumberWithDeaths;
                arenaOnline.playerNumberWithWins = playerNumberWithWins;

                arenaOnline.playerNumberWithTrophies = playerNumberWithTrophies;
                arenaOnline.playerNumberWithTrophiesPerRound = playerNumberWithTrophiesPerRound;

                arenaOnline.playerTotScore = playerTotScore;
                arenaOnline.playerNumberWithScore = playerNumberWithScore;
                arenaOnline.playersLateWaitingInLobbyForNextRound = playersLateWaitingInLobby;
                
                arenaOnline.countdownSafetyCatchTimer = countdownSafetyCatchTimer;
                arenaOnline.countdownInitiatedHoldFire = countdownInitiatedHoldFire;
                arenaOnline.playerResultColors = playerResultColors;
                arenaOnline.setupTime = arenaSetupTime;
                arenaOnline.lobbyCountDown = lobbyCountDown;
                arenaOnline.initiateLobbyCountdown = initiatedLobbyCountDown;

                arenaOnline.sainot = sainot;
                arenaOnline.arenaSaintAscendanceTimer = saintAscendanceTimer;
                arenaOnline.watcherCamoTimer = watcherCamoLimit;
                arenaOnline.watcherRippleLevel = watcherRippleLevel;
                arenaOnline.currentGameMode = currentGameMode;
                arenaOnline.currentLevel = currentLevel;
                arenaOnline.totalLevelCount = totalLevels;
                arenaOnline.painCatEgg = painCatEgg;
                arenaOnline.painCatThrows = painCatThrows;
                arenaOnline.painCatLizard = painCatLizard;
                arenaOnline.artiStunDistanceMult = artiStunDistance;
                arenaOnline.disableMaul = disableMaul;
                arenaOnline.itemSteal = arenaItemSteal;
                arenaOnline.allowJoiningMidRound = allowJoiningMidRound;
                arenaOnline.weaponCollisionFix = weaponCollisionFix;

                arenaOnline.enableBees = enableBees;
                arenaOnline.enableBombs = enableBombs;
                arenaOnline.enableCorpseGrab = enableCorpseGrab;

                arenaOnline.leaveForNextLevel = leaveForNextLevel;
                arenaOnline.hasPermissionToRejoin = hasPermissionToRejoin;
                arenaOnline.playersEqualToOnlineSitting = playersEqualToOnlineSitting;

                arenaOnline.bannedSlugs = bannedSlugs;
                arenaOnline.piggyBack = piggyBack;
                arenaOnline.voidMasterEnabled = voidMasterEnabled;
                arenaOnline.voidSpawnLethalityFactor = voidSpawnLethalityFactor;

                arenaOnline.amoebaDuration = amoebaDuration;
                arenaOnline.amoebaControl = amoebaControl;
                arenaOnline.fullInvisInRippleSpace = fullInvisInRippleSpace;
                arenaOnline.friendlyFire = friendlyFire;
                arenaOnline.enableOverseer = enableOverseer;

                arenaOnline.foodScore = foodScore;

                arenaOnline.spearHitScore = spearHitScore;
                arenaOnline.killScore = killScore;
                arenaOnline.aliveScore = aliveScore;
                arenaOnline.denEntryRule = denRule;
                arenaOnline.denScore = denScore;
                arenaOnline.hostLoadedOverlay = hostLoadedOverlay;
                arenaOnline.emptyKillTagScore = emptyKillScore;
                arenaOnline.challengeDenEjection = challengeDenEjection;
                arenaOnline.spearHitScore = spearHitScore;
                arenaOnline.killScore = killScore;
                arenaOnline.aliveScore = aliveScore;
                arenaOnline.denEntryRule = denRule;
                arenaOnline.denScore = denScore;
                arenaOnline.hostLoadedOverlay = hostLoadedOverlay;
                arenaOnline.emptyKillTagScore = emptyKillScore;
                arenaOnline.challengeDenEjection = challengeDenEjection;

                arenaOnline.artiExplosionCount = artiExplosionCapacity;
                arenaOnline.artiParryDistanceMult = artiParryDistance;
                arenaOnline.artiParryLeniency = artiParryLeniency;
                arenaOnline.enableMeadowCosmetics = enableMeadowCosmetics;
            }

            public override Type GetDataType() => typeof(ArenaLobbyData);
        }
    }
}
