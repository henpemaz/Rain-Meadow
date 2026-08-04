using System.Runtime.CompilerServices;

public static class ArenaPlayerExtensions
{
    private static readonly ConditionalWeakTable<ArenaSitting.ArenaPlayer, Data> _dataByInstance = new();

    extension (ArenaSitting.ArenaPlayer self)
    {
        public int RoundDeaths
        {
            get => _dataByInstance.GetOrCreateValue(self).roundDeaths;
            set => _dataByInstance.GetOrCreateValue(self).roundDeaths = value;
        }
    }

    private record Data
    {
        public int roundDeaths;
    }
}
