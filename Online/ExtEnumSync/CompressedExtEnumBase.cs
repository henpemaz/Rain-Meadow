using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow;

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
        ++this.version;
    }
    public bool IsEntryMapped(string entry) => entriesMap.ContainsKey(entry);
    public void AddEntryToMap(string entry, int index = -1)
    {
        if (!IsEntryMapped(entry))
        {
            bool insert = index >= 0 && index <= entriesMap.Count;

            RainMeadow.Debug($"New entry \"{entry}\" added to enum map of {enumType.FullName} at place {(insert ? index : entriesMap.Count)} !");

            if (insert)
            {
                foreach (var extEnumEntry in entriesMap)
                {
                    if (extEnumEntry.Value >= index) entriesMap[extEnumEntry.Key]++;
                }
                entriesMap.Add(entry, index);
            }
            else
            {
                entriesMap.Add(entry, entriesMap.Count);
            }

            LogMappedExtEnum();
            ++this.version;
        }
    }
    public void RemoveEntry(string entry)
    {
        if (entriesMap.TryGetValue(entry, out var index))
        {
            RainMeadow.Debug($"Removing entry \"{entry}\" from enum map of {enumType.FullName} at place {index} !");
            entriesMap.Remove(entry);

            foreach (var extEnumEntry in entriesMap)
            {
                if (extEnumEntry.Value > index) entriesMap[extEnumEntry.Key]--;
            }

            LogMappedExtEnum();
            ++this.version;
        }
    }
    public void RemoveEntry(int index)
    {
        foreach (var extEnumEntry in entriesMap)
        {
            if (extEnumEntry.Value == index)
            {
                RemoveEntry(extEnumEntry.Value);
                return;
            }
        }
    }

    public int GetIndex(string value)
    {
        try
        {
            return entriesMap[value];
        }
        catch (Exception ex)
        {
            RainMeadow.Error($"Could not find value {value} in map ! Was the entry newly added ? Map has {entriesMap.Count} elements while the ExtEnum has {enumEntries.entries.Count}.");
            RainMeadow.Error(ex);
            throw;
        }
    }
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
    public string[] cachedCompressedMap = [];
    internal string[] storedCompressedValues = [];
    internal byte clarificationAttempt = 0;
    internal const byte Patience = 3; // max clarification attempt
    public bool IsLongTable => entriesMap.Count >= byte.MaxValue; // TODO : maybe add something to allow 256+ items enums to be synced ?

    private int _version = 0;
    public int version {get => _version; set{_version = value; OnVersionUpdate();}}
    public virtual void OnVersionUpdate()
    {
        cachedCompressedMap = [];
    }


    // Compress the entries into whatever shape you find best
    protected abstract string[] GetCompressedEntries();
    public string[] GetAndCacheCompressedEntries()
    {
        if (this.cachedCompressedMap.Length <= 0)
        {
            RainMeadow.Debug($"Cached compressed entries of enum {this.enumType.FullName} !");
            this.cachedCompressedMap = this.GetCompressedEntries();
        }
        return this.cachedCompressedMap;
    }

    // Decompress it and give the result of the decompression. Use ProcessCompression for a standarized process
    public abstract DecompressionResult ReadAndSyncCompressedEntries(string[] compressedEntries);

    public void LogMappedExtEnum()
    {
        if (RainMeadow.rainMeadowOptions.CurrentLogLevel.Value > RainMeadow.LogLevel.Debug) return;

        RainMeadow.Debug($"Logging currently mapped of {enumType.FullName} ExtEnum :");
        List<string> sortedList = entriesMap.Keys.ToList();
        sortedList.Sort((x,y) => entriesMap[x] - entriesMap[y]);
        for (int i = 0; i < sortedList.Count; i++)
        {
            RainMeadow.Debug($"   > <{entriesMap[sortedList[i]]}> {sortedList[i]}");
        }
    }
    public void LogCompressionTest(bool logList = false)
    {
        if (RainMeadow.rainMeadowOptions.CurrentLogLevel.Value > RainMeadow.LogLevel.Debug) return;

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
        List<string> ambiguousEnumsEntries = [];

        for (int i = 0; i < compressedEntries.Length; i++)
        {
            List<string> search = oldEntries.FindAll(x => matchingAlgorithm(x, compressedEntries[i]));
            if (search.Count == 1)
            {
                newEntries.Add(search[0], i);
            }
            else if (search.Count == 0)
            {
                int exactFindIndex = oldEntries.FindIndex(x => x == compressedEntries[i]);
                if (exactFindIndex > -1)
                {
                    newEntries.Add(oldEntries[exactFindIndex], i);
                }
                else
                {
                    RainMeadow.Debug($"Found missing enum of {enumType.FullName} : {compressedEntries[i]}");
                    missingExtEnum.Add(new(compressedEntries[i], i));
                }
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
                        RainMeadow.Debug($"Found {search.Count} ambiguous enum of {enumType.FullName} : {compressedEntries[i]}  (could be {string.Join(", ", search)})");
                        ambiguousEnumsEntries.AddRange(search);
                        ambiguousExtEnum.Add(new(compressedEntries[i], i));
                    }
                }
            }
        }

        for (int i = 0; i < oldEntries.Count; i++)
        {
            if (!newEntries.Keys.Contains(oldEntries[i]) && !ambiguousEnumsEntries.Contains(oldEntries[i]))
            {
                RainMeadow.Debug($"Found additionnal enum of {enumType.FullName} : {oldEntries[i]}. Assigning it place {newEntries.Count}.");
                additionnalEnum.Add(new(oldEntries[i], newEntries.Count));
                newEntries.Add(oldEntries[i], newEntries.Count);
            }
        }

        DecompressionResult result = new(missingExtEnum, ambiguousExtEnum, additionnalEnum, enumType.FullName);
        if (result.IsOK) { this.entriesMap = newEntries; ++this.version; }
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