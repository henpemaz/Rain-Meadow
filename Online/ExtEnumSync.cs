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
    public bool IsLongTable => entriesMap.Count >= byte.MaxValue + 1; // TODO : maybe add something to allow 256+ items enums to be synced ?

    // Compress the entries into whatever shape you find best
    public abstract string[] GetCompressedEntries();
    // Decompress it and give the result of the decompression. Use ProcessCompression for a standarized process
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
    
    /* 
        Match compressed strings and returns the decompression result. 
        "matchingAlgorithm" and "ambiguousAlgorithm" both take a candidate (a string that may bethe value) and a compressed value. They return true if it match.
        "matchingAlgorithm" should match any string that could be compressed into the string given as second argument.
        "ambiguousAlgorithm" should be able to get ONE result from a bunch of candidate from "matchingAlgorithm". Mainly used from edge case or exact matching.
        This function can recognize exact match found by "matchingAlgorithm".
    */
    protected DecompressionResult ProcessCompression(string[] compressedEntries, Func<string, string, bool> matchingAlgorithm, Func<string, string, bool>? ambiguousAlgorithm = null)
    {
        List<string> oldEntries = this.entriesMap.Keys.ToList();
        List<ExtEnumEntry> missingExtEnum = [], ambiguousExtEnum = [], additionnalEnum = [];
        Dictionary<string, int> newEntries = [];

        for (int i = 0; i < compressedEntries.Length; i++)
        {
            List<string> search = oldEntries.FindAll(x => matchingAlgorithm(x, compressedEntries[i]));
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
                    int compressedExactFindIndex = ambiguousAlgorithm is not null ? search.FindIndex(x => ambiguousAlgorithm(x, compressedEntries[i])) : -1;
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
                RainMeadow.Debug($"Found additionnal enum of {enumType.FullName} : {oldEntries[i]}. Assigning it place {newEntries.Count}.");
                additionnalEnum.Add(new(oldEntries[i], newEntries.Count));
                newEntries.Add(oldEntries[i], newEntries.Count);
            }
        }

        DecompressionResult result = new(missingExtEnum, ambiguousExtEnum, additionnalEnum, enumType.FullName);
        if (result.IsOK) { this.entriesMap = newEntries; }
        return result;
    }

    public class DecompressionResult(ExtEnumEntry[] missingExtEnum, ExtEnumEntry[] ambiguousExtEnum, ExtEnumEntry[] additionnalExtEnum, string typeFullName) : Serializer.ICustomSerializable
    {
        public DecompressionResult()
             : this([], new ExtEnumEntry[0], "wawa") {}
        public DecompressionResult(ExtEnumEntry[] missingExtEnum, ExtEnumEntry[] ambiguousExtEnum, string typeFullName)
             : this(missingExtEnum, ambiguousExtEnum, [], typeFullName) {}
        public DecompressionResult(List<ExtEnumEntry> missingExtEnum, List<ExtEnumEntry> ambiguousExtEnum, List<ExtEnumEntry> additionnalExtEnum, string typeFullName) 
            : this(missingExtEnum.ToArray(), ambiguousExtEnum.ToArray(), additionnalExtEnum.ToArray(), typeFullName) {}
        public DecompressionResult(List<ExtEnumEntry> missingExtEnum, List<ExtEnumEntry> ambiguousExtEnum, string typeFullName) 
            : this(missingExtEnum.ToArray(), ambiguousExtEnum.ToArray(), typeFullName) {}

        // This part need to be synced for clarification
        public string TypeFullName = typeFullName;
        public ExtEnumEntry[] MissingExtEnum { get; private set; } = missingExtEnum;
        public ExtEnumEntry[] AmbiguousExtEnum { get; private set; } = ambiguousExtEnum;
        
        // This part doesn't need to be synced at all
        public ExtEnumEntry[] AdditionnalExtEnum { get; private set; } = additionnalExtEnum;
        public bool IsOK { get; private set; } = missingExtEnum.Length == 0 && ambiguousExtEnum.Length == 0;

        public void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref this.TypeFullName);
            if (serializer.IsWriting)
            {  
                byte tabSize = (byte)this.MissingExtEnum.Length;
                serializer.Serialize(ref tabSize);
                for (int i = 0; i < tabSize; i++)
                {
                    serializer.Serialize(ref this.MissingExtEnum[i].position);
                    serializer.Serialize(ref this.MissingExtEnum[i].value);
                }
                
                tabSize = (byte)this.AmbiguousExtEnum.Length;
                serializer.Serialize(ref tabSize);
                for (int i = 0; i < tabSize; i++)
                {
                    serializer.Serialize(ref this.AmbiguousExtEnum[i].position);
                    serializer.Serialize(ref this.AmbiguousExtEnum[i].value);
                }
            }
            else if (serializer.IsReading)
            {
                byte tabSize = 0;
                serializer.Serialize(ref tabSize);
                MissingExtEnum = new ExtEnumEntry[tabSize];
                for (int i = 0; i < tabSize; i++)
                {
                    ExtEnumEntry data = new("", 0);
                    serializer.Serialize(ref data.position);
                    serializer.Serialize(ref data.value);
                    MissingExtEnum[i] = data;
                }
                
                serializer.Serialize(ref tabSize);
                AmbiguousExtEnum = new ExtEnumEntry[tabSize];
                for (int i = 0; i < tabSize; i++)
                {
                    ExtEnumEntry data = new("", 0);
                    serializer.Serialize(ref data.position);
                    serializer.Serialize(ref data.value);
                    AmbiguousExtEnum[i] = data;
                }

                this.IsOK = this.MissingExtEnum.Length == 0 && this.AmbiguousExtEnum.Length == 0;
            }
            // RainMeadow.Debug($"Serialized compression result of enum {this.TypeFullName}, missing enums : {this.MissingExtEnum.Length}, ambiguous enums : {this.AmbiguousExtEnum.Length}. Reading ? {serializer.IsReading}");
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
        return ProcessCompression(compressedEntries, 
            (x,y) => DoesStringMatchCompressed(x, y));
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
        return ProcessCompression(compressedEntries, 
            (x,y) => DoesStringMatchCompressed(x, y), 
            (x,y) => x == y.Substring(1));
    }
}

// compress by sorting by amount of separator, then compressing the different prefixes by extra letters, then compressing the last suffix by extra letter and size
public class SeparatorCompressedExtEnum(Type enumType, char separator) : CompressedExtEnumBase(enumType)
{
    public char separator {get;} = separator;
    private static List<ExtEnumEntry> RecursiveCut(List<ExtEnumEntry> arrangedValues, char separator)
    {
        if (arrangedValues.Count == 0) return [];

        // RainMeadow.Debug("Logging list mid-compression");
        // for (int j = 0; j < arrangedValues.Count; j++)
        // {
        //     RainMeadow.Debug($"   > <{arrangedValues[j].position}> {arrangedValues[j].value}");
        // }

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

        // RainMeadow.Debug("To :");
        // for (int j = 0; j < arrangedValues.Count; j++)
        // {
        //     RainMeadow.Debug($"   > <{arrangedValues[j].position}> {arrangedValues[j].value}");
        // }

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
        return ProcessCompression(compressedEntries, 
            (x,y) => DoesStringMatchCompressed(x, y, separator), 
            (x,y) => x.Split(separator).Last() == y.Split(separator).Last().Substring(1));
    }
}

public static class MeadowExtEnumSync
{
    public static void ResetEnumEntriesMapping()
    {
        for (int i = 0; i < SyncedExtEnumList.Count; i++)
        {
            // ordering them alphabetically to reduce order mismatch chances
            SyncedExtEnumList[i].SetEnumEntriesFromCurrentExtEnum(true); 
            // SyncedExtEnumList[i].LogMappedExtEnum();
        }
        RainMeadow.Debug($"Enum entries map reset for <{SyncedExtEnumList.Count}> enums : [{string.Join(", ", SyncedExtEnumList.Select(x => x.enumType.FullName))}]");
    }

    // --------------------- Methods and Attributes

    // I don't want to assume that the order of SyncedExtEnumList is the same. I'll leave it public if some mods want to sync more enums here.
    // Also, having it static initialized is no biggies ! We'll assign the values later.
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

    // --------------------- Tests and logs
    
    internal static void LogTestCompression()
    {
        for (int i = 0; i < SyncedExtEnumList.Count; i++)
        {
            SyncedExtEnumList[i].LogCompressionTest(i == 0);
        }
    }
    
    // --------------------- RPCS

    // Quite heavy RPCs, even with the compression, I hope this isn't too much of an issue.
    // Heh, this is for the greater good.

    [RPCMethod(security = RPCSecurity.NoSecurity)] // Asking the owner for the compressed list of the enums. Client -> Owner
    public static void RequestCompressedExtEnums(RPCEvent request)
    {
        if (OnlineManager.lobby is null || OnlineManager.mePlayer != OnlineManager.lobby.owner) 
        { 
            RainMeadow.Error($"False request of enums : {(OnlineManager.lobby is null ? "Lobby is null" : "I am not the owner")} !");
            request.from.QueueEvent(new GenericResult.Fail(request));
            return; 
        }

        Dictionary<string, string> compressedExtEnumTable = [];
        try
        {
            for (int i = 0; i < SyncedExtEnumList.Count; i++)
            {
                compressedExtEnumTable.Add(
                    SyncedExtEnumList[i].enumType.FullName, 
                    CompressedExtEnumArrayToString(SyncedExtEnumList[i].GetCompressedEntries())
                );
            }
            // foreach (var compressedstuff in compressedExtEnumTable)
            // {
            //     RainMeadow.Debug("Logging compressed set of "+ compressedstuff.Key);
            //     string[] listofcompressedstuff = CompressedExtEnumStringToArray(compressedstuff.Value);
            //     for (int j = 0; j < listofcompressedstuff.Length; j++)
            //     {
            //         RainMeadow.Debug($"   > <{j}> {listofcompressedstuff[j]}");
            //     }
            // }
        }
        catch (System.Exception er)
        {
            RainMeadow.Error("Failed to send compressed enums : " + er);
            request.from.QueueEvent(new GenericResult.Error(request));
            return;
        }
        request.from.QueueEvent(new GenericResult.Ok(request));
        request.from.InvokeRPC(SendToSyncCompressedExtEnums, compressedExtEnumTable);
    }
    [RPCMethod(security = RPCSecurity.NoSecurity)] // Checking and syncing the enums recieved, also ask for clarification if necessary. Owner -> Client
    public static void SendToSyncCompressedExtEnums(RPCEvent rpc, Dictionary<string, string> compressedExtEnumTable)
    {
        if (OnlineManager.lobby is null || rpc.from != OnlineManager.lobby.owner || OnlineManager.lobby.enumsChecked) { return; }

        List<CompressedExtEnumBase.DecompressionResult> clarificationTable = [];

        foreach (var compressedExtEnumKeyPair in compressedExtEnumTable)
        {
            int i = SyncedExtEnumList.FindIndex(x => x.enumType.FullName == compressedExtEnumKeyPair.Key);
            if (i > -1)
            {
                string[] compressedExtEnum = CompressedExtEnumStringToArray(compressedExtEnumKeyPair.Value);
                CompressedExtEnumBase.DecompressionResult result = SyncedExtEnumList[i].ReadAndSyncCompressedEntries(compressedExtEnum);
                RainMeadow.Debug($"Read and Synced ExtEnum {compressedExtEnumKeyPair.Key} of host ! Missing enums : {result.MissingExtEnum.Length}, Ambiguous enums : {result.AmbiguousExtEnum.Length}, Extra enums : {result.AdditionnalExtEnum.Length}, Status OK ? {result.IsOK}");
                if (!result.IsOK)
                {
                    SyncedExtEnumList[i].storedCompressedValues = compressedExtEnum;
                    SyncedExtEnumList[i].clarificationAttempt = 0;
                    clarificationTable.Add(result);
                }
            }
        }

        if (clarificationTable.Count > 0)
        {
            RainMeadow.Debug($"Asking clarification for {clarificationTable.Count} enums : [{string.Join(", ", clarificationTable.Select(x => x.TypeFullName))}]");
            rpc.from.InvokeRPC(AskFromClarification, clarificationTable);
        }
        else
        {
            OnlineManager.lobby.OnEnumSyncSuccessful();
        }
    }
    [RPCMethod(security = RPCSecurity.NoSecurity)] // Asking the owner for clarification on some enums in their compressed form. The owner will send them back in full. Client -> Owner
    public static void AskFromClarification(RPCEvent rpc, List<CompressedExtEnumBase.DecompressionResult> clarificationTable)
    {
        if (OnlineManager.lobby is null || OnlineManager.mePlayer != OnlineManager.lobby.owner) { return; }

        List<CompressedExtEnumBase.DecompressionResult> thingsThatShoubldBeClearerTable = [];

        foreach (var resultErrors in clarificationTable)
        {
            int i = SyncedExtEnumList.FindIndex(x => x.enumType.FullName == resultErrors.TypeFullName);
            if (i > -1)
            {
                ExtEnumEntry[] clarifiedMissingExtEnum = new ExtEnumEntry[resultErrors.MissingExtEnum.Length];
                ExtEnumEntry[] clarifiedAmbiguousExtEnum = new ExtEnumEntry[resultErrors.AmbiguousExtEnum.Length];

                // there we can directly assume that the index matches, since... you were the one sending it a few ticks ago
                for (int j = 0; j < resultErrors.MissingExtEnum.Length; j++)
                {
                    clarifiedMissingExtEnum[j] = new(
                        SyncedExtEnumList[i].GetValueFromIndex(resultErrors.MissingExtEnum[j].position), 
                        resultErrors.MissingExtEnum[j].position
                    );
                }
                for (int j = 0; j < resultErrors.AmbiguousExtEnum.Length; j++)
                {
                    clarifiedAmbiguousExtEnum[j] = new(
                        SyncedExtEnumList[i].GetValueFromIndex(resultErrors.AmbiguousExtEnum[j].position), 
                        resultErrors.AmbiguousExtEnum[j].position
                    );
                }            
                
                CompressedExtEnumBase.DecompressionResult result = new(clarifiedMissingExtEnum, clarifiedAmbiguousExtEnum, resultErrors.TypeFullName);
                thingsThatShoubldBeClearerTable.Add(result);
            }
            else
            {
                RainMeadow.Error($"Couldn't find Enum to clarify : {resultErrors.TypeFullName} ! Will ignore it.");
            }
        }
        RainMeadow.Debug($"Sending clarification for {thingsThatShoubldBeClearerTable.Count} enums : [{string.Join(", ", thingsThatShoubldBeClearerTable.Select(x => x.TypeFullName))}]");
        rpc.from.InvokeRPC(SendToSyncClarification, thingsThatShoubldBeClearerTable);
    }
    [RPCMethod(security = RPCSecurity.NoSecurity)]  // Checking and syncing (again) the enums clarified. If everything goes right, the client should have everything clear and done here. Owner -> Client
    public static void SendToSyncClarification(RPCEvent rpc, List<CompressedExtEnumBase.DecompressionResult> thingsThatShoubldBeClearerTable)
    {
        if (OnlineManager.lobby is null || rpc.from != OnlineManager.lobby.owner || OnlineManager.lobby.enumsChecked) { return; }
        List<CompressedExtEnumBase.DecompressionResult> reclarificationTable = [];

        foreach (var resultClarification in thingsThatShoubldBeClearerTable)
        {
            int i = SyncedExtEnumList.FindIndex(x => x.enumType.FullName == resultClarification.TypeFullName);
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
                RainMeadow.Debug($"Read and Synced ExtEnum {resultClarification.TypeFullName} of host again..! Attemps : {SyncedExtEnumList[i].clarificationAttempt + 1}, Missing enums : {result.MissingExtEnum.Length}, Ambiguous enums : {result.AmbiguousExtEnum.Length}, Extra enums : {result.AdditionnalExtEnum.Length}, Status OK ? {result.IsOK}");
                if (!result.IsOK)
                {
                    // We don't want this to run indefinetly !
                    SyncedExtEnumList[i].clarificationAttempt++;
                    if (SyncedExtEnumList[i].clarificationAttempt >= CompressedExtEnumBase.Patience)
                    {
                        RainMeadow.Error($"Tried to clarify {resultClarification.TypeFullName} more than {CompressedExtEnumBase.Patience} times ! Not syncing enum.");
                    }
                    else
                    {
                        reclarificationTable.Add(result);
                        continue;
                    }
                }
                SyncedExtEnumList[i].storedCompressedValues = [];
            }
            else
            {
                RainMeadow.Error($"Couldn't find Enum to sync : {resultClarification.TypeFullName} ! Will ignore it.");
            }
        }

        if (reclarificationTable.Count > 0)
        {
            RainMeadow.Debug($"Asking clarification again for {reclarificationTable.Count} enums : [{string.Join(", ", reclarificationTable.Select(x => x.TypeFullName))}]");
            rpc.from.InvokeRPC(AskFromClarification, reclarificationTable);
        }
        else
        {
            OnlineManager.lobby.OnEnumSyncSuccessful();
        }
    }

}