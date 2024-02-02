using System.IO;
using UnityEngine;
using System.Linq.Expressions;
using System.Reflection;
using Ionic.Zlib;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.None)]
    internal class DeflateState : OnlineState
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

        public void Decompress(Stream into) {
            using (var compressStream = new MemoryStream(bytes))
            using (var decompressor = new DeflateStream(compressStream, CompressionMode.Decompress))
                decompressor.CopyTo(into);
        }

        private static byte[] Compress(Stream input, int len)
        {
            using (var compressStream = new MemoryStream())
            using (var compressor = new DeflateStream(compressStream, CompressionMode.Compress))
            {
                input.CopyTo(compressor, len);
                compressor.Close();
                return compressStream.ToArray();
            }
        }
    }
}