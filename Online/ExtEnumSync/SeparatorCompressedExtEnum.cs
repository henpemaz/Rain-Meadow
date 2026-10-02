using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainMeadow;

// compress by sorting by amount of separator, then compressing the different prefixes by extra letters, then compressing the last suffix by extra letter and size
public class SeparatorCompressedExtEnum(Type enumType, char separator) : CompressedExtEnumBase(enumType)
{
    public char separator {get;} = separator;
    private static List<ExtEnumEntry> RecursiveCut(List<ExtEnumEntry> arrangedValues, char separator)
    {
        if (arrangedValues.Count == 0) return [];

        // Assuming that they have the same amount of separator
        if (arrangedValues.First().value.Count(x => separator == x) == 0)
        {
            arrangedValues = SizeAndFirstLetterCompressedExtEnum.Compression(arrangedValues);
            for (int i = 0; i < arrangedValues.Count; i++)
            {
                arrangedValues[i].value = (char)(arrangedValues[i].value.First() + separator) + arrangedValues[i].value.Substring(1);
            }
        }
        else
        {
            arrangedValues.Sort((x, y) => x.value.Split(separator).First().CompareTo(y.value.Split(separator).First()));
            List<List<ExtEnumEntry>> cuttedList = [];
            List<ExtEnumEntry> uniquePrefix = []; // ExtEnumEntry to pass it into the blender- uh the compresser
            for (int i = 0; i < arrangedValues.Count; i++)
            {
                if (i == 0
                    || cuttedList[cuttedList.Count - 1].First().value.Split(separator).First() != arrangedValues[i].value.Split(separator).First())
                {
                    uniquePrefix.Add(new(arrangedValues[i].value.Split(separator).First(), 0));
                    cuttedList.Add([arrangedValues[i]]);
                }
                else
                {
                    cuttedList[cuttedList.Count - 1].Add(arrangedValues[i]);
                }
            }

            int currentUniquePrefixIndex = 0;
            uniquePrefix = FirstLetterCompressedExtEnum.Compression(uniquePrefix);
            for (int i = 0; i < cuttedList.Count; i++)
            {
                if (!FirstLetterCompressedExtEnum.DoesStringMatchCompressed(
                        cuttedList[i][0].value.Split(separator).First(),
                        uniquePrefix[currentUniquePrefixIndex].value))
                { currentUniquePrefixIndex++; }

                List<ExtEnumEntry> withoutPrefixCut = [];
                for (int j = 0; j < cuttedList[i].Count; j++)
                {
                    withoutPrefixCut.Add(new ExtEnumEntry(
                        cuttedList[i][j].position,
                        string.Join(separator.ToString(), cuttedList[i][j].value.Split(separator).Skip(1))
                    ));
                }

                withoutPrefixCut = RecursiveCut(withoutPrefixCut, separator);

                for (int j = 0; j < cuttedList[i].Count; j++)
                {
                    cuttedList[i][j].value = uniquePrefix[currentUniquePrefixIndex].value + separator + withoutPrefixCut[j].value;
                    cuttedList[i][j].position = withoutPrefixCut[j].position; // somehow the shuffling makes a mess in the positions, so we have to set it back
                }
            }
        }

        return arrangedValues;
    }
    public static List<ExtEnumEntry> Compression(List<ExtEnumEntry> arrangedValues, char separator)
    {
        if (arrangedValues.Count == 0) return [];

        // sorting and cutting the list by amount of separator
        arrangedValues.Sort((x, y) => x.value.Count(x => separator == x) - y.value.Count(x => separator == x));
        List<List<ExtEnumEntry>> cuttedList = [];
        for (int i = 0; i < arrangedValues.Count; i++)
        {
            if (i == 0
                || cuttedList[cuttedList.Count - 1][0].value.Count(x => separator == x) != arrangedValues[i].value.Count(x => separator == x))
            {
                cuttedList.Add([arrangedValues[i]]);
            }
            else
            {
                cuttedList[cuttedList.Count - 1].Add(arrangedValues[i]);
            }
        }

        // Checking each cut
        for (int j = 0; j < cuttedList.Count; j++)
        {
            cuttedList[j] = RecursiveCut(cuttedList[j], separator);
        }

        return arrangedValues;
    }
    public static bool DoesStringMatchCompressed(string value, string compressedValue, char separator)
    {
        string[] cutValue = value.Split(separator);
        string[] cutCompressedValue = compressedValue.Split(separator);
        if (cutValue.Length != cutCompressedValue.Length) return false;
        for (int i = 0; i < cutValue.Length; i++)
        {
            if (i == cutValue.Length - 1)
            {
               if (SizeAndFirstLetterCompressedExtEnum.DoesStringMatchCompressed(
                    cutValue[i],
                    (char)(cutCompressedValue[i].First() - separator) + cutCompressedValue[i].Substring(1)))
                {
                    return true;
                }
                return false;
            }
            else if (!FirstLetterCompressedExtEnum.DoesStringMatchCompressed(cutValue[i], cutCompressedValue[i]))
            {
                return false;
            }
        }
        return false;
    }

    protected override string[] GetCompressedEntries()
    {
        return ExtEnumEntry.ExtEnumEntryToArray(Compression(ExtEnumEntry.ToExtEnumEntryList(this.entriesMap), separator));
    }
    public override DecompressionResult ReadAndSyncCompressedEntries(string[] compressedEntries)
    {
        return ProcessCompression(compressedEntries,
            (x,y) => DoesStringMatchCompressed(x, y, separator),
            (x,y) => x.Split(separator).Last() == y.Split(separator).Last().Substring(1));
    }
}
