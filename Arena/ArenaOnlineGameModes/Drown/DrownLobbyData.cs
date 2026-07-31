using System;

namespace RainMeadow
{
    internal class DrownData : OnlineResource.ResourceData
    {
        public int currentWaveTimer;
        public int currentWave;
        public int rockCost;

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
            int rockCost;
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

            public State(DrownData lobbyData, OnlineResource onlineResource)
            {
                if (!DrownMode.IsDrownMode(out DrownMode drown))
                    return;

                currentWaveTimer = drown.currentWaveTimer;
                currentWave = drown.currentWave;
                densOpened = drown.openedDen;
                rockCost = drown.rockCost;
                spearCost = drown.spearCost;
                spearExplCost = drown.spearExplCost;
                bombCost = drown.bombCost;
                respCost = drown.respCost;
                denCost = drown.denCost;
                maxCreatures = drown.maxCreatures;
                creatureCleanupWaves = drown.creatureCleanupWaves;
            }

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                if (!DrownMode.IsDrownMode(out DrownMode drown))
                    return;

                drown.currentWaveTimer = currentWaveTimer;
                drown.currentWave = currentWave;
                drown.openedDen = densOpened;
                drown.rockCost = rockCost;
                drown.spearCost = spearCost;
                drown.spearExplCost = spearExplCost;
                drown.bombCost = bombCost;
                drown.respCost = respCost;
                drown.denCost = denCost;
                drown.maxCreatures = maxCreatures;
                drown.creatureCleanupWaves = creatureCleanupWaves;
            }

            public override Type GetDataType() => typeof(DrownData);
        }
    }
}
