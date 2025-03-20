using RainMeadow.Generics;
using System;
using System.Collections.Generic;

namespace RainMeadow
{
    internal class ArenaLobbyData : OnlineResource.ResourceData
    {

        public DynamicOrderedPlayerIDs playersReadiedUpz = new();
        public List<string> pain = new();

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
            [OnlineField(nullable = true)]
            public DynamicOrderedPlayerIDs playersReadiedUp;
            [OnlineField(nullable = true)]
            public List<string> playersReadiedUpString;
            [OnlineField]
            public bool countdownInitiatedHoldFire;
            [OnlineField]
            public int playerEnteredGame;
            [OnlineField]
            public int currentLevel;
            [OnlineField]
            public int totalLevels;
            [OnlineField]
            public int arenaSetupTime;
            [OnlineField]
            public int saintAscendanceTimer;
            [OnlineField]
            public bool sainot;
            [OnlineField]
            public bool painCatEgg;
            [OnlineField]
            public bool painCatThrows;
            [OnlineField]
            public bool painCatLizard;
            [OnlineField]
            public bool disableMaul;
            [OnlineField]
            public bool disableArtiStun;
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


                //playersReadiedUp = arenaLobbyData.playersReadiedUpz;
                playersReadiedUpString = arenaLobbyData.pain;
                //if (arenaLobbyData.playersReadiedUpz.list == null)
                //{
                //    arenaLobbyData.playersReadiedUpz.list = new List<MeadowPlayerId>();
                //}

                // playersReadiedUp.list = arenaLobbyData.playersReadiedUpz.list;

                RainMeadow.Debug($"========================= {playersReadiedUpString.Count}");



                playersChoosingSlugs = arena.playersInLobbyChoosingSlugs;
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
                (lobby.gameMode as ArenaOnlineGameMode).playersReadiedUpString = playersReadiedUpString;

                //(lobby.gameMode as ArenaOnlineGameMode).playersReadiedUp = playersReadiedUp.list;
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

                RainMeadow.Debug((lobby.gameMode as ArenaOnlineGameMode).playersReadiedUpString.Count);

            }

            public override Type GetDataType() => typeof(ArenaLobbyData);
        }
    }
}