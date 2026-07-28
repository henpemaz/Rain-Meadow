using Ionic.Zlib;
using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.None)]
    public class DeflateState : OnlineState
    {
        public class LongBytesFieldAttribute : OnlineFieldAttribute
        {
            public override Expression SerializerCallMethod(FieldInfo f, Expression serializerRef, Expression fieldRef)
            {
                return Expression.Call(serializerRef, typeof(Serializer).GetMethod(nameof(Serializer.SerializeLongArray)), fieldRef);
            }
        }
        [LongBytesField]
        public byte[] bytes;

        public DeflateState() { }
        public DeflateState(Stream input, int len)
        {
            this.bytes = Compress(input, len);
        }

        public void Decompress(Stream into)
        {
            using (var compressStream = new MemoryStream(bytes))
            using (var decompressor = new DeflateStream(compressStream, CompressionMode.Decompress))
                decompressor.CopyTo(into);
        }

        private static byte[] Compress(Stream input, int len)
        {
            using (var compressStream = new MemoryStream())
            {
                using (var compressor = new DeflateStream(compressStream, CompressionMode.Compress))
                {


                    var chunk = new byte[8192];
                    int remaining = len;
                    while (remaining > 0)
                    {
                        int read = input.Read(chunk, 0, Math.Min(chunk.Length, remaining));
                        if (read <= 0) break;
                        compressor.Write(chunk, 0, read);
                        remaining -= read;
                    }
                }
                return compressStream.ToArray();
            }
        }
    }
}