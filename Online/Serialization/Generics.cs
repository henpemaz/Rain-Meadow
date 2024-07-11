using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow.Generics
{
    // Welcome to generics hell

    /// <summary>
    /// Object that tracks/serializes whether it's a delta
    /// by convention returns object with IsEmptyDelta set on same-value delta (ease for polymorphism)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPrimaryDelta<T> : IDelta<T>
    {
        public bool IsEmptyDelta { get; }
    }

    /// <summary>
    /// Simple delta, doesn't know/serialize whether its delta or not
    /// by convention retuns null on same-value delta
    /// no convenient inheritance/poly support
    /// use new() to instantiate new
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDelta<T>
    {
        public T Delta(T other);
        public T ApplyDelta(T other);
    }

    /// <summary>
    /// ID for matching elements in list-wise deltas
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IIdentifiable<T> where T : IEquatable<T>
    {
        public T ID { get; }
    }

    public class IdentityComparer<T, U> : IEqualityComparer<T> where T : IIdentifiable<U> where U : IEquatable<U>
    {
        public static IdentityComparer<T, U> instance = new();
        public bool Equals(T x, T y)
        {
            return x.ID.Equals(y.ID);
        }

        public int GetHashCode(T obj)
        {
            return obj.ID.GetHashCode();
        }
    }

    /// <summary>
    /// Dynamic list, order-unaware
    /// </summary>
    public abstract class AddRemoveUnsortedList<T, Imp> : IDelta<Imp>, Serializer.ICustomSerializable where Imp : AddRemoveUnsortedList<T, Imp>, new()
    {
        public List<T> list;
        public List<T> removed;

        public AddRemoveUnsortedList() { }
        public AddRemoveUnsortedList(List<T> list)
        {
            this.list = list;
        }

        public Imp Delta(Imp other)
        {
            if (other == null) { return (Imp)this; }
            Imp delta = new();
            delta.list = list.Except(other.list).ToList();
            delta.removed = other.list.Except(list).ToList();
            return (delta.list.Count == 0 && delta.removed.Count == 0) ? null : delta;
        }

        public Imp ApplyDelta(Imp other)
        {
            Imp result = new();
            result.list = other == null ? list : list.Union(other.list).Except(other.removed).ToList();
            return result;
        }

        public abstract void CustomSerialize(Serializer serializer);
    }


    /// <summary>
    /// Dynamic list, order-aware, no subdelta
    /// </summary>
    public abstract class AddRemoveSortedList<T, Imp> : IDelta<Imp>, Serializer.ICustomSerializable where Imp : AddRemoveSortedList<T, Imp>, new()
    {
        public List<T> list;
        public List<byte> listIndexes;
        public List<byte> removedIndexes;

        public AddRemoveSortedList() { }
        public AddRemoveSortedList(List<T> list)
        {
            this.list = list;
        }

        public Imp Delta(Imp other)
        {
            if (other == null) { return (Imp)this; }

            Imp delta = new();
            delta.list = list.Except(other.list).ToList();
            delta.listIndexes = delta.list.Select(e => (byte)list.IndexOf(e)).ToList();
            delta.removedIndexes = other.list.Except(list).Select(e => (byte)other.list.IndexOf(e)).ToList();

            return (delta.list.Count == 0 && delta.removedIndexes.Count == 0) ? null : delta;
        }

        public Imp ApplyDelta(Imp other)
        {
            Imp result = new();
            result.list = list.ToList();
            if (other != null)
            {
                result.list.Capacity = list.Count + other.list.Count;
                for (int j = other.removedIndexes.Count - 1; j >= 0; j--)
                {
                    result.list.RemoveAt(other.removedIndexes[j]);
                }
                for (int i = 0; i < other.list.Count; i++)
                {
                    result.list.Insert(other.listIndexes[i], other.list[i]);
                }
            }
            return result;
        }

        public abstract void CustomSerialize(Serializer serializer);
    }

    /// <summary>
    /// Fixed ordered list, no subdelta
    /// </summary>
    public abstract class FixedOrderedList<T, Imp> : IDelta<Imp>, Serializer.ICustomSerializable where Imp : FixedOrderedList<T, Imp>, new()
    {
        public List<T> list;
        public List<byte> updateIndexes;

        public FixedOrderedList() { }
        public FixedOrderedList(List<T> list)
        {
            this.list = list;
        }

        public Imp Delta(Imp other)
        {
            if (other == null) { return (Imp)this; }

            Imp delta = new();
            (delta.list, delta.updateIndexes) = other.list.Select((e, i) => { return (e, i: (byte)i); }).Where(e => list[e.i].Equals(e.e)).ToListTuple();
            return (delta.list.Count == 0) ? null : delta;
        }

        public Imp ApplyDelta(Imp other)
        {
            Imp result = new();
            result.list = list.ToList();
            if (other != null)
            {
                for (int i = 0; i < other.updateIndexes.Count; i++)
                {
                    list[other.updateIndexes[i]] = other.list[i];
                }
            }
            return result;
        }

        public abstract void CustomSerialize(Serializer serializer);
    }

    /// <summary>
    /// Static list, no adds/removes supported, id-elementwise delta
    /// </summary>
    public abstract class IdentifiablesDeltaList<T, U, V, Imp> : Serializer.ICustomSerializable, IDelta<Imp> where T : IDelta<V>, V, IIdentifiable<U> where U : IEquatable<U> where Imp : IdentifiablesDeltaList<T, U, V, Imp>, new()
    {
        public List<T> list;
        public IdentifiablesDeltaList() { }
        public IdentifiablesDeltaList(List<T> list)
        {
            this.list = list;
        }

        public virtual Imp Delta(Imp other)
        {
            if (other == null) { return (Imp)this; }
            Imp delta = new();
            delta.list = list.Select(sl => (T)sl.Delta(other.list.FirstOrDefault(osl => osl.ID.Equals(sl.ID)))).Where(sl => sl != null).ToList();
            return delta.list.Count == 0 ? null : delta;
        }

        public virtual Imp ApplyDelta(Imp other)
        {
            Imp result = new();
            result.list = other == null ? list : list.Select(e => (T)e.ApplyDelta(other.list.FirstOrDefault(o => e.ID.Equals(o.ID)))).ToList();
            return result;
        }

        public abstract void CustomSerialize(Serializer serializer);
    }

    /// <summary>
    /// Dynamic list, id-elementwise delta
    /// </summary>
    public abstract class IdentifiablesAddRemoveDeltaList<T, U, W, Imp> : Serializer.ICustomSerializable, IDelta<Imp> where T : class, IDelta<W>, W, IIdentifiable<U> where U : IEquatable<U> where Imp : IdentifiablesAddRemoveDeltaList<T, U, W, Imp>, new()
    {
        public List<T> list;
        public List<U> removed;
        private Dictionary<U, T> lookup;
        private HashSet<U> removedLookup;
        public IdentifiablesAddRemoveDeltaList() { }
        public IdentifiablesAddRemoveDeltaList(List<T> list)
        {
            this.list = list;
            BuildLookup();
        }

        private void BuildLookup()
        {
            this.lookup = list.Select(e => new KeyValuePair<U, T>(e.ID, e)).ToDictionary();
            removedLookup = removed == null ? null : new HashSet<U>(removed);
        }

        public virtual Imp Delta(Imp other)
        {
            if (other == null) { return (Imp)this; }
            Imp delta = new();
            delta.list = list.Select(sl => other.lookup.TryGetValue(sl.ID, out var b) ? (T)sl.Delta(b) : sl).Where(sl => sl != null).ToList();
            delta.removed = other.list.Select(e => e.ID).Where(e => !lookup.ContainsKey(e)).ToList();
            delta.BuildLookup();
            return (delta.list.Count == 0 && delta.removed.Count == 0) ? null : delta;
        }

        public virtual Imp ApplyDelta(Imp other)
        {
            Imp result = new();
            if (other == null)
            {
                result.list = list;
            }
            else
            {
                result.list =
                list.Where(e => !other.removedLookup.Contains(e.ID)) // remove
                    .Select(e => other.lookup.TryGetValue(e.ID, out var o) ? (T)e.ApplyDelta(o) : e) // keep or update
                    .Concat(other.list.Where(o => !lookup.ContainsKey(o.ID))) // add new
                    .ToList();
            }
            result.BuildLookup();
            return result;
        }

        public void CustomSerialize(Serializer serializer)
        {
            SerializeImpl(serializer);
            if (serializer.IsReading)
            {
                BuildLookup();
            }
        }

        public abstract void SerializeImpl(Serializer serializer);
    }

    /// <summary>
    /// Dynamic list, id-elementwise delta
    /// </summary>
    public abstract class IdentifiablesAddRemovePrimaryDeltaList<T, U, W, Imp> : Serializer.ICustomSerializable, IDelta<Imp> where T : class, IPrimaryDelta<W>, W, IIdentifiable<U> where U : IEquatable<U> where Imp : IdentifiablesAddRemovePrimaryDeltaList<T, U, W, Imp>, new()
    {
        public List<T> list;
        public List<U> removed;
        private Dictionary<U, T> lookup;
        private HashSet<U> removedLookup;

        public IdentifiablesAddRemovePrimaryDeltaList() { }
        public IdentifiablesAddRemovePrimaryDeltaList(List<T> list)
        {
            this.list = list;
            BuildLookup();
        }

        private void BuildLookup()
        {
            this.lookup = list.Select(e => new KeyValuePair<U, T>(e.ID, e)).ToDictionary();
            removedLookup = removed == null ? null : new HashSet<U>(removed);
        }

        public virtual Imp Delta(Imp baseline)
        {
            if (baseline == null) { return (Imp)this; }
            Imp delta = new();
            delta.list = list.Select(e => baseline.lookup.TryGetValue(e.ID, out var b) ? (T)e.Delta(b) : e).Where(sl => !sl.IsEmptyDelta).ToList();
            delta.removed = baseline.list.Select(e => e.ID).Where(e => !lookup.ContainsKey(e)).ToList();
            delta.BuildLookup();
            return (delta.list.Count == 0 && delta.removed.Count == 0) ? null : delta;
        }

        public virtual Imp ApplyDelta(Imp incoming)
        {
            Imp result = new();
            if(incoming == null)
            {
                result.list = list;
            }
            else
            {
                result.list =
                list.Where(e => !incoming.removedLookup.Contains(e.ID)) // remove
                    .Select(e => incoming.lookup.TryGetValue(e.ID, out var o) ? (T)e.ApplyDelta(o) : e) // keep or update
                    .Concat(incoming.list.Where(o => !lookup.ContainsKey(o.ID))) // add new
                    .ToList();
            }
            result.BuildLookup();
            return result;
        }

        public void CustomSerialize(Serializer serializer)
        {
            SerializeImpl(serializer);
            if (serializer.IsReading)
            {
                BuildLookup();
            }
        }

        public abstract void SerializeImpl(Serializer serializer);
    }

    public class IdentifiablesAddRemoveDeltaListByUSort<T, W> : IdentifiablesAddRemoveDeltaList<T, ushort, W, IdentifiablesAddRemoveDeltaListByUSort<T, W>> where T : class, Serializer.ICustomSerializable, IDelta<W>, W, IIdentifiable<ushort>, new()
    {
        public IdentifiablesAddRemoveDeltaListByUSort() : base() { }
        public IdentifiablesAddRemoveDeltaListByUSort(List<T> list) : base(list) { }

        public override void SerializeImpl(Serializer serializer)
        {
            serializer.SerializeByte(ref list);
            if (serializer.IsDelta) serializer.Serialize(ref removed);
        }
    }

    public class IdentifiablesAddRemoveDeltaListByCustomSeri<T, U, W> : IdentifiablesAddRemoveDeltaList<T, U, W, IdentifiablesAddRemoveDeltaListByCustomSeri<T, U, W>> where T : class, Serializer.ICustomSerializable, IDelta<W>, W, IIdentifiable<U>, new() where U : Serializer.ICustomSerializable, IEquatable<U>, new()
    {
        public IdentifiablesAddRemoveDeltaListByCustomSeri() : base() { }
        public IdentifiablesAddRemoveDeltaListByCustomSeri(List<T> list) : base(list) { }

        public override void SerializeImpl(Serializer serializer)
        {
            serializer.SerializeByte(ref list);
            if (serializer.IsDelta) serializer.SerializeByte(ref removed);
        }
    }

    public class DeltaStates<T, W, U> : IdentifiablesAddRemovePrimaryDeltaList<T, U, W, DeltaStates<T, W, U>> where T : OnlineState, IPrimaryDelta<W>, W, IIdentifiable<U> where U : Serializer.ICustomSerializable, IEquatable<U>, new()
    {
        public DeltaStates() : base() { }
        public DeltaStates(List<T> list) : base(list) { }

        public override void SerializeImpl(Serializer serializer)
        {
            serializer.SerializePolyStatesShort(ref list);
            if (serializer.IsDelta) serializer.SerializeByte(ref removed); // this could potentially break actually
        }
    }

    public class DeltaDataStates<T>: IdentifiablesAddRemovePrimaryDeltaList<T, byte, OnlineState, DeltaDataStates<T>> where T: OnlineState, IIdentifiable<byte>
    {
        public DeltaDataStates() : base() { }
        public DeltaDataStates(List<T> list) : base(list) { }

        public override void SerializeImpl(Serializer serializer)
        {
            serializer.SerializePolyStatesByte(ref list);
            if (serializer.IsDelta) serializer.Serialize(ref removed);
        }
    }

    public class AddRemoveUnsortedUshorts : AddRemoveUnsortedList<ushort, AddRemoveUnsortedUshorts>
    {
        public AddRemoveUnsortedUshorts() { }
        public AddRemoveUnsortedUshorts(List<ushort> list) : base(list) { }

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref list);
            if (serializer.IsDelta) serializer.Serialize(ref removed);
        }
    }

    public class AddRemoveUnsortedCustomSerializables<T> : AddRemoveUnsortedList<T, AddRemoveUnsortedCustomSerializables<T>> where T : Serializer.ICustomSerializable, new()
    {
        public AddRemoveUnsortedCustomSerializables() { }
        public AddRemoveUnsortedCustomSerializables(List<T> list) : base(list) { }

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.SerializeByte(ref list);
            if (serializer.IsDelta) serializer.SerializeByte(ref removed);
        }
    }

    public class AddRemoveSortedCustomSerializables<T> : AddRemoveSortedList<T, AddRemoveSortedCustomSerializables<T>> where T : Serializer.ICustomSerializable, new()
    {
        public AddRemoveSortedCustomSerializables() { }
        public AddRemoveSortedCustomSerializables(List<T> list) : base(list) { }

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.SerializeByte(ref list);
            if (serializer.IsDelta)
            {
                serializer.Serialize(ref listIndexes);
                serializer.Serialize(ref removedIndexes);
            }
        }
    }
    public class AddRemoveSortedExtEnums<T> : AddRemoveSortedList<T, AddRemoveSortedExtEnums<T>> where T : ExtEnum<T>
    {
        public AddRemoveSortedExtEnums() { }
        public AddRemoveSortedExtEnums(List<T> list) : base(list) { }

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.SerializeExtEnums<T>(ref list);
            if (serializer.IsDelta)
            {
                serializer.Serialize(ref listIndexes);
                serializer.Serialize(ref removedIndexes);
            }
        }
    }

    public class AddRemoveSortedPlayerIDs : AddRemoveSortedList<MeadowPlayerId, AddRemoveSortedPlayerIDs>
    {
        public AddRemoveSortedPlayerIDs() { }
        public AddRemoveSortedPlayerIDs(List<MeadowPlayerId> list) : base(list) { }

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.SerializePlayerIds(ref list);
            if (serializer.IsDelta)
            {
                serializer.Serialize(ref listIndexes);
                serializer.Serialize(ref removedIndexes);
            }
        }
    }

    public class AddRemoveSortedEntityIDs : AddRemoveSortedList<OnlineEntity.EntityId, AddRemoveSortedEntityIDs>
    {
        public AddRemoveSortedEntityIDs() { }
        public AddRemoveSortedEntityIDs(List<OnlineEntity.EntityId> list) : base(list) { }

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.SerializeByte(ref list);
            if (serializer.IsDelta)
            {
                serializer.Serialize(ref listIndexes);
                serializer.Serialize(ref removedIndexes);
            }
        }
    }

    public class AddRemoveSortedEvents<T> : AddRemoveSortedList<T, AddRemoveSortedEvents<T>> where T : OnlineEvent
    {
        public AddRemoveSortedEvents() { }
        public AddRemoveSortedEvents(List<T> list) : base(list) { }

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.SerializeEvents(ref list);
            if (serializer.IsDelta)
            {
                serializer.Serialize(ref listIndexes);
                serializer.Serialize(ref removedIndexes);
            }
        }
    }

    public class AddRemoveSortedStates<T> : AddRemoveSortedList<T, AddRemoveSortedStates<T>> where T : OnlineState
    {
        public AddRemoveSortedStates() { }
        public AddRemoveSortedStates(List<T> list) : base(list) { }

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.SerializePolyStatesShort(ref list);
            if (serializer.IsDelta)
            {
                serializer.Serialize(ref listIndexes);
                serializer.Serialize(ref removedIndexes);
            }
        }
    }

    public class AddRemoveSortedStrings : AddRemoveSortedList<string, AddRemoveSortedStrings>
    {
        public AddRemoveSortedStrings() { }
        public AddRemoveSortedStrings(List<string> list) : base(list) { }

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref list);
            if (serializer.IsDelta)
            {
                serializer.Serialize(ref listIndexes);
                serializer.Serialize(ref removedIndexes);
            }
        }
    }

    public class AddRemoveSortedUshorts : AddRemoveSortedList<ushort, AddRemoveSortedUshorts>
    {
        public AddRemoveSortedUshorts() { }
        public AddRemoveSortedUshorts(List<ushort> list) : base(list) { }

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref list);
            if (serializer.IsDelta)
            {
                serializer.Serialize(ref listIndexes);
                serializer.Serialize(ref removedIndexes);
            }
        }
    }

    public class FixedOrderedUshorts : FixedOrderedList<ushort, FixedOrderedUshorts>
    {
        public FixedOrderedUshorts() { }
        public FixedOrderedUshorts(List<ushort> list) : base(list) { }

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref list);
            if (serializer.IsDelta)
            {
                serializer.Serialize(ref updateIndexes);
            }
        }
    }
}