using System;
using System.Linq;
using RainMeadow.Arena.ArenaOnlineGameModes.Drown;
namespace RainMeadow
{
    internal class DrownData : OnlineResource.ResourceData
    {

        public int currentWaveTimer;
        public int currentWave;
        public int spearCost;
        public int spearExpCost;
        public int bombCost;
        public int respCost;
        public int denCost;
        public int maxCreatures;
        public bool densOpened;
        public override ResourceDataState MakeState(OnlineResource resource)
        {
            return new State(this, resource);
        }

        internal class State : ResourceDataState
        {
            [OnlineField]
            int currentWaveTimer;
            [OnlineField]
            int currentWave;
            [OnlineField]
            int spearCost;
            [OnlineField]
            int spearExplCost;
            [OnlineField]
            int bombCost;
            [OnlineField]
            int respCost;
            [OnlineField]
            int denCost;
            [OnlineField]
            int maxCreatures;
            [OnlineField]
            int creatureCleanupWaves;

            [OnlineField]
            bool densOpened;
            public State() { }
            public State(DrownData drownLobbyData, OnlineResource onlineResource)
            {
                
                ArenaOnlineGameMode arena = (onlineResource as Lobby).gameMode as ArenaOnlineGameMode;
                var cachedDrown = DrownMode.isDrownMode(arena, out var drownData);

                if (cachedDrown)
                {
                    currentWaveTimer = (drownData).currentWaveTimer;
                    currentWave = drownData.currentWave;
                    densOpened = drownData.openedDen;
                    spearCost = drownData.spearCost;
                    spearExplCost = drownData.spearExplCost;
                    bombCost = drownData.bombCost;
                    respCost = drownData.respCost;
                    denCost = drownData.denCost;
                    maxCreatures = drownData.maxCreatures;
                    creatureCleanupWaves = drownData.creatureCleanupWaves;

                }

            }

            public override Type GetDataType() => typeof(DrownData);

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                var lobby = (resource as Lobby);
                var cachedDrown = DrownMode.isDrownMode((lobby.gameMode as ArenaOnlineGameMode), out var drownData);

                if (cachedDrown && drownData != null && (drownData as DrownMode != null))
                {
                    (drownData).currentWaveTimer = currentWaveTimer;
                    drownData.currentWave = currentWave;
                    drownData.openedDen = densOpened;
                    drownData.spearCost = spearCost;
                    drownData.spearExplCost = spearExplCost;
                    drownData.bombCost = bombCost;
                    drownData.respCost = respCost;
                    drownData.denCost = denCost;
                    drownData.maxCreatures = maxCreatures;
                    drownData.creatureCleanupWaves = creatureCleanupWaves;                    

                }
            }
        }
    }
}