using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public partial class Serializer
    {
        // todo make this load-order independent. How? map strings to indexes at lobby level don't rely on the .Index
        public void SerializeExtEnum<T>(ref T extEnum) where T : ExtEnum<T>
        {
            if (IsWriting)
            {
                if (OnlineManager.lobby == null) 
                {
                    throw new InvalidProgrammerException("Can't serialize ExtEnums without a lobby");
                }

                if (!OnlineManager.lobby.enumMap.ContainsKey(typeof(T)))
                {
                    throw new InvalidProgrammerException($"ExtEnum type {typeof(T).AssemblyQualifiedName} is not serializable in this lobby. TIP: If you are a modder add the enum type to Serializer.serializedExtEnums.");
                }

                var localindex = extEnum.Index;
                var remoteIndex = OnlineManager.lobby.enumMap[typeof(T)].First(x => x.Item2 == localindex).Item1;
                writer.Write(remoteIndex);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
            }
            if (IsReading)
            {
                var remoteIndex = reader.ReadByte();
                var localIndex = OnlineManager.lobby.enumMap[typeof(T)].First(x => x.Item1 == remoteIndex).Item2;
                extEnum = (T)Activator.CreateInstance(typeof(T), new object[] { ExtEnum<T>.values.GetEntry(localIndex), false });
            }
        }

        public void SerializeNullableExtEnum<T>(ref T extEnum) where T : ExtEnum<T>
        {
            if (IsWriting)
            {
                writer.Write(extEnum != null);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                if (extEnum != null)
                {
                    SerializeExtEnum<T>(ref extEnum);
                }
            }
            if (IsReading)
            {
                if (reader.ReadBoolean())
                {
                    SerializeExtEnum<T>(ref extEnum!);
                }
            }
        }


        public void SerializeExtEnums<T>(ref T[] extEnum) where T : ExtEnum<T>
        {
            if (IsWriting)
            {
                if (OnlineManager.lobby == null) 
                {
                    throw new InvalidProgrammerException("Can't serialize ExtEnums without a lobby");
                }

                if (!OnlineManager.lobby.enumMap.ContainsKey(typeof(T)))
                {
                    throw new InvalidProgrammerException($"ExtEnum type {typeof(T).AssemblyQualifiedName} is not serializable in this lobby. TIP: If you are a modder add the enum type to Serializer.serializedExtEnums.");
                }

                writer.Write((byte)extEnum.Length);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                for (int i = 0; i < extEnum.Length; i++)
                {
                    var enum2 = extEnum[i]; // c# doesn't allow ref params to be passed down in anonymous lambdas.
                    var remoteIndex = OnlineManager.lobby.enumMap[typeof(T)].First(x => x.Item2 == enum2.Index).Item1;
                    writer.Write(remoteIndex);
                }
            }
            if (IsReading)
            {
                extEnum = new T[reader.ReadByte()];
                for (int i = 0; i < extEnum.Length; i++)
                {
                    var localIndex = OnlineManager.lobby.enumMap[typeof(T)].First(x => x.Item1 == reader.ReadByte()).Item2;
                    extEnum[i] = (T)Activator.CreateInstance(typeof(T), new object[] { ExtEnum<T>.values.GetEntry(localIndex), false });
                }
            }
        }

        public void SerializeExtEnums<T>(ref List<T> extEnum) where T : ExtEnum<T>
        {
            if (IsWriting)
            {
                if (OnlineManager.lobby == null) 
                {
                    throw new InvalidProgrammerException("Can't serialize ExtEnums without a lobby");
                }

                if (!OnlineManager.lobby.enumMap.ContainsKey(typeof(T)))
                {
                    throw new InvalidProgrammerException($"ExtEnum type {typeof(T).AssemblyQualifiedName} is not serializable in this lobby. TIP: If you are a modder add the enum type to Serializer.serializedExtEnums.");
                }

                writer.Write((byte)extEnum.Count);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                for (int i = 0; i < extEnum.Count; i++)
                {
                    var enum2 = extEnum[i]; // c# doesn't allow ref params to be passed down in anonymous lambdas.
                    var remoteIndex = OnlineManager.lobby.enumMap[typeof(T)].First(x => x.Item2 == enum2.Index).Item1;
                    writer.Write(remoteIndex);
                }
            }
            if (IsReading)
            {
                var count = reader.ReadByte();
                extEnum = new List<T>();
                for (int i = 0; i < count; i++)
                {
                    var localIndex = OnlineManager.lobby.enumMap[typeof(T)].First(x => x.Item1 == reader.ReadByte()).Item2;
                    extEnum.Add((T)Activator.CreateInstance(typeof(T), new object[] { ExtEnum<T>.values.GetEntry(localIndex), false }));
                }
            }
        }
    }
}