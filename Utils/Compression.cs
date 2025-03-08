using System;
using System.IO;
using System.IO.Compression;

namespace RainMeadow;

public static class Compression
{
    /// <summary>
    /// Compresses the input bytes using a DeflateStream.
    /// </summary>
    /// <param name="bytes">The input, uncompressed bytes.</param>
    /// <returns>An array of compressed bytes.</returns>
    public static byte[] CompressBytes(byte[] bytes)
    {
        if (bytes.Length == 0) return bytes;
        try
        {
            using (MemoryStream inputStream = new(bytes))
            using (MemoryStream compressStream = new())
            using (DeflateStream compressor = new(compressStream, CompressionMode.Compress))
            {
                inputStream.CopyTo(compressor, bytes.Length);
                compressor.Close();

                var output = compressStream.ToArray();
                //if (bytes.Length > 128) //don't spam me with useless debug messages please
                RainMeadow.Debug($"Compressed {bytes.Length} bytes into {output.Length} bytes.");
                return output;
            }
        }
        catch (Exception ex)
        {
            RainMeadow.Error(ex);
        }
        return bytes; //fallback: just return the input
    }

    /// <summary>
    /// Decompresses the input bytes using a DeflateStream.
    /// </summary>
    /// <param name="bytes">The input, compressed bytes.</param>
    /// <returns>An array of uncompressed bytes.</returns>
    public static byte[] DecompressBytes(byte[] bytes)
    {
        if (bytes.Length == 0) return bytes;
        try
        {
            using (MemoryStream outputStream = new())
            using (MemoryStream compressedStream = new(bytes))
            using (DeflateStream decompressor = new(compressedStream, CompressionMode.Decompress))
            {
                decompressor.CopyTo(outputStream);
                decompressor.Close();

                var output = outputStream.ToArray();
                //if (output.Length > 128) //don't spam me with useless debug messages please
                RainMeadow.Debug($"Decompressed {bytes.Length} bytes into {output.Length} bytes.");
                return output;
            }
        }
        catch (Exception ex)
        {
            RainMeadow.Error(ex);
        }
        return bytes; //fallback: just return the input
    }
}
