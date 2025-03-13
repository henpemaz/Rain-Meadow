using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public partial class Serializer
    {
        public void SerializeExtEnum<T>(ref T extEnum) where T : ExtEnum<T>
        {
            if (IsWriting)
            {
                writer.Write((byte)OnlineManager.lobby.enumMapToRemote[typeof(T)][extEnum.Index]);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
            }
            if (IsReading)
            {
                int idx = reader.ReadByte();
                extEnum = (T)Activator.CreateInstance(typeof(T),
                    new object[] { ExtEnum<T>.values.GetEntry(
                        OnlineManager.lobby.enumMapToLocal.TryGetValue(typeof(T), out var map) ? map[idx]
                        : idx //default to the idx given... this isn't a nice solution, but it's a fallback
                        ), false });
            }
        }

        public void SerializeExtEnums<T>(ref T[] extEnum) where T : ExtEnum<T>
        {
            if (IsWriting)
            {
                writer.Write((byte)extEnum.Length);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                for (int i = 0; i < extEnum.Length; i++)
                {
                    writer.Write((byte)extEnum[i].Index);
                }
            }
            if (IsReading)
            {
                extEnum = new T[reader.ReadByte()];
                for (int i = 0; i < extEnum.Length; i++)
                {
                    extEnum[i] = (T)Activator.CreateInstance(typeof(T), new object[] { ExtEnum<T>.values.GetEntry(reader.ReadByte()), false });
                }
            }
        }

        public void SerializeExtEnums<T>(ref List<T> extEnum) where T : ExtEnum<T>
        {
            if (IsWriting)
            {
                writer.Write((byte)extEnum.Count);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                for (int i = 0; i < extEnum.Count; i++)
                {
                    writer.Write((byte)extEnum[i].Index);
                }
            }
            if (IsReading)
            {
                var count = reader.ReadByte();
                extEnum = new(count);
                for (int i = 0; i < count; i++)
                {
                    extEnum.Add((T)Activator.CreateInstance(typeof(T), new object[] { ExtEnum<T>.values.GetEntry(reader.ReadByte()), false }));
                }
            }
        }
    }
}
