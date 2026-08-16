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


            // Delta-aware dictionaries rather than plain ones. A plain Dictionary field is
            // compared by reference (OnlineState.OnlineFieldAttribute.ComparisonMethod), and this
            // state is rebuilt every tick, so the "arenaScore" group used to register as changed
            // and be re-serialized on every single tick even when nobody had scored. These types
            // return null from Delta() when nothing changed, which clears the group's value flag
            // and skips both the serialization and the send.
            // They must be nullable: if any one field in the group changes, the whole group is
            // serialized, and the unchanged fields are null deltas at that point.

            [OnlineField(nullable = true, group = "arenaScore")]
            public UshortToIntDict winsByInLobbyId;

            [OnlineField(nullable = true, group = "arenaScore")]
            public UshortToIntDict deathsByInLobbyId;

            [OnlineField(nullable = true, group = "arenaScore")]
            public UshortToIntDict roundDeathsByInLobbyId;

            [OnlineField(nullable = true, group = "arenaScore")]
            public UshortToIntDict totalScoreByInLobbyId;

            [OnlineField(nullable = true, group = "arenaScore")]
            public UshortToIntDict scoreByInLobbyId;

            [OnlineField(nullable = true, group = "arenaScore")]
            public UshortToKillListDict allKillsByInLobbyId;

            [OnlineField(nullable = true, group = "arenaScore")]
            public UshortToKillListDict roundKillsByInLobbyId;


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

                winsByInLobbyId = SnapshotScores(arenaOnline.WinsByOPlayer);
                deathsByInLobbyId = SnapshotScores(arenaOnline.DeathsByOPlayer);
                roundDeathsByInLobbyId = SnapshotScores(arenaOnline.RoundDeathsByOPlayer);
                totalScoreByInLobbyId = SnapshotScores(arenaOnline.TotalScoreByOPlayer);
                scoreByInLobbyId = SnapshotScores(arenaOnline.ScoreByOPlayer);
                allKillsByInLobbyId = SnapshotKills(arenaOnline.AllKillsByOPlayer);
                roundKillsByInLobbyId = SnapshotKills(arenaOnline.RoundKillsByOPlayer);

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

                // Updated in place rather than rebuilt: ReadTo runs for every state that arrives,
                // so allocating seven fresh dictionaries here was per-tick garbage on every
                // client. Clear() keeps the buckets, so re-adding the same players costs nothing.
                ApplyScores(arenaOnline.WinsByOPlayer, winsByInLobbyId);
                ApplyScores(arenaOnline.DeathsByOPlayer, deathsByInLobbyId);
                ApplyScores(arenaOnline.RoundDeathsByOPlayer, roundDeathsByInLobbyId);
                ApplyScores(arenaOnline.TotalScoreByOPlayer, totalScoreByInLobbyId);
                ApplyScores(arenaOnline.ScoreByOPlayer, scoreByInLobbyId);
                ApplyKills(arenaOnline.AllKillsByOPlayer, allKillsByInLobbyId);
                ApplyKills(arenaOnline.RoundKillsByOPlayer, roundKillsByInLobbyId);

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

            /// <summary>
            /// Snapshots a per-player score dictionary into its delta-aware wire form.
            /// </summary>
            private static UshortToIntDict SnapshotScores(Dictionary<OnlinePlayer, int> scores)
            {
                List<KeyValuePair<ushort, int>> entries = new(scores.Count);

                foreach (KeyValuePair<OnlinePlayer, int> score in scores)
                    entries.Add(new KeyValuePair<ushort, int>(score.Key.inLobbyId, score.Value));

                return new UshortToIntDict(entries);
            }

            /// <summary>
            /// Snapshots a per-player trophy dictionary into its delta-aware wire form.
            /// </summary>
            /// <remarks>
            /// The trophy lists are wrapped by reference, not copied, and no
            /// <see cref="IconSymbol.IconSymbolData"/> is converted to a string here. Both of
            /// those only happen if <see cref="ArenaKillList"/> reports a change, which keeps a
            /// long sitting's accumulated kills from being re-serialized every tick. See
            /// <see cref="ArenaKillList(List{IconSymbol.IconSymbolData})"/> for why the caller
            /// must replace these lists rather than mutate them.
            /// </remarks>
            private static UshortToKillListDict SnapshotKills(
                Dictionary<OnlinePlayer, List<IconSymbol.IconSymbolData>> kills)
            {
                List<KeyValuePair<ushort, ArenaKillList>> entries = new(kills.Count);

                foreach (KeyValuePair<OnlinePlayer, List<IconSymbol.IconSymbolData>> kill in kills)
                {
                    entries.Add(
                        new KeyValuePair<ushort, ArenaKillList>(
                            kill.Key.inLobbyId,
                            new ArenaKillList(kill.Value)
                        )
                    );
                }

                return new UshortToKillListDict(entries);
            }

            private static void ApplyScores(
                Dictionary<OnlinePlayer, int> target,
                UshortToIntDict source)
            {
                target.Clear();

                foreach (OnlinePlayer player in OnlineManager.players)
                {
                    target[player] = source.lookup.TryGetValue(player.inLobbyId, out int score)
                        ? score
                        : 0;
                }
            }

            /// <remarks>
            /// The parsed trophy lists are handed over by reference. When the "arenaScore" group
            /// is unchanged the incoming state keeps the baseline's already-parsed lists, so
            /// nothing is re-parsed and nothing is allocated. Clients only ever read these
            /// dictionaries - only the lobby owner writes them, and it writes fresh lists.
            /// </remarks>
            private static void ApplyKills(
                Dictionary<OnlinePlayer, List<IconSymbol.IconSymbolData>> target,
                UshortToKillListDict source)
            {
                target.Clear();

                foreach (OnlinePlayer player in OnlineManager.players)
                {
                    target[player] =
                        source.lookup.TryGetValue(player.inLobbyId, out ArenaKillList kills)
                            ? kills.kills
                            : [];
                }
            }

            public override Type GetDataType() => typeof(ArenaLobbyData);
        }
    }
}
