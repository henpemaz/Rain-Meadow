using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainMeadow;

public class ExtEnumEntry : Serializer.ICustomSerializable
{
    public static List<ExtEnumEntry> ToExtEnumEntryList(Dictionary<string, int> entries, bool allToUnsetPos = false)
    {
        List<ExtEnumEntry> sortedValues = [];
        foreach (var keyValuePair in entries)
        {
            sortedValues.Add(new ExtEnumEntry(allToUnsetPos ? -1 : keyValuePair.Value, keyValuePair.Key));
        }
        return sortedValues;
    }
    public static List<ExtEnumEntry> ToExtEnumEntryList(string[] entries, bool allToUnsetPos = false)
    {
        List<ExtEnumEntry> sortedValues = [];
        for (int i = 0; i < entries.Length; i++)
        {
            sortedValues.Add(new ExtEnumEntry(allToUnsetPos ? -1 : i, entries[i]));
        }
        return sortedValues;
    }
    public static List<ExtEnumEntry> ToExtEnumEntryList(List<string> entries, bool allToUnsetPos = false)
    {
        List<ExtEnumEntry> sortedValues = [];
        for (int i = 0; i < entries.Count; i++)
        {
            sortedValues.Add(new ExtEnumEntry(allToUnsetPos ? -1 : i, entries[i]));
        }
        return sortedValues;
    }
    public static ExtEnumEntry[] ToExtEnumEntryArray(string[] entries, bool allToUnsetPos = false)
    {
        ExtEnumEntry[] sortedValues = new ExtEnumEntry[entries.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            sortedValues[i] = new ExtEnumEntry(allToUnsetPos ? -1 : i, entries[i]);
        }
        return sortedValues;
    }
    public static ExtEnumEntry[] ToExtEnumEntryArray(List<string> entries, bool allToUnsetPos = false)
    {
        ExtEnumEntry[] sortedValues = new ExtEnumEntry[entries.Count];
        for (int i = 0; i < entries.Count; i++)
        {
            sortedValues[i] = new ExtEnumEntry(allToUnsetPos ? -1 : i, entries[i]);
        }
        return sortedValues;
    }
    public static string[] ExtEnumEntryToArray(List<ExtEnumEntry> sortedValues)
    {
        string[] entries = new string[sortedValues.Count];
        for (int i = 0; i < sortedValues.Count; i++)
        {
            entries[sortedValues[i].position] = (string)sortedValues[i].value.Clone();
        }
        return entries;
    }
    public static string[] ExtEnumEntryToArray(ExtEnumEntry[] sortedValues)
    {
        string[] entries = new string[sortedValues.Length];
        for (int i = 0; i < sortedValues.Length; i++)
        {
            entries[sortedValues[i].position] = (string)sortedValues[i].value.Clone();
        }
        return entries;
    }

    public string value;
    public int position;
    public ExtEnumEntry(string value, int position) { this.value = (string)value.Clone(); this.position = position;}
    public ExtEnumEntry(int position, string value) : this(value, position) {}

    public void CustomSerialize(Serializer serializer)
    {
        serializer.Serialize(ref value);
        serializer.Serialize(ref position);
    }
}

public abstract class CompressedExtEnumBase
{
    public CompressedExtEnumBase(Type enumType)
    {
        if (!ExtEnumBase.valueDictionary.ContainsKey(enumType)) { throw new ArgumentException($"{enumType.FullName} is not an enum type !"); }
        this.enumType = enumType;
        this.enumEntries = ExtEnumBase.valueDictionary[enumType];
        SetEnumEntriesFromCurrentExtEnum();
    }
    public void SetEnumEntriesFromCurrentExtEnum(bool orderalphabetically = false)
    {
        entriesMap.Clear();
        for (int i = 0; i < enumEntries.entries.Count; i++)
        {
            entriesMap.Add(enumEntries.entries[i], i);
        }
        if (orderalphabetically)
        {
            List<string> sortedList = entriesMap.Keys.ToList();
            sortedList.Sort((x,y) => x.CompareTo(y));
            entriesMap.Clear();
            for (int i = 0; i < sortedList.Count; i++)
            {
                entriesMap.Add(sortedList[i], i);
            }
        }
    }
    public int GetIndex(string value) => entriesMap[value];
    public int GetIndex<T>(T extEnum) where T : ExtEnum<T> => GetIndex(extEnum.value);
    public string? GetValueFromIndex(int index) 
    {
        try
        {
            return entriesMap.First(x => x.Value == index).Key;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public Type enumType {get;}
    public ExtEnumType enumEntries {get;}
    public Dictionary<string, int> entriesMap = [];
    internal string[] storedCompressedValues = [];
    internal byte clarificationAttempt = 0;
    internal const byte Patience = 3; // max clarification attempt

    // Compress the entries into whatever shape you find best
    public abstract string[] GetCompressedEntries();
    // Decompress it and give the result of the decompression. Be sure to add an exception if you find the exact wording (so it doesn't count as an ambiguity or a miss)
    public abstract DecompressionResult ReadAndSyncCompressedEntries(string[] compressedEntries);

    internal void LogMappedExtEnum()
    {
        RainMeadow.Debug($"Logging currently mapped of {enumType.FullName} ExtEnum :");
        List<string> sortedList = entriesMap.Keys.ToList();
        sortedList.Sort((x,y) => entriesMap[x] - entriesMap[y]);
        for (int i = 0; i < sortedList.Count; i++)
        {
            RainMeadow.Debug($"   > <{entriesMap[sortedList[i]]}> {sortedList[i]}");
        }
    }
    internal void LogCompressionTest(bool logList = false)
    {
        RainMeadow.Debug($"Starting compression test of {enumType.FullName} ExtEnum with {this.GetType().FullName} method...");
        List<string> unsortedStart = entriesMap.Keys.ToList();
        unsortedStart.Sort((x,y) => entriesMap[x] - entriesMap[y]);
        string[] start = unsortedStart.ToArray();
        string[] end = GetCompressedEntries();

        if (logList)
        {
            RainMeadow.Debug($"Entries of {enumType.FullName} ExtEnum are :");

            for (int i = 0; i < start.Length; i++)
            {
                RainMeadow.Debug($"   > <{entriesMap[start[i]]}> {start[i]}");
            }
            RainMeadow.Debug($"Compression result is :");
            for (int i = 0; i < end.Length; i++)
            {
                RainMeadow.Debug($"   > <{i}> {end[i]}");
            }
        }

        DecompressionResult result = ReadAndSyncCompressedEntries(end);
        if (logList)
        {
            string[] endResult = entriesMap.Keys.ToArray();
            RainMeadow.Debug($"End result is  :");
            for (int i = 0; i < endResult.Length; i++)
            {
                RainMeadow.Debug($"   > <{entriesMap[endResult[i]]}> {endResult[i]}");
            }
        }

        if (result.IsOK)
        {
            int startLenght = string.Concat(start).Length;
            int endLenght = string.Concat(end).Length;
            RainMeadow.Debug($"Compression and decompression test successful !");
            RainMeadow.Debug($" - Compressed {start.Length} entries");
            RainMeadow.Debug($" - Started with {startLenght} character reduced to {endLenght} characters");
            RainMeadow.Debug($"   => That's around a {100 - (int)(100f * endLenght/startLenght)}% compression rate !");
        }
        else
        {
            RainMeadow.Debug($"Error while testing compression : Found {result.MissingExtEnum.Length} missing enum(s) and {result.AmbiguousExtEnum.Length} ambiguous enum(s) somehow.");
            RainMeadow.Debug($"Missing enums are :");
            for (int i = 0; i < result.MissingExtEnum.Length; i++)
            {
                RainMeadow.Debug($"   >{result.MissingExtEnum[i].value}");
            }
            RainMeadow.Debug($"Ambiguous enums is :");
            for (int i = 0; i < result.AmbiguousExtEnum.Length; i++)
            {
                RainMeadow.Debug($"   >{result.AmbiguousExtEnum[i].value}");
            }
        }        
    }
    
    public class DecompressionResult(ExtEnumEntry[] missingExtEnum, ExtEnumEntry[] ambiguousExtEnum) : Serializer.ICustomSerializable
    {
        public DecompressionResult() : this([], new ExtEnumEntry[0]) {}
        public DecompressionResult(List<ExtEnumEntry> missingExtEnum, List<ExtEnumEntry> ambiguousExtEnum) 
            : this(missingExtEnum.ToArray(), ambiguousExtEnum.ToArray()) {}

        private byte MissingExtEnumSize = (byte)missingExtEnum.Length;
        public ExtEnumEntry[] MissingExtEnum { get; private set; } = missingExtEnum;
        private byte AmbiguousExtEnumSize = (byte)ambiguousExtEnum.Length;
        public ExtEnumEntry[] AmbiguousExtEnum { get; private set; } = ambiguousExtEnum;
        public bool IsOK { get; private set; } = missingExtEnum.Length == 0 && ambiguousExtEnum.Length == 0;

        public void CustomSerialize(Serializer serializer)
        {
            if (serializer.IsWriting)
            {  
                serializer.Serialize(ref this.MissingExtEnumSize);
                for (int i = 0; i < this.MissingExtEnumSize; i++)
                {
                    serializer.Serialize(ref this.MissingExtEnum[i].position);
                    serializer.Serialize(ref this.MissingExtEnum[i].value);
                }
                
                serializer.Serialize(ref this.AmbiguousExtEnumSize);
                for (int i = 0; i < this.AmbiguousExtEnumSize; i++)
                {
                    serializer.Serialize(ref this.AmbiguousExtEnum[i].position);
                    serializer.Serialize(ref this.AmbiguousExtEnum[i].value);
                }
            }
            else if (serializer.IsReading)
            {
                serializer.Serialize(ref this.MissingExtEnumSize);
                MissingExtEnum = new ExtEnumEntry[this.MissingExtEnumSize];
                for (int i = 0; i < this.MissingExtEnumSize; i++)
                {
                    ExtEnumEntry data = new("", 0);
                    serializer.Serialize(ref data.position);
                    serializer.Serialize(ref data.value);
                    MissingExtEnum[i] = data;
                }
                
                serializer.Serialize(ref this.AmbiguousExtEnumSize);
                MissingExtEnum = new ExtEnumEntry[this.AmbiguousExtEnumSize];
                for (int i = 0; i < this.AmbiguousExtEnumSize; i++)
                {
                    ExtEnumEntry data = new("", 0);
                    serializer.Serialize(ref data.position);
                    serializer.Serialize(ref data.value);
                    AmbiguousExtEnum[i] = data;
                }

                this.IsOK = this.MissingExtEnum.Length == 0 && this.AmbiguousExtEnum.Length == 0;
            }
        }
    }
}

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
    public override string[] GetCompressedEntries()
    {
        List<ExtEnumEntry> sortedValues = ExtEnumEntry.ToExtEnumEntryList(this.entriesMap);
        if (sortedValues.Count == 0) return [];

        sortedValues.Sort((x, y) => x.value.CompareTo(y.value));
        
        return ExtEnumEntry.ExtEnumEntryToArray(Compression(sortedValues));
    }
    public override DecompressionResult ReadAndSyncCompressedEntries(string[] compressedEntries)
    {
        List<string> oldEntries = this.entriesMap.Keys.ToList();
        List<ExtEnumEntry> missingExtEnum = [], ambiguousExtEnum = [];
        Dictionary<string, int> newEntries = [];
        for (int i = 0; i < compressedEntries.Length; i++)
        {
            List<string> search = oldEntries.FindAll(x => DoesStringMatchCompressed(x, compressedEntries[i]));
            if (search.Count == 1)
            {
                newEntries.Add(search[0], i);
            }
            else if (search.Count == 0)
            {
                RainMeadow.Debug($"Found missing enum of {enumType.FullName} : {compressedEntries[i]}");
                missingExtEnum.Add(new(compressedEntries[i], i));
            }
            else
            {
                int exactFindIndex = search.FindIndex(x => x  == compressedEntries[i]);
                if (exactFindIndex == -1)
                {
                    RainMeadow.Debug($"Found {search.Count} ambiguous enum of {enumType.FullName} : {compressedEntries[i]}");
                    ambiguousExtEnum.Add(new(compressedEntries[i], i));
                }
                else
                {
                    newEntries.Add(search[exactFindIndex], i);
                }
            }
        }

        for (int i = 0; i < oldEntries.Count; i++)
        {
            if (!newEntries.Keys.Contains(oldEntries[i]))
            {
                newEntries.Add(oldEntries[i], newEntries.Count);
            }
        }

        DecompressionResult result = new(missingExtEnum, ambiguousExtEnum);
        if (result.IsOK) { this.entriesMap = newEntries; }
        return result;
    }
}
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
            // RainMeadow.Debug($"We have for size {length} :");
            // for (int j = 0; j < cuttedList[i].Count; j++)
            // {
            //     RainMeadow.Debug($"   > {cuttedList[i][j].value}");
            // }

            cuttedList[i].Sort((x, y) => x.value.CompareTo(y.value));
            cuttedList[i] = FirstLetterCompressedExtEnum.Compression(cuttedList[i]);

            // RainMeadow.Debug($"Turned into :");
            for (int j = 0; j < cuttedList[i].Count; j++)
            {
                cuttedList[i][j].value = (char)length + cuttedList[i][j].value;
                // RainMeadow.Debug($"   > {cuttedList[i][j].value}");
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
        
        // if (FirstLetterCompressedExtEnum.DoesStringMatchCompressed(value, compressedValue)) { RainMeadow.Debug($"Matching {value} and <{size}> {compressedValue} ({compressedValueNSize})"); }
        return FirstLetterCompressedExtEnum.DoesStringMatchCompressed(value, compressedValue);
    }
    public override string[] GetCompressedEntries()
    {
        return ExtEnumEntry.ExtEnumEntryToArray(Compression(ExtEnumEntry.ToExtEnumEntryList(this.entriesMap)));
    }

    public override DecompressionResult ReadAndSyncCompressedEntries(string[] compressedEntries)
    {
        List<string> oldEntries = this.entriesMap.Keys.ToList();
        List<ExtEnumEntry> missingExtEnum = [], ambiguousExtEnum = [];
        Dictionary<string, int> newEntries = [];

        for (int i = 0; i < compressedEntries.Length; i++)
        {
            List<string> search = oldEntries.FindAll(x => DoesStringMatchCompressed(x, compressedEntries[i]));
            if (search.Count == 1)
            {
                newEntries.Add(search[0], i);
            }
            else if (search.Count == 0)
            {
                RainMeadow.Debug($"Found missing enum of {enumType.FullName} : {compressedEntries[i]}");
                missingExtEnum.Add(new(compressedEntries[i], i));
            }
            else
            {
                int exactFindIndex = search.FindIndex(x => x  == compressedEntries[i]);
                if (exactFindIndex > -1)
                {
                    newEntries.Add(search[exactFindIndex], i);
                }
                else
                {
                    int compressedExactFindIndex = search.FindIndex(x => x  == compressedEntries[i].Substring(1));
                    if (compressedExactFindIndex > -1)
                    {
                        newEntries.Add(search[exactFindIndex], i);
                    }
                    else
                    {
                        RainMeadow.Debug($"Found {search.Count} ambiguous enum of {enumType.FullName} : {compressedEntries[i]}");
                        ambiguousExtEnum.Add(new(compressedEntries[i], i));
                    }
                }
            }
        }

        for (int i = 0; i < oldEntries.Count; i++)
        {
            if (!newEntries.Keys.Contains(oldEntries[i]))
            {
                newEntries.Add(oldEntries[i], newEntries.Count);
            }
        }

        DecompressionResult result = new(missingExtEnum, ambiguousExtEnum);
        if (result.IsOK) { this.entriesMap = newEntries; }
        return result;
    }
}

// compress by sorting by amount of separator, then compressing the different prefixes by extra letters, then compressing the last suffix by extra letter and size
public class SeparatorCompressedExtEnum(Type enumType, char separator) : CompressedExtEnumBase(enumType)
{
    public char separator {get;} = separator;
    private static List<ExtEnumEntry> RecursiveCut(List<ExtEnumEntry> arrangedValues, char separator)
    {
        if (arrangedValues.Count == 0) return [];

        // Assuming that they have the same amount of separator
        arrangedValues.Sort((x, y) => x.value.Split(separator).First().CompareTo(y.value.Split(separator).First()));
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
    
    public override string[] GetCompressedEntries()
    {
        return ExtEnumEntry.ExtEnumEntryToArray(Compression(ExtEnumEntry.ToExtEnumEntryList(this.entriesMap), separator));
    }
    public override DecompressionResult ReadAndSyncCompressedEntries(string[] compressedEntries)
    {
        List<string> oldEntries = this.entriesMap.Keys.ToList();
        List<ExtEnumEntry> missingExtEnum = [], ambiguousExtEnum = [];
        Dictionary<string, int> newEntries = [];

        for (int i = 0; i < compressedEntries.Length; i++)
        {
            List<string> search = oldEntries.FindAll(x => DoesStringMatchCompressed(x, compressedEntries[i], separator));
            if (search.Count == 1)
            {
                newEntries.Add(search[0], i);
            }
            else if (search.Count == 0)
            {
                RainMeadow.Debug($"Found missing enum of {enumType.FullName} : {compressedEntries[i]}");
                missingExtEnum.Add(new(compressedEntries[i], i));
            }
            else
            {
                int exactFindIndex = search.FindIndex(x => x  == compressedEntries[i]);
                if (exactFindIndex > -1)
                {
                    newEntries.Add(search[exactFindIndex], i);
                }
                else
                {
                    int compressedExactFindIndex = search.FindIndex(x => x.Split(separator).Last() == compressedEntries[i].Split(separator).Last().Substring(1));
                    if (compressedExactFindIndex > -1)
                    {
                        newEntries.Add(search[exactFindIndex], i);
                    }
                    else
                    {
                        RainMeadow.Debug($"Found {search.Count} ambiguous enum of {enumType.FullName} : {compressedEntries[i]}");
                        ambiguousExtEnum.Add(new(compressedEntries[i], i));
                    }
                }
            }
        }

        for (int i = 0; i < oldEntries.Count; i++)
        {
            if (!newEntries.Keys.Contains(oldEntries[i]))
            {
                newEntries.Add(oldEntries[i], newEntries.Count);
            }
        }

        DecompressionResult result = new(missingExtEnum, ambiguousExtEnum);
        if (result.IsOK) { this.entriesMap = newEntries; }
        return result;
    }
}

public static class MeadowExtEnumSync
{
    // --------------------- Hooks
    internal static void ApplyHooks()
    {
        MatchmakingManager.OnLobbyJoined += MatchmakingManager_BTWVersionChecker_OnLobbyJoined;
    }
    private static void MatchmakingManager_BTWVersionChecker_OnLobbyJoined(bool ok, string error)
    {
        if (ok)
        {
            if (OnlineManager.lobby is not null)
            {
                for (int i = 0; i < SyncedExtEnumList.Count; i++)
                {
                    SyncedExtEnumList[i].SetEnumEntriesFromCurrentExtEnum(true); // ordering them alphabetically to reduce ordeer mismatch chances
                }
                if (OnlineManager.lobby.isOwner)
                {
                    // Uh have fun ig ? Not much to do there
                    // Swap2EnumForTestingPurposes(100); shuffling is a bad idea yeah.
                    LogTestCompression();
                }
                else
                {
                    OnlineManager.lobby.owner?.InvokeRPC(RequestCompressedExtEnums);
                }
            }
        }
    }

    // --------------------- Methods and Attributes

    // I don't want to assume that the order of SyncedExtEnumList is the same. I'll leave it public if some mods want to sync more enums here.
    // Also, having it static initialized is no biggies ! We take back the values later.
    public static List<CompressedExtEnumBase> SyncedExtEnumList = new()
    {
        new FirstLetterCompressedExtEnum(typeof(SlugcatStats.Name)),
        new SizeAndFirstLetterCompressedExtEnum(typeof(AbstractPhysicalObject.AbstractObjectType)),
        new SizeAndFirstLetterCompressedExtEnum(typeof(CreatureTemplate.Type)),
        new SeparatorCompressedExtEnum(typeof(OnlineState.StateType), '.'),
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

    public static bool IsSyncedExtEnum(Type enumType, out CompressedExtEnumBase? compressedExtEnum)
    {
        compressedExtEnum = null;
        int i = SyncedExtEnumList.FindIndex(x => x.enumType == enumType);
        if (i > -1)
        {
            compressedExtEnum = SyncedExtEnumList[i];
            return true;
        }
        return false;
    }

    public static byte MeadowIndex<T>(this T extEnum) where T : ExtEnum<T>
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
    public static string GetExtEnumValue<T>(byte index) where T : ExtEnum<T>
    {
        string? entry = null;
        if (OnlineManager.lobby is not null && IsSyncedExtEnum(typeof(T), out var compressedExtEnum))
        {
            entry = compressedExtEnum.GetValueFromIndex(index);
        }
        return entry is null ? ExtEnum<T>.values.GetEntry(index) : entry;
    }

    // TODO : find a better place for this
    public static bool IsDictionary(this Type type, out Type? dictInterface)
    {
        dictInterface = null;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            dictInterface = type;
            return true;
        }
        else
        {
            foreach (var i in type.GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    dictInterface = i;
                    return true;
                }
            }
        }

        return false;
    }

    // --------------------- Tests and logs
    
    internal static void LogTestCompression()
    {
        for (int i = 0; i < SyncedExtEnumList.Count; i++)
        {
            SyncedExtEnumList[i].LogCompressionTest(i == 0);
        }
    }

    // This swaps the base enum list ! (not permanently, it's recent after every restart)
    // Used only for testing purposes
    private static void Swap2EnumForTestingPurposes(int swaps = 1)
    {
        RainMeadow.Debug($"Swapping two enum order (x{swaps}) for testing purposes...");
        for (int s = 1; s <= swaps; s++)
        {
            try
            {
                int en = UnityEngine.Random.Range(0, SyncedExtEnumList.Count);
                int i = UnityEngine.Random.Range(0, SyncedExtEnumList[en].entriesMap.Count);
                int j = UnityEngine.Random.Range(0, SyncedExtEnumList[en].entriesMap.Count);

                (SyncedExtEnumList[en].entriesMap[SyncedExtEnumList[en].entriesMap.ElementAt(j).Key], SyncedExtEnumList[en].entriesMap[SyncedExtEnumList[en].entriesMap.ElementAt(i).Key]) 
                    = (SyncedExtEnumList[en].entriesMap[SyncedExtEnumList[en].entriesMap.ElementAt(i).Key], SyncedExtEnumList[en].entriesMap[SyncedExtEnumList[en].entriesMap.ElementAt(j).Key]);
                RainMeadow.Debug($"Swapped <{SyncedExtEnumList[en].entriesMap[SyncedExtEnumList[en].entriesMap.ElementAt(i).Key]}>[{SyncedExtEnumList[en].entriesMap.ElementAt(i).Key}] and <{SyncedExtEnumList[en].entriesMap[SyncedExtEnumList[en].entriesMap.ElementAt(j).Key]}>[{SyncedExtEnumList[en].entriesMap.ElementAt(j).Key}] of enum [{SyncedExtEnumList[en].enumType.FullName}]");
            }
            catch (Exception ex)
            {
                RainMeadow.Debug("Error while changing the order of the enums ! " + ex);
            }
        }
        RainMeadow.Debug($"Done without issues ! For now...");
    }
    
    // --------------------- RPCS

    // Quite a heavy RPC, even with the compression, I hope this isn't too much of an issue.
    // Heh, this is for the greater good.
    [RPCMethod]
    public static void RequestCompressedExtEnums(RPCEvent rpc)
    {
        if (OnlineManager.lobby is null || OnlineManager.mePlayer != OnlineManager.lobby.owner) { return; }

        Dictionary<string, string> compressedExtEnumTable = [];
        for (int i = 0; i < SyncedExtEnumList.Count; i++)
        {
            compressedExtEnumTable.Add(
                SyncedExtEnumList[i].enumType.FullName, 
                CompressedExtEnumArrayToString(SyncedExtEnumList[i].GetCompressedEntries())
            );
        }
        rpc.from.InvokeRPC(SendToSyncCompressedExtEnums, compressedExtEnumTable);
    }
    [RPCMethod]
    public static void SendToSyncCompressedExtEnums(RPCEvent rpc, Dictionary<string, string> compressedExtEnumTable)
    {
        if (OnlineManager.lobby is null || rpc.from != OnlineManager.lobby.owner) { return; }
        foreach (var compressedExtEnumKeyPair in compressedExtEnumTable)
        {
            int i = SyncedExtEnumList.FindIndex(x => x.enumType.FullName == compressedExtEnumKeyPair.Key);
            if (i > -1)
            {
                string[] compressedExtEnum = CompressedExtEnumStringToArray(compressedExtEnumKeyPair.Value);
                // SyncedExtEnumList[i].LogMappedExtEnum();
                CompressedExtEnumBase.DecompressionResult result = SyncedExtEnumList[i].ReadAndSyncCompressedEntries(compressedExtEnum);
                RainMeadow.Debug($"Read and Synced ExtEnum {compressedExtEnumKeyPair.Key} of host ! Missing enums : {result.MissingExtEnum.Length}, Ambiguous enums : {result.AmbiguousExtEnum.Length}, Status OK ? {result.IsOK}");
                // SyncedExtEnumList[i].LogMappedExtEnum();
                if (!result.IsOK)
                {
                    SyncedExtEnumList[i].storedCompressedValues = compressedExtEnum;
                    SyncedExtEnumList[i].clarificationAttempt = 0;
                    rpc.from.InvokeRPC(AskFromClarification, compressedExtEnumKeyPair.Key, result);
                }
            }
        }
    }
    [RPCMethod] 
    public static void AskFromClarification(RPCEvent rpc, string extEnumName, CompressedExtEnumBase.DecompressionResult resultErrors)
    {
        if (OnlineManager.lobby is null || OnlineManager.mePlayer != OnlineManager.lobby.owner || resultErrors.IsOK) { return; }
        int i = SyncedExtEnumList.FindIndex(x => x.enumType.FullName == extEnumName);
        if (i > -1)
        {
            ExtEnumEntry[] clarifiedMissingExtEnum = new ExtEnumEntry[resultErrors.MissingExtEnum.Length];
            ExtEnumEntry[] clarifiedAmbiguousExtEnum = new ExtEnumEntry[resultErrors.AmbiguousExtEnum.Length];

            // there we can directly assume that the index matches, since... you were the one sending it a few ticks ago
            for (int j = 0; j < resultErrors.MissingExtEnum.Length; j++)
            {
                clarifiedMissingExtEnum[j] = new(
                    SyncedExtEnumList[i].entriesMap[resultErrors.MissingExtEnum[j].value], 
                    resultErrors.MissingExtEnum[j].value
                );
            }
            for (int j = 0; j < resultErrors.AmbiguousExtEnum.Length; j++)
            {
                clarifiedAmbiguousExtEnum[j] = new(
                    SyncedExtEnumList[i].entriesMap[resultErrors.AmbiguousExtEnum[j].value], 
                    resultErrors.AmbiguousExtEnum[j].value
                );
            }            
            
            CompressedExtEnumBase.DecompressionResult result = new(clarifiedMissingExtEnum, clarifiedAmbiguousExtEnum);
            rpc.from.InvokeRPC(SendToSyncClarification, extEnumName, result);
        }
    }
    [RPCMethod] 
    public static void SendToSyncClarification(RPCEvent rpc, string extEnumName, CompressedExtEnumBase.DecompressionResult resultClarification)
    {
        if (OnlineManager.lobby is null || rpc.from != OnlineManager.lobby.owner) { return; }
        int i = SyncedExtEnumList.FindIndex(x => x.enumType.FullName == extEnumName);
        if (i > -1)
        {
            if (SyncedExtEnumList[i].storedCompressedValues.Length == 0) { return; } // you never asked for clarification, cmon, you know what you were doing !
            
            for (int j = 0; j < resultClarification.MissingExtEnum.Length; j++)
            {
                // This is technically creating enums if some are missings ? That's such a rare case, I don't think it'd cause error anyway.
                SyncedExtEnumList[i].entriesMap.Add(resultClarification.MissingExtEnum[j].value, SyncedExtEnumList[i].entriesMap.Count);
            }
            for (int j = 0; j < resultClarification.AmbiguousExtEnum.Length; j++)
            {
                // Putting the exact value so it's more clear :D
                SyncedExtEnumList[i].storedCompressedValues[resultClarification.AmbiguousExtEnum[j].position] = resultClarification.AmbiguousExtEnum[j].value;
            }

            CompressedExtEnumBase.DecompressionResult result = SyncedExtEnumList[i].ReadAndSyncCompressedEntries(SyncedExtEnumList[i].storedCompressedValues);
            RainMeadow.Debug($"Read and Synced ExtEnum {extEnumName} of host again..! Attemps : {SyncedExtEnumList[i].clarificationAttempt + 1}, Missing enums : {result.MissingExtEnum.Length}, Ambiguous enums : {result.AmbiguousExtEnum.Length}, Status OK ? {result.IsOK}");
            if (!result.IsOK)
            {
                // We don't want this to run indefinetly !
                SyncedExtEnumList[i].clarificationAttempt++;
                if (SyncedExtEnumList[i].clarificationAttempt >= CompressedExtEnumBase.Patience)
                {
                    RainMeadow.Error($"Tried to clarify {extEnumName} more than {CompressedExtEnumBase.Patience} times ! Not syncing enum.");
                }
                else
                {
                    rpc.from.InvokeRPC(AskFromClarification, extEnumName, result);
                    return;
                }
            }
            SyncedExtEnumList[i].storedCompressedValues = [];
        }
    }

}