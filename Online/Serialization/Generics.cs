using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    // Welcome to generics hell

    public interface IDelta<T>
    {
        public T Delta(T other);
        public void AddDelta(T other);
        public bool isDelta { get; }
        public bool isEmptyDelta { get; }
    }

    // T is the type, U is the concrete class for new()
    public abstract class SerializableIDeltaUnsortedList<T, U> : IDelta<U>, Serializer.ICustomSerializable where U : SerializableIDeltaUnsortedList<T, U>, new()
    {
        public List<T> list;
        public List<T> removed;
        protected bool _isDelta;

        public bool isDelta { get => _isDelta; protected set => _isDelta = value; }
        public bool isEmptyDelta { get; protected set; }

        public SerializableIDeltaUnsortedList() { }
        public SerializableIDeltaUnsortedList(List<T> list)
        {
            this.list = list;
        }

        public U Delta(U other)
        {
            if (isDelta) throw new InvalidProgrammerException("is already delta");
            if (other == null) { return (U)this; }
            if (other.isDelta) throw new InvalidProgrammerException("other is delta");
            return new U()
            {
                isDelta = true,
                list = list.Except(other.list).ToList(),
                removed = other.list.Except(list).ToList(),
                isEmptyDelta = list.Count == 0 && removed.Count == 0
            };
        }

        public void AddDelta(U other)
        {
            if (isDelta) throw new InvalidProgrammerException("is already delta");
            if (!other.isDelta) throw new InvalidProgrammerException("other not delta");
            list = list.Union(other.list).Except(other.removed).ToList();
        }

        public abstract void CustomSerialize(Serializer serializer);
    }


    // not thorougly tested
    // T is the type, U is the concrete class for new()
    public abstract class SerializableIDeltaSortedList<T, U> : IDelta<U>, Serializer.ICustomSerializable where U : SerializableIDeltaSortedList<T, U>, new()
    {
        public List<T> list;
        public List<byte> listIndexes;
        public List<byte> removedIndexes;
        protected bool _isDelta;

        public bool isDelta { get => _isDelta; protected set => _isDelta = value; }
        public bool isEmptyDelta { get; protected set; }

        public SerializableIDeltaSortedList() { }
        public SerializableIDeltaSortedList(List<T> list)
        {
            this.list = list;
        }

        public U Delta(U other)
        {
            if (isDelta) throw new InvalidProgrammerException("is already delta");
            if (other == null) { return (U)this; }
            if (other.isDelta) throw new InvalidProgrammerException("other is delta");

            var delta = new U()
            {
                isDelta = true,
                list = list.Except(other.list).ToList(),
                listIndexes = list.Except(other.list).Select(e => (byte)list.IndexOf(e)).ToList(),
                removedIndexes = other.list.Except(list).Select(e => (byte)other.list.IndexOf(e)).ToList(),
            };
            delta.isEmptyDelta = delta.list.Count == 0 && delta.removedIndexes.Count == 0;
            return delta;
        }

        public void AddDelta(U other)
        {
            if (isDelta) throw new InvalidProgrammerException("is already delta");
            if (!other.isDelta) throw new InvalidProgrammerException("other not delta");
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


    public class SerializableIDeltaUnsortedListOfUShorts : SerializableIDeltaUnsortedList<ushort, SerializableIDeltaUnsortedListOfUShorts>
    {
        public SerializableIDeltaUnsortedListOfUShorts() { }
        public SerializableIDeltaUnsortedListOfUShorts(List<ushort> list) : base(list) { }

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref _isDelta);
            serializer.Serialize(ref list);
            if (isDelta) serializer.Serialize(ref removed);
        }
    }

    public class SerializableIDeltaUnsortedListOfICustomSerializables<T> : SerializableIDeltaUnsortedList<T, SerializableIDeltaUnsortedListOfICustomSerializables<T>> where T : Serializer.ICustomSerializable, new()
    {
        public SerializableIDeltaUnsortedListOfICustomSerializables() { }
        public SerializableIDeltaUnsortedListOfICustomSerializables(List<T> list) : base(list) { }

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref _isDelta);
            serializer.Serialize(ref list);
            if (isDelta) serializer.Serialize(ref removed);
        }
    }

    public class SerializableIDeltaSortedListOfICustomSerializables<T> : SerializableIDeltaSortedList<T, SerializableIDeltaSortedListOfICustomSerializables<T>> where T : Serializer.ICustomSerializable, new()
    {
        public SerializableIDeltaSortedListOfICustomSerializables() { }
        public SerializableIDeltaSortedListOfICustomSerializables(List<T> list) : base(list) { }

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref _isDelta);
            serializer.Serialize(ref list);
            if (isDelta)
            {
                serializer.Serialize(ref listIndexes);
                serializer.Serialize(ref removedIndexes);
            }
        }
    }
}
