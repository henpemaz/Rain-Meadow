using System.Runtime.CompilerServices;

public static class ArenaPlayerExtensions
{
    private static readonly ConditionalWeakTable<ArenaSitting.ArenaPlayer, Data> _dataByInstance = new();

    public static int GetRoundDeaths(this ArenaSitting.ArenaPlayer self)
    {
        return _dataByInstance.GetOrCreateValue(self).roundDeaths;
    }

    public static void SetRoundDeaths(this ArenaSitting.ArenaPlayer self, int value)
    {
        _dataByInstance.GetOrCreateValue(self).roundDeaths = value;
    }

    private record Data
    {
        public int roundDeaths;
    }
}
