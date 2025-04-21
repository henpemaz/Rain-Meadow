using Ionic.Zlib;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using K4os.Compression.LZ4.Streams;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.None)]
    internal class Lz4CompressedState : OnlineState
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

        public Lz4CompressedState() { }
        public Lz4CompressedState(Stream input, int len)
        {
            this.bytes = Compress(input, len);
        }

        public void Decompress(Stream into)
        {
            using (var compressStream = new MemoryStream(bytes))
            using (var decompressor = LZ4Stream.Decode(compressStream))
                decompressor.CopyTo(into);
        }

        private static byte[] Compress(Stream input, int len)
        {
            using (var compressStream = new MemoryStream())
            using (var compressor = LZ4Stream.Encode(compressStream))
            {
                input.CopyTo(compressor, len);
                compressor.Close();
                return compressStream.ToArray();
            }
        }
    }
}