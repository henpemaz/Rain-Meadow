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
                if (OnlineManager.lobby.enumMapToLocal.TryGetValue(typeof(T), out var map))
                    extEnum = (T)Activator.CreateInstance(typeof(T),
                        new object[] { ExtEnum<T>.values.GetEntry(map[idx]), false });
                else
                {
                    RainMeadow.Error($"Failed to find ExtEnum map for {typeof(T)}; using backup value of {idx}");
                    extEnum = (T)Activator.CreateInstance(typeof(T),
                        new object[] { ExtEnum<T>.values.GetEntry(idx), false });
                }
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
                    writer.Write(OnlineManager.lobby.enumMapToRemote[typeof(T)][extEnum[i].Index]);
                }
            }
            if (IsReading)
            {
                extEnum = new T[reader.ReadByte()];
                for (int i = 0; i < extEnum.Length; i++)
                {
                    int idx = reader.ReadByte();
                    if (OnlineManager.lobby.enumMapToLocal.TryGetValue(typeof(T), out var map))
                        extEnum[i] = (T)Activator.CreateInstance(typeof(T),
                            new object[] { ExtEnum<T>.values.GetEntry(map[idx]), false });
                    else
                    {
                        RainMeadow.Error($"Failed to find ExtEnum map for {typeof(T)}; using backup value of {idx}");
                        extEnum[i] = (T)Activator.CreateInstance(typeof(T),
                            new object[] { ExtEnum<T>.values.GetEntry(idx), false });
                    }
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
                    writer.Write(OnlineManager.lobby.enumMapToRemote[typeof(T)][extEnum[i].Index]);
                }
            }
            if (IsReading)
            {
                var count = reader.ReadByte();
                extEnum = new(count);
                for (int i = 0; i < count; i++)
                {
                    int idx = reader.ReadByte();
                    if (OnlineManager.lobby.enumMapToLocal.TryGetValue(typeof(T), out var map))
                        extEnum.Add((T)Activator.CreateInstance(typeof(T),
                            new object[] { ExtEnum<T>.values.GetEntry(map[idx]), false }));
                    else
                    {
                        RainMeadow.Error($"Failed to find ExtEnum map for {typeof(T)}; using backup value of {idx}");
                        extEnum.Add((T)Activator.CreateInstance(typeof(T),
                            new object[] { ExtEnum<T>.values.GetEntry(idx), false }));
                    }
                }
            }
        }
    }
}
