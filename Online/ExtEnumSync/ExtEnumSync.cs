using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow;
public static partial class ExtEnumSync
{
    // I don't want to assume that the order of SyncedExtEnumList is the same. I'll leave it public if some mods want to sync more enums here.
    // Also, having it static initialized is no biggies ! We'll assign the values later.
    public static SeparatorCompressedExtEnum OnlineStateTypeMap {get;}
    public static List<CompressedExtEnumBase> SyncedExtEnumList = new()
    {
        new FirstLetterCompressedExtEnum(typeof(SlugcatStats.Name)),
        new FirstLetterCompressedExtEnum(typeof(SlugcatStats.Timeline)),
        new SizeAndFirstLetterCompressedExtEnum(typeof(AbstractPhysicalObject.AbstractObjectType)),
        new SizeAndFirstLetterCompressedExtEnum(typeof(CreatureTemplate.Type)),
        (OnlineStateTypeMap = new SeparatorCompressedExtEnum(typeof(OnlineState.StateType), '.')),
    };

    // We need a double special character for the trim since the compressed value can have ANY character in it
    public const string compressionSeparator = ";;";
    public static string CompressedExtEnumArrayToString(string[] compressedExtEnum)
    {
        return string.Join(compressionSeparator, compressedExtEnum);
    }
    public static string[] CompressedExtEnumStringToArray(string compressedExtEnum)
    {
        return compressedExtEnum.Split([compressionSeparator], StringSplitOptions.RemoveEmptyEntries);
    }

    public static void ResetEnumEntriesMapping()
    {
        for (int i = 0; i < SyncedExtEnumList.Count; i++)
        {
            // ordering them alphabetically to reduce order mismatch chances
            SyncedExtEnumList[i].SetEnumEntriesFromCurrentExtEnum(true);
            if (OnlineManager.lobby.isOwner) {SyncedExtEnumList[i].LogMappedExtEnum();}
        }
        RainMeadow.Info($"Enum entries map reset for <{SyncedExtEnumList.Count}> enums : [{string.Join(", ", SyncedExtEnumList.Select(x => x.enumType.FullName))}]");
    }

    public static bool IsSyncedExtEnum(Type enumType, out CompressedExtEnumBase compressedExtEnum)
    {
        compressedExtEnum = null!;
        int i = SyncedExtEnumList.FindIndex(x => x.enumType == enumType);
        if (i > -1)
        {
            compressedExtEnum = SyncedExtEnumList[i];
            return true;
        }
        return false;
    }

    public static byte MappedIndex<T>(this T extEnum) where T : ExtEnum<T>
    {
        if (OnlineManager.lobby is not null && IsSyncedExtEnum(typeof(T), out var compressedExtEnum))
        {
            return (byte)compressedExtEnum.GetIndex(extEnum);
        }
        else
        {
            return (byte)extEnum.Index;
        }
    }
    public static string? GetExtEnumValue<T>(byte index) where T : ExtEnum<T>
    {
        string? entry = null;
        bool mapped = IsSyncedExtEnum(typeof(T), out var compressedExtEnum);
        if (OnlineManager.lobby is not null && mapped)
        {
            entry = compressedExtEnum.GetValueFromIndex(index);
        }
        entry = entry is null ? ExtEnum<T>.values.GetEntry(index) : entry;
        if (entry is null) { RainMeadow.Error($"Couldn't find enum index {index} from enum type {typeof(T).FullName}. Mapped Enum ? {mapped}. Numbers of entries : {(mapped ? compressedExtEnum.entriesMap.Count : ExtEnum<T>.values.Count)}"); }
        return entry;
    }

    internal static void LogTestCompression()
    {
        for (int i = 0; i < SyncedExtEnumList.Count; i++)
        {
            SyncedExtEnumList[i].LogCompressionTest(i == 0);
        }
    }
}