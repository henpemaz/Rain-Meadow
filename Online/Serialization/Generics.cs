using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    // Welcome to generics hell

    public interface IPrimaryDelta<T>
    {
        public bool IsDelta { get; set; }
        public T Delta(T other);
        public T ApplyDelta(T other); // Apply has a chance to pass other through without processing
    }

    public interface IDelta<T>
    {
        public T Delta(T other);
        public void ApplyDelta(T other);
    }

    public interface IIdentifiable<T> where T: IEquatable<T>
    {
        public T ID { get; }
    }

    /// <summary>
    /// Dynamic list, order-unaware
    /// </summary>
    public abstract class AddRemoveUnsortedList<T, U> : IDelta<U>, Serializer.ICustomSerializable where U : AddRemoveUnsortedList<T, U>, new()
    {
        public List<T> list;
        public List<T> removed;

        public AddRemoveUnsortedList() { }
        public AddRemoveUnsortedList(List<T> list)
        {
            this.list = list;
        }

        public U Delta(U other)
        {
            if (other == null) { return (U)this; }
            var delta = new U()
            {
                list = list.Except(other.list).ToList(),
                removed = other.list.Except(list).ToList(),
            };
            return (delta.list.Count == 0 && delta.removed.Count == 0) ? null : delta;
        }

        public void ApplyDelta(U other)
        {
            if(other != null) list = list.Union(other.list).Except(other.removed).ToList();
        }

        public abstract void CustomSerialize(Serializer serializer);
    }


    /// <summary>
    /// Dynamic list, order-aware
    /// </summary>
    public abstract class AddRemoveSortedList<T, U> : IDelta<U>, Serializer.ICustomSerializable where U : AddRemoveSortedList<T, U>, new()
    {
        public List<T> list;
        public List<byte> listIndexes;
        public List<byte> removedIndexes;

        public AddRemoveSortedList() { }
        public AddRemoveSortedList(List<T> list)
        {
            this.list = list;
        }

        public U Delta(U other)
        {
            if (other == null) { return (U)this; }

            var delta = new U()
            {
                list = list.Except(other.list).ToList(),
                listIndexes = list.Except(other.list).Select(e => (byte)list.IndexOf(e)).ToList(),
                removedIndexes = other.list.Except(list).Select(e => (byte)other.list.IndexOf(e)).ToList(),
            };
            return (delta.list.Count == 0 && delta.removedIndexes.Count == 0) ? null : delta;
        }

        public void ApplyDelta(U other)
        {
            list.Capacity = list.Count + other.list.Count;
            for (int j = other.removedIndexes.Count - 1; j >= 0; j--)
            {
                list.RemoveAt(other.removedIndexes[j]);
            }
            for (int i = 0; i < other.list.Count; i++)
            {
                list.Insert(other.listIndexes[i], other.list[i]);
            }
        }

        public abstract void CustomSerialize(Serializer serializer);
    }

    /// <summary>
    /// Fixed list, no adds/removes supported, id-elementwise delta
    /// </summary>
    public class IdentifiablesDeltaList<T, U> : Serializer.ICustomSerializable, IDelta<IdentifiablesDeltaList<T,U>> where T: Serializer.ICustomSerializable, IDelta<T>, IIdentifiable<U>, new() where U: IEquatable<U>
    {
        public List<T> list;
        public IdentifiablesDeltaList() { }
        public IdentifiablesDeltaList(List<T> list)
        {
            this.list = list;
        }

        public virtual void ApplyDelta(IdentifiablesDeltaList<T, U> other)
        {
            foreach (var otherElement in other.list)
            {
                foreach (var element in list)
                {
                    if (element.ID.Equals(otherElement.ID))
                    {
                        element.ApplyDelta(otherElement);
                        break;
                    }
                }
            }
        }

        public virtual IdentifiablesDeltaList<T, U> Delta(IdentifiablesDeltaList<T, U> other)
        {
            if (other == null) { return this; }
            var deltaList = list.Select(sl => sl.Delta(other.list.FirstOrDefault(osl => osl.ID.Equals(sl.ID)))).Where(sl => sl != null).ToList();
            return deltaList.Count == 0 ? null : new() { list = deltaList };
        }

        public virtual void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref list);
        }
    }

    public class AddRemoveUnsortedUshorts : AddRemoveUnsortedList<ushort, AddRemoveUnsortedUshorts>
    {
        public AddRemoveUnsortedUshorts() { }
        public AddRemoveUnsortedUshorts(List<ushort> list) : base(list) { }

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref list);
            if(serializer.IsDelta) serializer.Serialize(ref removed);
        }
    }

    public class AddRemoveUnsortedCustomSerializables<T> : AddRemoveUnsortedList<T, AddRemoveUnsortedCustomSerializables<T>> where T : Serializer.ICustomSerializable, new()
    {
        public AddRemoveUnsortedCustomSerializables() { }
        public AddRemoveUnsortedCustomSerializables(List<T> list) : base(list) { }

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref list);
            if (serializer.IsDelta) serializer.Serialize(ref removed);
        }
    }

    public class AddRemoveSortedCustomSerializables<T> : AddRemoveSortedList<T, AddRemoveSortedCustomSerializables<T>> where T : Serializer.ICustomSerializable, new()
    {
        public AddRemoveSortedCustomSerializables() { }
        public AddRemoveSortedCustomSerializables(List<T> list) : base(list) { }

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


}
