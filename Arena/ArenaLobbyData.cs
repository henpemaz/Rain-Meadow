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
            public Dictionary<string, int> playersChoosingSlugs;
            [OnlineField]
            public bool countdownInitiatedHoldFire;
            [OnlineField]
            public int arenaCountDownTimerRemix;


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
                playersChoosingSlugs = arena.playersInLobbyChoosingSlugs;
                countdownInitiatedHoldFire = arena.countdownInitiatedHoldFire;
                arenaCountDownTimerRemix = arena.setupTime;

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
                (lobby.gameMode as ArenaOnlineGameMode).countdownInitiatedHoldFire = countdownInitiatedHoldFire;
                (lobby.gameMode as ArenaOnlineGameMode).setupTime = arenaCountDownTimerRemix;


            }

            public override Type GetDataType() => typeof(ArenaLobbyData);
        }
    }
}
