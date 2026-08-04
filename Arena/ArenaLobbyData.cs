using System;
using System.Collections.Generic;
using System.Linq;
using RainMeadow.Generics;
using RWCustom;

namespace RainMeadow
{
    internal class ArenaLobbyData : OnlineResource.ResourceData
    {
        public override ResourceDataState MakeState(OnlineResource resource)
        {
            return new State(this, resource);
        }

        internal class State : ResourceDataState
        {
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
            public DynamicOrderedPlayerIDs playersReadiedUp;

            [OnlineField(group = "arenaLobby")]
            public List<int> bannedSlugs;


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
            public int survivalScore;

            [OnlineField(group = "arenaSetup")]
            public int denScore;

            [OnlineField(group = "arenaSetup", nullable = true)]
            public ArenaSetup.GameTypeSetup.DenEntryRule denRule;

            [OnlineField(group = "arenaSetup")]
            public int emptyDeathScore;

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


            [OnlineField(group = "arenaGameplay")]
            public List<ushort> arenaSittingOnlineOrder;

            [OnlineField(group = "arenaGameplay")]
            public List<ushort> playersLateWaitingInLobby;

            [OnlineField(nullable = true, group = "arenaGameplay")]
            public Generics.DynamicOrderedPlayerIDs reigningChamps;

            [OnlineField(group = "arenaGameplay")]
            public int currentLevel;

            [OnlineField(group = "arenaGameplay")]
            public bool countdownInitiatedHoldFire;

            [OnlineField(group = "arenaGameplay")]
            public bool leaveForNextLevel;


            [OnlineField(group = "arenaScore")]
            public Dictionary<int, int> winsByInLobbyId;

            [OnlineField(group = "arenaScore")]
            public Dictionary<int, int> deathsByInLobbyId;

            [OnlineField(group = "arenaScore")]
            public Dictionary<int, int> roundDeathsByInLobbyId;

            [OnlineField(group = "arenaScore")]
            public Dictionary<int, int> totalScoreByInLobbyId;

            [OnlineField(group = "arenaScore")]
            public Dictionary<int, int> scoreByInLobbyId;

            [OnlineField(group = "arenaScore")]
            public Dictionary<int, List<string>> allKillsByInLobbyId;

            [OnlineField(group = "arenaScore")]
            public Dictionary<int, List<string>> roundKillsByInLobbyId;


            [OnlineField]
            public int playerEnteredGame;

            [OnlineField]
            public bool playersEqualToOnlineSitting;

            [OnlineField]
            public bool hostLoadedOverlay;


            public State() { }

            public State(ArenaLobbyData lobbyData, OnlineResource onlineResource)
            {
                Lobby lobby = (Lobby)onlineResource;
                ArenaOnlineGameMode arenaOnline = (ArenaOnlineGameMode)lobby.gameMode;

                isInGame = Custom.rainWorld.processManager.currentMainLoop is RainWorldGame;
                playList = new List<string>(arenaOnline.playList);
                shufflePlayList = arenaOnline.shufflePlayList;
                arenaSittingOnlineOrder = new List<ushort>(arenaOnline.arenaSittingOnlineOrder);
                allPlayersReadyLockLobby = arenaOnline.allPlayersReadyLockLobby;
                returnToLobby = arenaOnline.returnToLobby;
                onlineArenaSettingsInterfaceMultiChoice = new Dictionary<string, int>(arenaOnline.onlineArenaSettingsInterfaceMultiChoice);
                onlineArenaSettingsInterfaceBool = new Dictionary<string, bool>(arenaOnline.onlineArenaSettingsInterfaceeBool);
                playersReadiedUp = new DynamicOrderedPlayerIDs(arenaOnline.playersReadiedUp.list.ToList());
                reigningChamps = new DynamicOrderedPlayerIDs(arenaOnline.reigningChamps.list.ToList());

                winsByInLobbyId = arenaOnline.WinsByOPlayer.ToDictionary(
                    kvp => (int)kvp.Key.inLobbyId,
                    kvp => kvp.Value
                );
                deathsByInLobbyId = arenaOnline.DeathsByOPlayer.ToDictionary(
                    kvp => (int)kvp.Key.inLobbyId,
                    kvp => kvp.Value
                );
                roundDeathsByInLobbyId = arenaOnline.RoundDeathsByOPlayer.ToDictionary(
                    kvp => (int)kvp.Key.inLobbyId,
                    kvp => kvp.Value
                );
                totalScoreByInLobbyId = arenaOnline.TotalScoreByOPlayer.ToDictionary(
                    kvp => (int)kvp.Key.inLobbyId,
                    kvp => kvp.Value
                );
                scoreByInLobbyId = arenaOnline.ScoreByOPlayer.ToDictionary(
                    kvp => (int)kvp.Key.inLobbyId,
                    kvp => kvp.Value
                );
                allKillsByInLobbyId = arenaOnline.AllKillsByOPlayer.ToDictionary(
                    kvp => (int)kvp.Key.inLobbyId,
                    kvp => kvp.Value
                        .Select(trophy => trophy.ToString())
                        .ToList()
                );
                roundKillsByInLobbyId = arenaOnline.RoundKillsByOPlayer.ToDictionary(
                    kvp => (int)kvp.Key.inLobbyId,
                    kvp => kvp.Value
                        .Select(trophy => trophy.ToString())
                        .ToList()
                );

                playersLateWaitingInLobby = new List<ushort>(arenaOnline.playersLateWaitingInLobbyForNextRound);

                playersChoosingSlugs = new Dictionary<string, int>(arenaOnline.playersInLobbyChoosingSlugs);
                countdownSafetyCatchTimer = arenaOnline.countdownSafetyCatchTimer;
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

                bannedSlugs = new List<int>(arenaOnline.bannedSlugs);
                voidMasterEnabled = arenaOnline.voidMasterEnabled;
                voidSpawnLethalityFactor = arenaOnline.voidSpawnLethalityFactor;
                amoebaDuration = arenaOnline.amoebaDuration;
                fullInvisInRippleSpace = arenaOnline.fullInvisInRippleSpace;
                amoebaControl = arenaOnline.amoebaControl;
                friendlyFire = arenaOnline.friendlyFire;
                enableOverseer = arenaOnline.enableOverseer;

                foodScore = arenaOnline.foodScore;

                spearHitScore = arenaOnline.spearHitScore;
                killScore = arenaOnline.killScore;
                survivalScore = arenaOnline.survivalScore;
                denRule = arenaOnline.denEntryRule;
                denScore = arenaOnline.denScore;
                hostLoadedOverlay = arenaOnline.hostLoadedOverlay;
                emptyDeathScore = arenaOnline.emptyDeathScore;
                challengeDenEjection = arenaOnline.challengeDenEjection;

                artiExplosionCapacity = arenaOnline.artiExplosionCount;
                artiParryDistance = arenaOnline.artiParryDistanceMult;
                artiParryLeniency = arenaOnline.artiParryLeniency;
                enableMeadowCosmetics = arenaOnline.enableMeadowCosmetics;

            }

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                Lobby lobby = (Lobby)resource;
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

                arenaOnline.WinsByOPlayer = OnlineManager.players.ToDictionary(
                    player => player,
                    player => winsByInLobbyId.TryGetValue(player.inLobbyId, out int value)
                        ? value
                        : 0
                );
                arenaOnline.DeathsByOPlayer = OnlineManager.players.ToDictionary(
                    player => player,
                    player => deathsByInLobbyId.TryGetValue(player.inLobbyId, out int value)
                        ? value
                        : 0
                );
                arenaOnline.RoundDeathsByOPlayer = OnlineManager.players.ToDictionary(
                    player => player,
                    player => roundDeathsByInLobbyId.TryGetValue(player.inLobbyId, out int value)
                        ? value
                        : 0
                );
                arenaOnline.TotalScoreByOPlayer = OnlineManager.players.ToDictionary(
                    player => player,
                    player => totalScoreByInLobbyId.TryGetValue(player.inLobbyId, out int value)
                        ? value
                        : 0
                );
                arenaOnline.ScoreByOPlayer = OnlineManager.players.ToDictionary(
                    player => player,
                    player => scoreByInLobbyId.TryGetValue(player.inLobbyId, out int value)
                        ? value
                        : 0
                );
                arenaOnline.AllKillsByOPlayer = OnlineManager.players.ToDictionary(
                    player => player,
                    player => allKillsByInLobbyId.TryGetValue(player.inLobbyId, out List<string>? value)
                        ? value.Select(IconSymbol.IconSymbolData.IconSymbolDataFromString).ToList()
                        : []
                );
                arenaOnline.RoundKillsByOPlayer = OnlineManager.players.ToDictionary(
                    player => player,
                    player => roundKillsByInLobbyId.TryGetValue(player.inLobbyId, out List<string>? value)
                        ? value.Select(IconSymbol.IconSymbolData.IconSymbolDataFromString).ToList()
                        : []
                );

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
                arenaOnline.survivalScore = survivalScore;
                arenaOnline.denEntryRule = denRule;
                arenaOnline.denScore = denScore;
                arenaOnline.hostLoadedOverlay = hostLoadedOverlay;
                arenaOnline.emptyDeathScore = emptyDeathScore;
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
