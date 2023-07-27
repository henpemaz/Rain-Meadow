using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow.Generics
{
    // Welcome to generics hell
    // I think I want to go back to using a type parameter for concrete type with new() instead of newinstance pattern, 
    //      because then we get the right types out of delta/addelta without silly casts
    // how do we manage returning null on empty delta, vs supporting override of delta?

    public interface IDelta<T>
    {
        //public T EmptyInstance();
        public T Delta(T other);
        public T ApplyDelta(T other);
    }

    public interface IIdentifiable<T> where T : IEquatable<T>
    {
        public T ID { get; }
    }

    public class IdentityComparer<T, U> : IEqualityComparer<T> where T : IIdentifiable<U> where U : IEquatable<U>
    {
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
    public abstract class AddRemoveUnsortedList<T> : IDelta<AddRemoveUnsortedList<T>>, Serializer.ICustomSerializable
    {
        public List<T> list;
        public List<T> removed;

        public AddRemoveUnsortedList() { }
        public AddRemoveUnsortedList(List<T> list)
        {
            this.list = list;
        }

        public abstract AddRemoveUnsortedList<T> EmptyInstance();

        public AddRemoveUnsortedList<T> Delta(AddRemoveUnsortedList<T> other)
        {
            if (other == null) { return this; }
            var delta = EmptyInstance();
            delta.list = list.Except(other.list).ToList();
            delta.removed = other.list.Except(list).ToList();
            return (delta.list.Count == 0 && delta.removed.Count == 0) ? null : delta;
        }

        public AddRemoveUnsortedList<T> ApplyDelta(AddRemoveUnsortedList<T> other)
        {
            var result = EmptyInstance();
            result.list = other == null ? list : list.Union(other.list).Except(other.removed).ToList();
            return result;
        }

        public abstract void CustomSerialize(Serializer serializer);
    }


    /// <summary>
    /// Dynamic list, order-aware
    /// </summary>
    public abstract class AddRemoveSortedList<T> : IDelta<AddRemoveSortedList<T>>, Serializer.ICustomSerializable
    {
        public List<T> list;
        public List<byte> listIndexes;
        public List<byte> removedIndexes;

        public AddRemoveSortedList() { }
        public AddRemoveSortedList(List<T> list)
        {
            this.list = list;
        }

        public abstract AddRemoveSortedList<T> EmptyInstance();

        public AddRemoveSortedList<T> Delta(AddRemoveSortedList<T> other)
        {
            if (other == null) { return this; }

            var delta = EmptyInstance();
            delta.list = list.Except(other.list).ToList();
            delta.listIndexes = list.Except(other.list).Select(e => (byte)list.IndexOf(e)).ToList();
            delta.removedIndexes = other.list.Except(list).Select(e => (byte)other.list.IndexOf(e)).ToList();

            return (delta.list.Count == 0 && delta.removedIndexes.Count == 0) ? null : delta;
        }

        public AddRemoveSortedList<T> ApplyDelta(AddRemoveSortedList<T> other)
        {
            var result = EmptyInstance();
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
    /// Fixed list, no adds/removes supported, id-elementwise delta
    /// </summary>
    public class IdentifiablesDeltaList<T, U, W> : Serializer.ICustomSerializable, IDelta<IdentifiablesDeltaList<T, U, W>> where T : Serializer.ICustomSerializable, IDelta<W>, W, IIdentifiable<U>, new() where U : IEquatable<U>
    {
        public List<T> list;
        public IdentifiablesDeltaList() { }
        public IdentifiablesDeltaList(List<T> list)
        {
            this.list = list;
        }
        public virtual IdentifiablesDeltaList<T, U, W> EmptyInstance() => new();

        public virtual IdentifiablesDeltaList<T, U, W> Delta(IdentifiablesDeltaList<T, U, W> other)
        {
            if (other == null) { return this; }
            var delta = EmptyInstance();
            delta.list = list.Select(sl => (T)sl.Delta(other.list.FirstOrDefault(osl => osl.ID.Equals(sl.ID)))).Where(sl => sl != null).ToList();
            return delta.list.Count == 0 ? null : delta;
        }

        public virtual IdentifiablesDeltaList<T, U, W> ApplyDelta(IdentifiablesDeltaList<T, U, W> other)
        {
            var result = EmptyInstance();
            result.list = other == null ? list : list.Select(e => (T)e.ApplyDelta(other.list.FirstOrDefault(o => e.ID.Equals(o.ID)))).ToList();
            return result;
        }

        public virtual void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref list);
        }
    }

    /// <summary>
    /// Dynamic list, id-elementwise delta
    /// </summary>
    public abstract class IdentifiablesAddRemoveDeltaList<T, U, W> : Serializer.ICustomSerializable, IDelta<IdentifiablesAddRemoveDeltaList<T, U, W>> where T : class, IDelta<W>, W, IIdentifiable<U> where U : IEquatable<U>
    {
        public List<T> list;
        public List<U> removed;
        public IdentifiablesAddRemoveDeltaList() { }
        public IdentifiablesAddRemoveDeltaList(List<T> list)
        {
            this.list = list;
        }
        public abstract IdentifiablesAddRemoveDeltaList<T, U, W> EmptyInstance();

        public virtual IdentifiablesAddRemoveDeltaList<T, U, W> Delta(IdentifiablesAddRemoveDeltaList<T, U, W> other)
        {
            if (other == null) { return this; }
            var delta = EmptyInstance();
            delta.list = list.Select(sl => (T)sl.Delta(other.list.FirstOrDefault(osl => osl.ID.Equals(sl.ID)))).Where(sl => sl != null).ToList();
            delta.removed = other.list.Except(list, new IdentityComparer<T, U>()).Select(e => e.ID).ToList();
            return (delta.list.Count == 0 && delta.removed.Count == 0) ? null : delta;
        }

        public virtual IdentifiablesAddRemoveDeltaList<T, U, W> ApplyDelta(IdentifiablesAddRemoveDeltaList<T, U, W> other)
        {
            var result = EmptyInstance();
            result.list = other == null ? list : list.Where(e => other.list.FirstOrDefault(o => e.ID.Equals(o.ID)) != null).Select(e => (T)e.ApplyDelta(other.list.FirstOrDefault(o => e.ID.Equals(o.ID)))).Concat(other.list.Where(o => list.FirstOrDefault(e => e.ID.Equals(o.ID)) == null)).ToList();
            return result;
        }

        public abstract void CustomSerialize(Serializer serializer);
    }

    public class IdentifiablesAddRemoveDeltaListByUSort<T, W> : IdentifiablesAddRemoveDeltaList<T, ushort, W> where T : class, Serializer.ICustomSerializable, IDelta<W>, W, IIdentifiable<ushort>, new()
    {
        public IdentifiablesAddRemoveDeltaListByUSort() : base() { }
        public IdentifiablesAddRemoveDeltaListByUSort(List<T> list) : base(list) { }
        public override IdentifiablesAddRemoveDeltaList<T, ushort, W> EmptyInstance() => new IdentifiablesAddRemoveDeltaListByUSort<T, W>();

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref list);
            if (serializer.IsDelta) serializer.Serialize(ref removed);
        }
    }

    public class IdentifiablesAddRemoveDeltaListByCustomSeri<T, U, W> : IdentifiablesAddRemoveDeltaList<T, U, W> where T : class, Serializer.ICustomSerializable, IDelta<W>, W, IIdentifiable<U>, new() where U : Serializer.ICustomSerializable, IEquatable<U>, new()
    {
        public IdentifiablesAddRemoveDeltaListByCustomSeri() : base() { }
        public IdentifiablesAddRemoveDeltaListByCustomSeri(List<T> list) : base(list) { }
        public override IdentifiablesAddRemoveDeltaList<T, U, W> EmptyInstance() => new IdentifiablesAddRemoveDeltaListByCustomSeri<T, U, W>();

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref list);
            if (serializer.IsDelta) serializer.Serialize(ref removed);
        }
    }

    public class DeltaStates<T, U> : IdentifiablesAddRemoveDeltaList<T, U, OnlineState> where T : OnlineState, IDelta<OnlineState>, IIdentifiable<U> where U : Serializer.ICustomSerializable, IEquatable<U>, new()
    {
        public DeltaStates() : base() { }
        public DeltaStates(List<T> list) : base(list) { }
        public override IdentifiablesAddRemoveDeltaList<T, U, OnlineState> EmptyInstance() => new DeltaStates<T, U>();

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.SerializePolyStates(ref list);
            if (serializer.IsDelta) serializer.Serialize(ref removed);
        }
    }

    public class AddRemoveUnsortedUshorts : AddRemoveUnsortedList<ushort>
    {
        public AddRemoveUnsortedUshorts() { }
        public AddRemoveUnsortedUshorts(List<ushort> list) : base(list) { }
        public override AddRemoveUnsortedList<ushort> EmptyInstance() => new AddRemoveUnsortedUshorts();

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref list);
            if (serializer.IsDelta) serializer.Serialize(ref removed);
        }
    }

    public class AddRemoveUnsortedCustomSerializables<T> : AddRemoveUnsortedList<T> where T : Serializer.ICustomSerializable, new()
    {
        public AddRemoveUnsortedCustomSerializables() { }
        public AddRemoveUnsortedCustomSerializables(List<T> list) : base(list) { }
        public override AddRemoveUnsortedList<T> EmptyInstance() => new AddRemoveUnsortedCustomSerializables<T>();

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref list);
            if (serializer.IsDelta) serializer.Serialize(ref removed);
        }
    }

    public class AddRemoveSortedCustomSerializables<T> : AddRemoveSortedList<T> where T : Serializer.ICustomSerializable, new()
    {
        public AddRemoveSortedCustomSerializables() { }
        public AddRemoveSortedCustomSerializables(List<T> list) : base(list) { }
        public override AddRemoveSortedList<T> EmptyInstance() => new AddRemoveSortedCustomSerializables<T>();

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

    public class AddRemoveSortedPlayerIDs : AddRemoveSortedList<MeadowPlayerId>
    {
        public AddRemoveSortedPlayerIDs() { }
        public AddRemoveSortedPlayerIDs(List<MeadowPlayerId> list) : base(list) { }
        public override AddRemoveSortedList<MeadowPlayerId> EmptyInstance() => new AddRemoveSortedPlayerIDs();

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

    public class AddRemoveSortedUshorts : AddRemoveSortedList<ushort>
    {
        public AddRemoveSortedUshorts() { }
        public AddRemoveSortedUshorts(List<ushort> list) : base(list) { }
        public override AddRemoveSortedList<ushort> EmptyInstance() => new AddRemoveSortedUshorts();

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
}
