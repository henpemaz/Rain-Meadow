using System;

namespace RainMeadow;
public static partial class ExtEnumSync
{
    public static Type? GetExtEnumType(this ExtEnumType extEnumType)
    {
        foreach (var extEnumData in ExtEnumBase.valueDictionary)
        {
            if (extEnumData.Value == extEnumType) return extEnumData.Key;
        }
        return null;
    }
    public static bool TryGetExtEnumType(this ExtEnumType extEnumType, out Type type)
    {
        type = GetExtEnumType(extEnumType)!;
        return type is not null;
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
}