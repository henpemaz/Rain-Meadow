using System;
using System.Collections.Generic;
using System.Linq;
using RainMeadow.Generics;

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
            public Dictionary<int, int> winsByInLobbyId;

            [OnlineField(group = "arenaScore")]
            public Dictionary<int, int> deathsByInLobbyId;

            [OnlineField(group = "arenaScore")]
            public Dictionary<int, int> totalScoreByInLobbyId;

            [OnlineField(group = "arenaScore")]
            public Dictionary<int, int> scoreByInLobbyId;

            [OnlineField(group = "arenaScore")]
            public Dictionary<int, List<string>> allKillsByInLobbyId;

            [OnlineField(group = "arenaScore")]
            public Dictionary<int, List<string>> roundKillsByInLobbyId;


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
                Lobby lobby = (Lobby)onlineResource;
                ArenaOnlineGameMode arenaOnline = (ArenaOnlineGameMode)lobby.gameMode;

                isInGame = RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame;
                playList = arenaOnline.playList.ToList();
                shufflePlayList = arenaOnline.shufflePlayList;
                arenaSittingOnlineOrder = arenaOnline.arenaSittingOnlineOrder.ToList();
                allPlayersReadyLockLobby = arenaOnline.allPlayersReadyLockLobby;
                returnToLobby = arenaOnline.returnToLobby;
                onlineArenaSettingsInterfaceMultiChoice = arenaOnline.onlineArenaSettingsInterfaceMultiChoice.ToDictionary();
                onlineArenaSettingsInterfaceBool = arenaOnline.onlineArenaSettingsInterfaceeBool.ToDictionary();
                playersReadiedUp = new DynamicOrderedPlayerIDs(arenaOnline.playersReadiedUp.list.ToList());
                reigningChamps = new DynamicOrderedPlayerIDs(arenaOnline.reigningChamps.list.ToList());

                playersLateWaitingInLobby = arenaOnline.playersLateWaitingInLobbyForNextRound.ToList();

                winsByInLobbyId       = arenaOnline.winsByInLobbyId.ToDictionary();
                deathsByInLobbyId     = arenaOnline.deathsByInLobbyId.ToDictionary();
                totalScoreByInLobbyId = arenaOnline.totalScoreByInLobbyId.ToDictionary();
                scoreByInLobbyId      = arenaOnline.scoreByInLobbyId.ToDictionary();
                allKillsByInLobbyId   = arenaOnline.allKillsByInLobbyId.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                        .Select(trophy => trophy.ToString())
                        .ToList()
                );
                roundKillsByInLobbyId = arenaOnline.roundKillsByInLobbyId.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                        .Select(trophy => trophy.ToString())
                        .ToList()
                );

                playersChoosingSlugs = arenaOnline.playersInLobbyChoosingSlugs.ToDictionary();
                countdownInitiatedHoldFire = arenaOnline.countdownInitiatedHoldFire;
                playerResultColors = arenaOnline.playerResultColors;
                arenaSetupTime = arenaOnline.setupTime;
                lobbyCountDown = arenaOnline.lobbyCountDown;
                initiatedLobbyCountDown = arenaOnline.initiateLobbyCountdown;
                sainot = arenaOnline.sainot;
                saintAscendanceTimer = arenaOnline.arenaSaintAscendanceTimer;
                watcherCamoLimit = arenaOnline.watcherCamoTimer;
                watcherRippleLevel = arenaOnline.watcherRippleLevel;
                currentGameMode = arenaOnline.currentGameMode;
                currentLevel = arenaOnline.currentLevel;
                totalLevels = arenaOnline.totalLevelCount;
                painCatEgg = arenaOnline.painCatEgg;
                painCatThrows = arenaOnline.painCatThrows;
                painCatLizard = arenaOnline.painCatLizard;
                disableMaul = arenaOnline.disableMaul;
                artiStunDistance = arenaOnline.artiStunDistanceMult;
                arenaItemSteal = arenaOnline.itemSteal;
                allowJoiningMidRound = arenaOnline.allowJoiningMidRound;
                weaponCollisionFix = arenaOnline.weaponCollisionFix;
                enableBombs = arenaOnline.enableBombs;
                enableBees = arenaOnline.enableBees;
                enableCorpseGrab = arenaOnline.enableCorpseGrab;
                leaveForNextLevel = arenaOnline.leaveForNextLevel;
                hasPermissionToRejoin = arenaOnline.hasPermissionToRejoin;
                playersEqualToOnlineSitting = arenaOnline.playersEqualToOnlineSitting;
                piggyBack = arenaOnline.piggyBack;

                bannedSlugs = arenaOnline.bannedSlugs.ToList();
                voidMasterEnabled = arenaOnline.voidMasterEnabled;
                voidSpawnLethalityFactor = arenaOnline.voidSpawnLethalityFactor;
                amoebaDuration = arenaOnline.amoebaDuration;
                amoebaControl = arenaOnline.amoebaControl;
                friendlyFire = arenaOnline.friendlyFire;
                enableOverseer = arenaOnline.enableOverseer;

                foodScore = arenaOnline.foodScore;

                spearHitScore = arenaOnline.spearHitScore;
                killScore = arenaOnline.killScore;
                aliveScore = arenaOnline.aliveScore;
                denRule = arenaOnline.denEntryRule;
                denScore = arenaOnline.denScore;
                hostLoadedOverlay = arenaOnline.hostLoadedOverlay;
                emptyKillScore = arenaOnline.emptyKillTagScore;
                challengeDenEjection = arenaOnline.challengeDenEjection;

                artiExplosionCapacity = arenaOnline.artiExplosionCount;
                artiParryDistance = arenaOnline.artiParryDistanceMult;
                artiParryLeniency = arenaOnline.artiParryLeniency;
                enableMeadowCosmetics = arenaOnline.enableMeadowCosmetics;

            }

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource onlineResource)
            {
                Lobby lobby = (Lobby)onlineResource;
                ArenaOnlineGameMode arenaOnline = (ArenaOnlineGameMode)lobby.gameMode;

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

                arenaOnline.playersLateWaitingInLobbyForNextRound = playersLateWaitingInLobby;

                arenaOnline.winsByInLobbyId       = winsByInLobbyId;
                arenaOnline.deathsByInLobbyId     = deathsByInLobbyId;
                arenaOnline.totalScoreByInLobbyId = totalScoreByInLobbyId;
                arenaOnline.scoreByInLobbyId      = scoreByInLobbyId;
                arenaOnline.allKillsByInLobbyId   = allKillsByInLobbyId.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                        .Select(IconSymbol.IconSymbolData.IconSymbolDataFromString)
                        .ToList()
                );
                arenaOnline.roundKillsByInLobbyId = roundKillsByInLobbyId.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                        .Select(IconSymbol.IconSymbolData.IconSymbolDataFromString)
                        .ToList()
                );

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


                arenaOnline.artiExplosionCount = artiExplosionCapacity;
                arenaOnline.artiParryDistanceMult = artiParryDistance;
                arenaOnline.artiParryLeniency = artiParryLeniency;
                arenaOnline.enableMeadowCosmetics = enableMeadowCosmetics;

            }

            public override Type GetDataType() => typeof(ArenaLobbyData);
        }
    }
}
