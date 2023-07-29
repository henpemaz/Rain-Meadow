using System.Collections.Generic;

namespace RainMeadow
{
    public partial class Serializer
    {
        public interface ICustomSerializable
        {
            void CustomSerialize(Serializer serializer);
        }

        public void Serialize<T>(ref T customSerializable) where T : ICustomSerializable, new()
        {
            if (IsReading) customSerializable = new();
            customSerializable.CustomSerialize(this);
        }

        public void SerializeNullable<T>(ref T customSerializable) where T : ICustomSerializable, new()
        {
            if (IsWriting)
            {
                writer.Write(customSerializable != null);
                if (customSerializable != null)
                {
                    customSerializable.CustomSerialize(this);
                }
            }
            if (IsReading)
            {
                if (reader.ReadBoolean())
                {
                    customSerializable = new();
                    customSerializable.CustomSerialize(this);
                }
            }
        }

        public void Serialize<T>(ref List<T> listOfSerializables) where T : ICustomSerializable, new()
        {
            if (IsWriting)
            {
                writer.Write((byte)listOfSerializables.Count);
                for (int i = 0; i < listOfSerializables.Count; i++)
                {
                    listOfSerializables[i].CustomSerialize(this);
                }
            }
            if (IsReading)
            {
                var count = reader.ReadByte();
                listOfSerializables = new(count);
                for (int i = 0; i < count; i++)
                {
                    T item = new();
                    item.CustomSerialize(this);
                    listOfSerializables.Add(item);
                }
            }
        }

        public void Serialize<T>(ref T[] arrayOfSerializables) where T : ICustomSerializable, new()
        {
            if (IsWriting)
            {
                writer.Write((byte)arrayOfSerializables.Length);
                for (int i = 0; i < arrayOfSerializables.Length; i++)
                {
                    arrayOfSerializables[i].CustomSerialize(this);
                }
            }
            if (IsReading)
            {
                var count = reader.ReadByte();
                arrayOfSerializables = new T[count];
                for (int i = 0; i < count; i++)
                {
                    T item = new();
                    item.CustomSerialize(this);
                    arrayOfSerializables[i] = item;
                }
            }
        }
    }
}