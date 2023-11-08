using Newtonsoft.Json.Linq;
using System;

namespace RainMeadow
{
    public partial class Serializer
    {
        // todo make this load-order independent. How? map strings to indexes somewhere else don't rely on the .Index
        public void SerializeExtEnum<T>(ref T extEnum) where T : ExtEnum<T>
        {
            if (IsWriting)
            {
                writer.Write((byte)extEnum.Index);
            }
            if (IsReading)
            {
                extEnum = (T)Activator.CreateInstance(typeof(T), new object[] { ExtEnum<T>.values.GetEntry(reader.ReadByte()) , false});
            }
        }
    }
}