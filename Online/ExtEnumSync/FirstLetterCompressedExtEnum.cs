using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainMeadow;

// compress by removing the extra letters needed to guess the enum
public class FirstLetterCompressedExtEnum(Type enumType) : CompressedExtEnumBase(enumType)
{
    public static List<ExtEnumEntry> Compression(List<ExtEnumEntry> sortedValues)
    {
        if (sortedValues.Count == 0) return [];

        // cutting the list by 1st letter
        List<List<ExtEnumEntry>> cuttedList = [];
        for (int i = 0; i < sortedValues.Count; i++)
        {
            if (i == 0
                || sortedValues[i].value.Length == 0
                || cuttedList[cuttedList.Count - 1][0].value.Length == 0
                || cuttedList[cuttedList.Count - 1][0].value[0] != sortedValues[i].value[0])
            {
                cuttedList.Add([sortedValues[i]]);
            }
            else
            {
                cuttedList[cuttedList.Count - 1].Add(sortedValues[i]);
            }
        }

        // Checking each cut
        for (int i = 0; i < cuttedList.Count; i++)
        {
            if (cuttedList[i].Count == 1)
            {
                cuttedList[i][0].value = cuttedList[i][0].value.Length == 0 ? "" : cuttedList[i][0].value[0].ToString();
            }
            else
            {
                // removing the first letter, call a recursive loop, then add it back
                List<ExtEnumEntry> withoutFirstLetterCut = [];
                for (int j = 0; j < cuttedList[i].Count; j++)
                {
                    // making a new object to not modify our old table rigth away
                    withoutFirstLetterCut.Add(new ExtEnumEntry(
                        cuttedList[i][j].position, cuttedList[i][j].value.Length <= 1 ? "" : cuttedList[i][j].value.Substring(1)));
                }

                withoutFirstLetterCut = Compression(withoutFirstLetterCut);

                for (int j = 0; j < cuttedList[i].Count; j++)
                {
                    cuttedList[i][j].value = (cuttedList[i][j].value.Length == 0 ? "" : cuttedList[i][j].value[0].ToString()) + withoutFirstLetterCut[j].value;
                }
            }
        }

        // merging things together- actually nevermind ! Classes are passed by reference, you just return here.
        return sortedValues;
    }
    public static bool DoesStringMatchCompressed(string value, string compressedValue)
        => value.Length >= compressedValue.Length && value.Substring(0, compressedValue.Length) == compressedValue;
    protected override string[] GetCompressedEntries()
    {
        List<ExtEnumEntry> sortedValues = ExtEnumEntry.ToExtEnumEntryList(this.entriesMap);
        if (sortedValues.Count == 0) return [];

        sortedValues.Sort((x, y) => x.value.CompareTo(y.value));

        return ExtEnumEntry.ExtEnumEntryToArray(Compression(sortedValues));
    }
    public override DecompressionResult ReadAndSyncCompressedEntries(string[] compressedEntries)
    {
        return ProcessCompression(compressedEntries,
            (x,y) => DoesStringMatchCompressed(x, y));
    }
}