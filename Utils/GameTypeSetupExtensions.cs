using System.Runtime.CompilerServices;

public static class GameTypeSetupExtensions
{
    private static readonly ConditionalWeakTable<ArenaSetup.GameTypeSetup, Data> _dataByInstance = new();

    public static int GetKillScore(this ArenaSetup.GameTypeSetup self)
    {
        return _dataByInstance.GetOrCreateValue(self).killScore;
    }

    public static void SetKillScore(this ArenaSetup.GameTypeSetup self, int value)
    {
        _dataByInstance.GetOrCreateValue(self).killScore = value;
    }

    public static int GetEmptyDeathScore(this ArenaSetup.GameTypeSetup self)
    {
        return _dataByInstance.GetOrCreateValue(self).emptyDeathScore;
    }

    public static void SetEmptyDeathScore(this ArenaSetup.GameTypeSetup self, int value)
    {
        _dataByInstance.GetOrCreateValue(self).emptyDeathScore = value;
    }

    private record Data
    {
        public int killScore;
        public int emptyDeathScore;
    }
}
