using System;
using System.Collections.Generic;
using RainMeadow.Generics;

namespace RainMeadow
{
    /// <summary>
    /// One player's trophy list as carried by the lobby state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="DynamicKVPList{TKey, TValue, Imp}"/> decides whether an entry changed by calling
    /// <see cref="object.Equals(object)"/> on the value. A bare <see cref="List{T}"/> compares by
    /// reference, so it would report a change on every tick and the "arenaScore" group would never
    /// go quiet. Comparing element-wise here is what lets the group be skipped entirely while
    /// nobody is scoring.
    /// </para>
    /// <para>
    /// The <see cref="IconSymbol.IconSymbolData"/>/string round trip is deliberately confined to
    /// <see cref="CustomSerialize"/>. Both directions are expensive - ToString boxes and formats,
    /// IconSymbolDataFromString does linear ExtEnum lookups - and serialization only runs when the
    /// delta says something actually changed.
    /// </para>
    /// </remarks>
    public class ArenaKillList : Serializer.ICustomSerializable, IEquatable<ArenaKillList>
    {
        public List<IconSymbol.IconSymbolData> kills;

        public ArenaKillList() => kills = [];

        /// <summary>
        /// Wraps <paramref name="kills"/> by reference rather than copying.
        /// </summary>
        /// <remarks>
        /// The caller must replace the list to change it, never mutate it in place. States are
        /// retained as delta baselines, so a list that changes underneath a baseline would make
        /// that baseline indistinguishable from the current value and the change would never be
        /// sent. Arena stat bookkeeping assigns fresh lists, which satisfies this.
        /// </remarks>
        public ArenaKillList(List<IconSymbol.IconSymbolData> kills) => this.kills = kills;

        public bool Equals(ArenaKillList? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other) || ReferenceEquals(kills, other.kills))
                return true;
            if (kills.Count != other.kills.Count)
                return false;

            for (int i = 0; i < kills.Count; i++)
            {
                // IconSymbolData is a struct with no Equals override, so the default
                // ValueType.Equals would fall back to reflection. Compare the fields directly.
                IconSymbol.IconSymbolData mine = kills[i];
                IconSymbol.IconSymbolData theirs = other.kills[i];

                if (mine.critType != theirs.critType
                    || mine.itemType != theirs.itemType
                    || mine.intData != theirs.intData)
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj) => Equals(obj as ArenaKillList);

        public override int GetHashCode() => kills.Count;

        public void CustomSerialize(Serializer serializer)
        {
            if (serializer.IsWriting)
            {
                if (kills.Count > ushort.MaxValue)
                    throw new OverflowException("too many kills");

                serializer.writer.Write((ushort)kills.Count);
                for (int i = 0; i < kills.Count; i++)
                    serializer.writer.Write(kills[i].ToString());
            }
            if (serializer.IsReading)
            {
                ushort count = serializer.reader.ReadUInt16();
                kills = new List<IconSymbol.IconSymbolData>(count);
                for (int i = 0; i < count; i++)
                {
                    kills.Add(
                        IconSymbol.IconSymbolData.IconSymbolDataFromString(
                            serializer.reader.ReadString()
                        )
                    );
                }
            }
        }
    }

    public class UshortToKillListDict : DynamicKVPList<ushort, ArenaKillList, UshortToKillListDict>
    {
        public UshortToKillListDict() { }
        public UshortToKillListDict(List<KeyValuePair<ushort, ArenaKillList>> list) : base(list) { }

        public override void SerializeImpl(Serializer serializer)
        {
            if (serializer.IsWriting)
            {
                if (list.Count > byte.MaxValue)
                    throw new OverflowException("too many players");

                serializer.writer.Write((byte)list.Count);
                foreach (KeyValuePair<ushort, ArenaKillList> kvp in list)
                {
                    serializer.writer.Write(kvp.Key);
                    kvp.Value.CustomSerialize(serializer);
                }
            }
            if (serializer.IsReading)
            {
                byte count = serializer.reader.ReadByte();
                list = new List<KeyValuePair<ushort, ArenaKillList>>(count);
                for (int i = 0; i < count; i++)
                {
                    ushort key = serializer.reader.ReadUInt16();
                    ArenaKillList value = new();
                    value.CustomSerialize(serializer);
                    list.Add(new KeyValuePair<ushort, ArenaKillList>(key, value));
                }
            }

            if (serializer.IsDelta)
                serializer.Serialize(ref removed);
        }
    }
}
