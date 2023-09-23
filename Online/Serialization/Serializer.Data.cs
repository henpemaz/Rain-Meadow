using RWCustom;
using System.Collections.Generic;
using UnityEngine;
using static RainMeadow.Serializer;

namespace RainMeadow
{
    public partial class Serializer
    {

        public void Serialize(ref byte data)
        {
            if (IsWriting) writer.Write(data);
            if (IsReading) data = reader.ReadByte();
        }

        public void Serialize(ref byte[] data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (IsReading)
            {
                data = new byte[reader.ReadByte()];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = reader.ReadByte();
                }
            }
        }

        public void Serialize(ref List<byte> data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (IsReading)
            {
                var count = reader.ReadByte();
                data = new(count);
                for (int i = 0; i < count; i++)
                {
                    data.Add(reader.ReadByte());
                }
            }
        }

        public void Serialize(ref sbyte data)
        {
            if (IsWriting) writer.Write(data);
            if (IsReading) data = reader.ReadSByte();
        }

        public void Serialize(ref sbyte[] data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (IsReading)
            {
                data = new sbyte[reader.ReadByte()];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = reader.ReadSByte();
                }
            }
        }

        public void Serialize(ref List<sbyte> data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (IsReading)
            {
                var count = reader.ReadByte();
                data = new(count);
                for (int i = 0; i < count; i++)
                {
                    data.Add(reader.ReadSByte());
                }
            }
        }

        public void Serialize(ref ushort data)
        {
            if (IsWriting) writer.Write(data);
            if (IsReading) data = reader.ReadUInt16();
        }

        public void Serialize(ref ushort[] data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (IsReading)
            {
                data = new ushort[reader.ReadByte()];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = reader.ReadUInt16();
                }
            }
        }

        public void Serialize(ref List<ushort> data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (IsReading)
            {
                var count = reader.ReadByte();
                data = new(count);
                for (int i = 0; i < count; i++)
                {
                    data.Add(reader.ReadUInt16());
                }
            }
        }

        public void Serialize(ref short data)
        {
            if (IsWriting) writer.Write(data);
            if (IsReading) data = reader.ReadInt16();
        }

        public void Serialize(ref short[] data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (IsReading)
            {
                data = new short[reader.ReadByte()];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = reader.ReadInt16();
                }
            }
        }

        public void Serialize(ref List<short> data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (IsReading)
            {
                var count = reader.ReadByte();
                data = new(count);
                for (int i = 0; i < count; i++)
                {
                    data.Add(reader.ReadInt16());
                }
            }
        }

        public void Serialize(ref int data)
        {
            if (IsWriting) writer.Write(data);
            if (IsReading) data = reader.ReadInt32();
        }

        public void Serialize(ref int[] data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (IsReading)
            {
                data = new int[reader.ReadByte()];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = reader.ReadInt32();
                }
            }
        }

        public void Serialize(ref List<int> data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (IsReading)
            {
                var count = reader.ReadByte();
                data = new(count);
                for (int i = 0; i < count; i++)
                {
                    data.Add(reader.ReadInt32());
                }
            }
        }

        public void Serialize(ref uint data)
        {
            if (IsWriting) writer.Write(data);
            if (IsReading) data = reader.ReadUInt32();
        }

        public void Serialize(ref uint[] data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (IsReading)
            {
                data = new uint[reader.ReadByte()];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = reader.ReadUInt32();
                }
            }
        }

        public void Serialize(ref List<uint> data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (IsReading)
            {
                var count = reader.ReadByte();
                data = new(count);
                for (int i = 0; i < count; i++)
                {
                    data.Add(reader.ReadUInt32());
                }
            }
        }

        public void Serialize(ref bool data)
        {
            if (IsWriting) writer.Write(data);
            if (IsReading) data = reader.ReadBoolean();
        }

        public void Serialize(ref bool[] data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (IsReading)
            {
                data = new bool[reader.ReadByte()];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = reader.ReadBoolean();
                }
            }
        }

        public void Serialize(ref List<bool> data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (IsReading)
            {
                var count = reader.ReadByte();
                data = new(count);
                for (int i = 0; i < count; i++)
                {
                    data.Add(reader.ReadBoolean());
                }
            }
        }

        public void Serialize(ref ulong data)
        {
            if (IsWriting) writer.Write(data);
            if (IsReading) data = reader.ReadUInt64();
        }

        public void Serialize(ref ulong[] data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (IsReading)
            {
                data = new ulong[reader.ReadByte()];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = reader.ReadUInt64();
                }
            }
        }

        public void Serialize(ref List<ulong> data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (IsReading)
            {
                var count = reader.ReadByte();
                data = new(count);
                for (int i = 0; i < count; i++)
                {
                    data.Add(reader.ReadUInt64());
                }
            }
        }

        public void Serialize(ref float data)
        {
            if (IsWriting) writer.Write(data);
            if (IsReading) data = reader.ReadSingle();
        }

        public void Serialize(ref float[] data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (IsReading)
            {
                data = new float[reader.ReadByte()];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = reader.ReadSingle();
                }
            }
        }

        public void Serialize(ref List<float> data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (IsReading)
            {
                var count = reader.ReadByte();
                data = new(count);
                for (int i = 0; i < count; i++)
                {
                    data.Add(reader.ReadSingle());
                }
            }
        }

        public void SerializeHalf(ref float data)
        {
            if (IsWriting) writer.Write(Mathf.FloatToHalf(data));
            if (IsReading) data = Mathf.HalfToFloat(reader.ReadUInt16());
        }

        public void SerializeHalf(ref float[] data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(Mathf.FloatToHalf(data[i]));
                }
            }
            if (IsReading)
            {
                data = new float[reader.ReadByte()];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = Mathf.HalfToFloat(reader.ReadUInt16());
                }
            }
        }

        public void SerializeHalf(ref List<float> data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(Mathf.FloatToHalf(data[i]));
                }
            }
            if (IsReading)
            {
                var count = reader.ReadByte();
                data = new(count);
                for (int i = 0; i < count; i++)
                {
                    data.Add(Mathf.HalfToFloat(reader.ReadUInt16()));
                }
            }
        }

        public void Serialize(ref string data)
        {
            if (IsWriting) writer.Write(data);
            if (IsReading) data = reader.ReadString();
        }

        public void SerializeNullable(ref string data) {
            if (IsWriting)
            {
                writer.Write(data != null);
                if (data != null)
                {
                    writer.Write(data);
                }
            }
            if (IsReading)
            {
                if (reader.ReadBoolean())
                {
                    data = reader.ReadString();
                }
            }
        }

        public void Serialize(ref string[] data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (IsReading)
            {
                data = new string[reader.ReadByte()];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = reader.ReadString();
                }
            }
        }

        public void Serialize(ref List<string> data)
        {
            if (IsWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (IsReading)
            {
                var count = reader.ReadByte();
                data = new(count);
                for (int i = 0; i < count; i++)
                {
                    data.Add(reader.ReadString());
                }
            }
        }

        public void SerializeNullable(ref List<string> data) 
        {
            if (IsWriting)
            {
                writer.Write(data != null);
                if (data != null)
                {
                    writer.Write((byte)data.Count);
                    for (int i = 0; i < data.Count; i++)
                    {
                        writer.Write(data[i]);
                    }
                }
            }
            if (IsReading)
            {
                if (reader.ReadBoolean())
                {
                    var count = reader.ReadByte();
                    data = new(count);
                    for (int i = 0; i < count; i++)
                    {
                        data.Add(reader.ReadString());
                    }
                }
            }
        }


        // Todo make Half version for these as well see SerializeHalf(float)
        public void Serialize(ref Vector2 data)
        {
            if (IsWriting)
            {
                writer.Write(data.x);
                writer.Write(data.y);
            }
            if (IsReading)
            {
                data.x = reader.ReadSingle();
                data.y = reader.ReadSingle();
            }
        }

        public void SerializeNullable(ref Vector2? data)
        {
            if (IsWriting)
            {
                writer.Write(data.HasValue);
                if (data.HasValue)
                {
                    writer.Write(data.Value.x);
                    writer.Write(data.Value.y);
                }
            }
            if (IsReading)
            {
                if (reader.ReadBoolean())
                {
                    data = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                }
            }
        }

        // We cast to shorts as we assume that
        // all IntVector2 represent tile coordinates.
        // Sorry if this caused a bug, yell at DevLope and Henpemaz.
        public void Serialize(ref IntVector2 data)
        {
            if (IsWriting)
            {
                writer.Write((short)data.x);
                writer.Write((short)data.y);
            }
            if (IsReading)
            {
                data.x = reader.ReadInt16();
                data.y = reader.ReadInt16();
            }
        }

        public void Serialize(ref WorldCoordinate pos)
        {
            if (IsWriting)
            {
                writer.Write((short)pos.room);
                writer.Write((short)pos.x);
                writer.Write((short)pos.y);
                writer.Write((short)pos.abstractNode);
            }
            if (IsReading)
            {
                pos = new WorldCoordinate()
                {
                    room = reader.ReadInt16(),
                    x = reader.ReadInt16(),
                    y = reader.ReadInt16(),
                    abstractNode = reader.ReadInt16(),
                };
            }
        }

        public void SerializeNullable(ref WorldCoordinate? pos)
        {
            if (IsWriting)
            {
                writer.Write(pos.HasValue);
                if (pos.HasValue)
                {
                    writer.Write((short)pos.Value.room);
                    writer.Write((short)pos.Value.x);
                    writer.Write((short)pos.Value.y);
                    writer.Write((short)pos.Value.abstractNode);
                }
            }
            if (IsReading)
            {
                if (reader.ReadBoolean())
                {
                    pos = new WorldCoordinate()
                    {
                        room = reader.ReadInt16(),
                        x = reader.ReadInt16(),
                        y = reader.ReadInt16(),
                        abstractNode = reader.ReadInt16(),
                    };
                }
            }
        }
    }
}