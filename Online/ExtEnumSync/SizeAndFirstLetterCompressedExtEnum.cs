using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainMeadow;

// compress by sorting by size, put it as a char at the start and then removing the extra letters needed to guess the enum
public class SizeAndFirstLetterCompressedExtEnum(Type enumType) : CompressedExtEnumBase(enumType)
{
    public static List<ExtEnumEntry> Compression(List<ExtEnumEntry> arrangedValues)
    {
        // Sorting and cutting the list by size
        arrangedValues.Sort((x, y) => x.value.Length - y.value.Length);
        List<List<ExtEnumEntry>> cuttedList = [];
        for (int i = 0; i < arrangedValues.Count; i++)
        {
            if (i == 0 || cuttedList[cuttedList.Count - 1][0].value.Length != arrangedValues[i].value.Length)
            {
                cuttedList.Add([arrangedValues[i]]);
            }
            else
            {
                cuttedList[cuttedList.Count - 1].Add(arrangedValues[i]);
            }
        }

        // Checking each cut and sorting them individiually
        for (int i = 0; i < cuttedList.Count; i++)
        {
            ushort length = (ushort)cuttedList[i][0].value.Length;

            cuttedList[i].Sort((x, y) => x.value.CompareTo(y.value));
            cuttedList[i] = FirstLetterCompressedExtEnum.Compression(cuttedList[i]);

            for (int j = 0; j < cuttedList[i].Count; j++)
            {
                cuttedList[i][j].value = (char)length + cuttedList[i][j].value;
            }
        }

        // returning the completed list
        return arrangedValues;
    }
    public static string SplitCompressedSizeAndValue(string compressedValueNSize, out int size)
    {
        if (compressedValueNSize.Length < 2) { size = 0; return ""; }
        size = compressedValueNSize[0];
        return compressedValueNSize.Substring(1);
    }
    public static bool DoesStringMatchCompressed(string value, string compressedValueNSize)
    {
        string compressedValue = SplitCompressedSizeAndValue(compressedValueNSize, out var size);

        if (value.Length != size) return false;

        return FirstLetterCompressedExtEnum.DoesStringMatchCompressed(value, compressedValue);
    }
    protected override string[] GetCompressedEntries()
    {
        return ExtEnumEntry.ExtEnumEntryToArray(Compression(ExtEnumEntry.ToExtEnumEntryList(this.entriesMap)));
    }

    public override DecompressionResult ReadAndSyncCompressedEntries(string[] compressedEntries)
    {
        return ProcessCompression(compressedEntries,
            (x,y) => DoesStringMatchCompressed(x, y),
            (x,y) => x == y.Substring(1));
    }
}