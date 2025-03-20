using System.Linq.Expressions;
using System.Reflection;
using System.IO;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.None)]
    internal class BrotliState : OnlineState
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

        public BrotliState() { }
        public BrotliState(Stream input, int len)
        {
            this.bytes = Compress(input, len);
        }

        public void Decompress(Stream into)
        {
            using (var compressStream = new MemoryStream(bytes))
            using (var decompressor = new Brotli.BrotliStream(compressStream, System.IO.Compression.CompressionMode.Decompress))
                decompressor.CopyTo(into);
        }

        private static byte[] Compress(Stream input, int len)
        {
            using (var compressStream = new MemoryStream())
            using (var compressor = new Brotli.BrotliStream(compressStream, System.IO.Compression.CompressionMode.Compress))
            {
                input.CopyTo(compressor, len);
                compressor.Close();
                return compressStream.ToArray();
            }
        }
    }
}