using System.Collections.Generic;

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