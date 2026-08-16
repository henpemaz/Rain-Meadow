using System.Runtime.CompilerServices;

public static class GameTypeSetupExtensions
{
    private static readonly ConditionalWeakTable<ArenaSetup.GameTypeSetup, Data> _dataByInstance = new();

    extension (ArenaSetup.GameTypeSetup self)
    {
        public int KillScore
        {
            get => _dataByInstance.GetOrCreateValue(self).killScore;
            set => _dataByInstance.GetOrCreateValue(self).killScore = value;
        }

        public int EmptyDeathScore
        {
            get => _dataByInstance.GetOrCreateValue(self).emptyDeathScore;
            set => _dataByInstance.GetOrCreateValue(self).emptyDeathScore = value;
        }
    }

    private record Data
    {
        public int killScore;
        public int emptyDeathScore;
    }
}
