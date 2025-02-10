using System;
using System.Collections.Generic;

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
            [OnlineField]
            public bool isInGame;
            [OnlineField]
            public bool allPlayersReadyLockLobby;
            [OnlineField]
            public List<string> playList;
            [OnlineField]
            public List<ushort> arenaSittingOnlineOrder;
            [OnlineField]
            public bool returnToLobby;
            [OnlineField]
            public Dictionary<string, int> onlineArenaSettingsInterfaceMultiChoice;
            [OnlineField]
            public Dictionary<string, bool> onlineArenaSettingsInterfaceBool;
            [OnlineField]
            public Dictionary<ushort, int> playersChoosingSlugs;
            [OnlineField]
            public Dictionary<string, int> playerResultColors;
            [OnlineField]
            public List<ushort> playersReadiedUp;
            [OnlineField]
            public bool countdownInitiatedHoldFire;
            [OnlineField]
            public int playerEnteredGame;
            [OnlineField]
            public int clientsAreReadiedUp;
            [OnlineField]
            public int currentLevel;
            [OnlineField]
            public int totalLevels;
            [OnlineField]
            public int arenaSetupTime;
            [OnlineField]
            public bool sainot;
            [OnlineField]
            public bool painCatEgg;
            [OnlineField]
            public bool painCatThrows;
            [OnlineField]
            public bool painCatLizard;
            [OnlineField]
            public string currentGameMode;
            public State() { }
            public State(ArenaLobbyData arenaLobbyData, OnlineResource onlineResource)
            {
                ArenaOnlineGameMode arena = (onlineResource as Lobby).gameMode as ArenaOnlineGameMode;
                isInGame = RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame;
                playList = arena.playList;
                arenaSittingOnlineOrder = arena.arenaSittingOnlineOrder;
                allPlayersReadyLockLobby = arena.allPlayersReadyLockLobby;
                returnToLobby = arena.returnToLobby;
                onlineArenaSettingsInterfaceMultiChoice = arena.onlineArenaSettingsInterfaceMultiChoice;
                onlineArenaSettingsInterfaceBool = arena.onlineArenaSettingsInterfaceeBool;
                playersReadiedUp = arena.playersReadiedUp;
                playersChoosingSlugs = arena.playersInLobbyChoosingSlugs;
                countdownInitiatedHoldFire = arena.countdownInitiatedHoldFire;
                playerResultColors = arena.playerResultColors;
                playerEnteredGame = arena.playerEnteredGame;
                clientsAreReadiedUp = arena.clientsAreReadiedUp;
                arenaSetupTime = arena.setupTime;
                sainot = arena.sainot;
                currentGameMode = arena.currentGameMode;
                currentLevel = arena.currentLevel;
                totalLevels = arena.totalLevelCount;
                painCatEgg = arena.painCatEgg;
                painCatThrows = arena.painCatThrows;
                painCatLizard = arena.painCatLizard;
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
                (lobby.gameMode as ArenaOnlineGameMode).countdownInitiatedHoldFire = countdownInitiatedHoldFire;
                (lobby.gameMode as ArenaOnlineGameMode).playerResultColors = playerResultColors;
                (lobby.gameMode as ArenaOnlineGameMode).playerEnteredGame = playerEnteredGame;
                (lobby.gameMode as ArenaOnlineGameMode).clientsAreReadiedUp = clientsAreReadiedUp;
                (lobby.gameMode as ArenaOnlineGameMode).setupTime = arenaSetupTime;
                (lobby.gameMode as ArenaOnlineGameMode).sainot = sainot;
                (lobby.gameMode as ArenaOnlineGameMode).currentGameMode = currentGameMode;
                (lobby.gameMode as ArenaOnlineGameMode).currentLevel = currentLevel;
                (lobby.gameMode as ArenaOnlineGameMode).totalLevelCount = totalLevels;
                (lobby.gameMode as ArenaOnlineGameMode).painCatEgg = painCatEgg;
                (lobby.gameMode as ArenaOnlineGameMode).painCatThrows = painCatThrows;
                (lobby.gameMode as ArenaOnlineGameMode).painCatLizard = painCatLizard;

            }

            public override Type GetDataType() => typeof(ArenaLobbyData);
        }
    }
}
