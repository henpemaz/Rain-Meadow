using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace RainMeadow
{
    public partial class Serializer
    {

        public void Serialize(ref byte data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadByte();
        }

        public void Serialize(ref byte[] data)
        {
            if (isWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (isReading)
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
            if (isWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (isReading)
            {
                data = new(reader.ReadByte());
                for (int i = 0; i < data.Count; i++)
                {
                    data[i] = reader.ReadByte();
                }
            }
        }

        public void Serialize(ref sbyte data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadSByte();
        }

        public void Serialize(ref sbyte[] data)
        {
            if (isWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (isReading)
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
            if (isWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (isReading)
            {
                data = new(reader.ReadByte());
                for (int i = 0; i < data.Count; i++)
                {
                    data[i] = reader.ReadSByte();
                }
            }
        }

        public void Serialize(ref ushort data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadUInt16();
        }

        public void Serialize(ref ushort[] data)
        {
            if (isWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (isReading)
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
            if (isWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (isReading)
            {
                data = new(reader.ReadByte());
                for (int i = 0; i < data.Count; i++)
                {
                    data[i] = reader.ReadUInt16();
                }
            }
        }
        
        public void Serialize(ref short data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadInt16();
        }

        public void Serialize(ref short[] data)
        {
            if (isWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (isReading)
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
            if (isWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (isReading)
            {
                data = new(reader.ReadByte());
                for (int i = 0; i < data.Count; i++)
                {
                    data[i] = reader.ReadInt16();
                }
            }
        }

        public void Serialize(ref int data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadInt32();
        }

        public void Serialize(ref int[] data)
        {
            if (isWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (isReading)
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
            if (isWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (isReading)
            {
                data = new(reader.ReadByte());
                for (int i = 0; i < data.Count; i++)
                {
                    data[i] = reader.ReadInt32();
                }
            }
        }

        public void Serialize(ref uint data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadUInt32();
        }

        public void Serialize(ref uint[] data)
        {
            if (isWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (isReading)
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
            if (isWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (isReading)
            {
                data = new(reader.ReadByte());
                for (int i = 0; i < data.Count; i++)
                {
                    data[i] = reader.ReadUInt32();
                }
            }
        }

        public void Serialize(ref bool data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadBoolean();
        }

        public void Serialize(ref bool[] data)
        {
            if (isWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (isReading)
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
            if (isWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (isReading)
            {
                data = new(reader.ReadByte());
                for (int i = 0; i < data.Count; i++)
                {
                    data[i] = reader.ReadBoolean();
                }
            }
        }

        public void Serialize(ref ulong data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadUInt64();
        }

        public void Serialize(ref ulong[] data)
        {
            if (isWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (isReading)
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
            if (isWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (isReading)
            {
                data = new(reader.ReadByte());
                for (int i = 0; i < data.Count; i++)
                {
                    data[i] = reader.ReadUInt64();
                }
            }
        }

        public void Serialize(ref float data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadSingle();
        }

        public void Serialize(ref float[] data)
        {
            if (isWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (isReading)
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
            if (isWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (isReading)
            {
                data = new(reader.ReadByte());
                for (int i = 0; i < data.Count; i++)
                {
                    data[i] = reader.ReadSingle();
                }
            }
        }

        public void Serialize(ref string data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadString();
        }

        public void Serialize(ref string[] data)
        {
            if (isWriting)
            {
                writer.Write((byte)data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (isReading)
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
            if (isWriting)
            {
                writer.Write((byte)data.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    writer.Write(data[i]);
                }
            }
            if (isReading)
            {
                data = new(reader.ReadByte());
                for (int i = 0; i < data.Count; i++)
                {
                    data[i] = reader.ReadString();
                }
            }
        }

        public void Serialize(ref Vector2 data)
        {
            if (isWriting)
            {
                writer.Write(data.x);
                writer.Write(data.y);
            }
            if (isReading)
            {
                data.x = reader.ReadSingle();
                data.y = reader.ReadSingle();
            }
        }

        public void Serialize(ref Vector2? data)
        {
            if (isWriting)
            {
                writer.Write(data.HasValue);
                if (data.HasValue)
                {
                    writer.Write(data.Value.x);
                    writer.Write(data.Value.y);
                }
            }
            if (isReading)
            {
                if (reader.ReadBoolean())
                {
                    data = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                }
            }
        }

        public void SerializeNoStrings(ref WorldCoordinate pos)
        {
            if (isWriting)
            {
                writer.Write((short)pos.room);
                writer.Write((short)pos.x);
                writer.Write((short)pos.y);
                writer.Write((short)pos.abstractNode);
            }
            if (isReading)
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