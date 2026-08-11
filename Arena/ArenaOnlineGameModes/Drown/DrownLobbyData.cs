using System;

namespace RainMeadow
{
    internal class DrownData : OnlineResource.ResourceData
    {
        public int currentWaveTimer;
        public int currentWave;
        public int rockCost;

        public int spearCost;
        public int explosiveSpearCost;
        public int bombCost;
        public int electricSpearCost;
        public int boomerangCost;
        public int respCost;
        public int denCost;
        public int maxCreatures;
        public int creatureCleanupWaves;
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
            int explosiveSpearCost;
            [OnlineField]
            int bombCost;
            [OnlineField]
            int electricSpearCost;
            [OnlineField]
            int boomerangCost;
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
                rockCost = drown.RockCost;
                spearCost = drown.SpearCost;
                explosiveSpearCost = drown.ExplosiveSpearCost;
                bombCost = drown.BombCost;
                electricSpearCost = drown.ElectricSpearCost;
                boomerangCost = drown.BoomerangCost;
                respCost = drown.RespCost;
                denCost = drown.DenCost;
                maxCreatures = drown.MaxCreatures;
                creatureCleanupWaves = drown.CreatureCleanupWaves;
            }

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                if (!DrownMode.IsDrownMode(out DrownMode drown))
                    return;

                drown.currentWaveTimer = currentWaveTimer;
                drown.currentWave = currentWave;
                drown.openedDen = densOpened;
                drown.RockCost = rockCost;
                drown.SpearCost = spearCost;
                drown.ExplosiveSpearCost = explosiveSpearCost;
                drown.BombCost = bombCost;
                drown.ElectricSpearCost = electricSpearCost;
                drown.BoomerangCost = boomerangCost;
                drown.RespCost = respCost;
                drown.DenCost = denCost;
                drown.MaxCreatures = maxCreatures;
                drown.CreatureCleanupWaves = creatureCleanupWaves;
            }

            public override Type GetDataType() => typeof(DrownData);
        }
    }
}
