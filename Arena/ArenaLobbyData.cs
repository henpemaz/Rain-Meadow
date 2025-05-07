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

            // Group: arenaSetup
            [OnlineField(group = "arenaSetup")]
            public List<string> playList;
            [OnlineField(group = "arenaSetup")]
            public int totalLevels;
            [OnlineField(group = "arenaSetup")]
            public int arenaSetupTime;
            [OnlineField(group = "arenaSetup")]
            public int saintAscendanceTimer;
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
            public Dictionary<int, int> playerNumberWithKills;
            [OnlineField(group = "arenaGameplay")]
            public Dictionary<int, int> playerNumberWithDeaths;
            [OnlineField(group = "arenaGameplay")]
            public Dictionary<int, int> playerNumberWithWins;
            [OnlineField(group = "arenaGameplay")]
            public bool countdownInitiatedHoldFire;
            [OnlineField(group = "arenaGameplay")]
            public int playerEnteredGame;

            public State() { }
            public State(ArenaLobbyData arenaLobbyData, OnlineResource onlineResource)
            {
                ArenaOnlineGameMode arena = (onlineResource as Lobby).gameMode as ArenaOnlineGameMode;
                isInGame = RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame;             
                playList = arena.playList;
                arenaSittingOnlineOrder = new(arena.arenaSittingOnlineOrder);
                allPlayersReadyLockLobby = arena.allPlayersReadyLockLobby;
                returnToLobby = arena.returnToLobby;
                onlineArenaSettingsInterfaceMultiChoice = arena.onlineArenaSettingsInterfaceMultiChoice;
                onlineArenaSettingsInterfaceBool = arena.onlineArenaSettingsInterfaceeBool;
                playersReadiedUp = new(arena.playersReadiedUp.list.ToList());
                reigningChamps = new(arena.reigningChamps.list.ToList());
                playerNumberWithKills = new(arena.playerNumberWithKills);
                playerNumberWithDeaths = new(arena.playerNumberWithDeaths);
                playerNumberWithWins = new(arena.playerNumberWithWins);
                playersLateWaitingInLobby = new(arena.playersLateWaitingInLobbyForNextRound);

                playersChoosingSlugs = new(arena.playersInLobbyChoosingSlugs.ToDictionary<string, int>());
                countdownInitiatedHoldFire = arena.countdownInitiatedHoldFire;
                playerResultColors = arena.playerResultColors;
                playerEnteredGame = arena.playerEnteredGame;
                arenaSetupTime = arena.setupTime;
                sainot = arena.sainot;
                saintAscendanceTimer = arena.arenaSaintAscendanceTimer;
                currentGameMode = arena.currentGameMode;
                currentLevel = arena.currentLevel;
                totalLevels = arena.totalLevelCount;
                painCatEgg = arena.painCatEgg;
                painCatThrows = arena.painCatThrows;
                painCatLizard = arena.painCatLizard;
                disableMaul = arena.disableMaul;
                disableArtiStun = arena.disableArtiStun;

            }

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                var lobby = (resource as Lobby);
                (lobby.gameMode as ArenaOnlineGameMode).isInGame = isInGame;
                (lobby.gameMode as ArenaOnlineGameMode).playList = playList;
                (lobby.gameMode as ArenaOnlineGameMode).arenaSittingOnlineOrder = arenaSittingOnlineOrder;
                (lobby.gameMode as ArenaOnlineGameMode).allPlayersReadyLockLobby = allPlayersReadyLockLobby;
                (lobby.gameMode as ArenaOnlineGameMode).returnToLobby = returnToLobby;
                (lobby.gameMode as ArenaOnlineGameMode).onlineArenaSettingsInterfaceMultiChoice = onlineArenaSettingsInterfaceMultiChoice;
                (lobby.gameMode as ArenaOnlineGameMode).onlineArenaSettingsInterfaceeBool = onlineArenaSettingsInterfaceBool;
                (lobby.gameMode as ArenaOnlineGameMode).playersInLobbyChoosingSlugs = playersChoosingSlugs;
                (lobby.gameMode as ArenaOnlineGameMode).playersReadiedUp = playersReadiedUp;
                (lobby.gameMode as ArenaOnlineGameMode).reigningChamps = reigningChamps;
                (lobby.gameMode as ArenaOnlineGameMode).playerNumberWithKills = playerNumberWithKills;
                (lobby.gameMode as ArenaOnlineGameMode).playerNumberWithDeaths = playerNumberWithDeaths;
                (lobby.gameMode as ArenaOnlineGameMode).playerNumberWithWins = playerNumberWithWins;

                (lobby.gameMode as ArenaOnlineGameMode).playersLateWaitingInLobbyForNextRound = playersLateWaitingInLobby;


                (lobby.gameMode as ArenaOnlineGameMode).countdownInitiatedHoldFire = countdownInitiatedHoldFire;
                (lobby.gameMode as ArenaOnlineGameMode).playerResultColors = playerResultColors;
                (lobby.gameMode as ArenaOnlineGameMode).playerEnteredGame = playerEnteredGame;
                (lobby.gameMode as ArenaOnlineGameMode).setupTime = arenaSetupTime;
                (lobby.gameMode as ArenaOnlineGameMode).sainot = sainot;
                (lobby.gameMode as ArenaOnlineGameMode).arenaSaintAscendanceTimer = saintAscendanceTimer;
                (lobby.gameMode as ArenaOnlineGameMode).currentGameMode = currentGameMode;
                (lobby.gameMode as ArenaOnlineGameMode).currentLevel = currentLevel;
                (lobby.gameMode as ArenaOnlineGameMode).totalLevelCount = totalLevels;
                (lobby.gameMode as ArenaOnlineGameMode).painCatEgg = painCatEgg;
                (lobby.gameMode as ArenaOnlineGameMode).painCatThrows = painCatThrows;
                (lobby.gameMode as ArenaOnlineGameMode).painCatLizard = painCatLizard;
                (lobby.gameMode as ArenaOnlineGameMode).disableArtiStun = disableArtiStun;
                (lobby.gameMode as ArenaOnlineGameMode).disableMaul = disableMaul;

            }

            public override Type GetDataType() => typeof(ArenaLobbyData);
        }
    }
}