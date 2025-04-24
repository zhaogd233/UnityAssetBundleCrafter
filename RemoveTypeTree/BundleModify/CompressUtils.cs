using K4os.Compression.LZ4;
using SevenZip.Compression.LZMA;
using UnityFS;

namespace BundleCrafter
{
    public class CompressUtils
    {
        public static byte[] DecompressBytes(CompressionType compressionType, byte[] compressedBytes, uint uncompressedSize)
        {
            switch (compressionType)
            {
                case CompressionType.None:
                    {
                        return compressedBytes;
                    }
                case CompressionType.Lzma:
                    {
                        var uncompressedStream = new MemoryStream((int)(uncompressedSize));
                        using (var compressedStream = new MemoryStream(compressedBytes))
                        {
                            // 修正原HybridCLR方法实现, 原写死的Header BlockSize
                            // ComparessHelper.Decompress7Zip(compressedStream, uncompressedStream, m_Header.compressedBlocksInfoSize, m_Header.uncompressedBlocksInfoSize);
                            Decompress7Zip(compressedStream, uncompressedStream, compressedBytes.Length, uncompressedSize);
                        }
                        return uncompressedStream.ReadAllBytes();
                    }
                case CompressionType.Lz4:
                case CompressionType.Lz4HC:
                    {
                        var uncompressedBytes = new byte[uncompressedSize];
                        // var numWrite = LZ4.LZ4Codec.Decode(compressedBytes, 0, compressedBytes.Length, uncompressedBytes, 0, uncompressedBytes.Length, true);
                        var numWrite = LZ4Codec.Decode(compressedBytes, 0, compressedBytes.Length, uncompressedBytes, 0, uncompressedBytes.Length);
                        if (numWrite != uncompressedSize)
                        {
                            throw new IOException($"Lz4 decompression error, write {numWrite} bytes but expected {uncompressedSize} bytes");
                        }
                        return uncompressedBytes;
                    }
                default:
                    throw new IOException($"Unsupported compression type {compressionType}");
            }
        }

        public static byte[] CompressBytes(CompressionType compressionType, byte[] uncompressBytes, ref int compressSize, LZ4Level level = LZ4Level.L03_HC)
        {
            switch (compressionType)
            {
                case CompressionType.None:
                    {
                        return uncompressBytes;
                    }
                case CompressionType.Lzma:
                    {
                        var compressedStream = new MemoryStream(compressSize);
                        using (var uncompressedStream = new MemoryStream(uncompressBytes))
                        {
                            // 修正原HybridCLR方法实现, 原写死的Header BlockSize
                            // ComparessHelper.Decompress7Zip(compressedStream, uncompressedStream, m_Header.compressedBlocksInfoSize, m_Header.uncompressedBlocksInfoSize);
                            Compress7Zip(compressedStream, uncompressedStream, compressSize, uncompressBytes.Length);
                        }
                        return compressedStream.ReadAllBytes();
                    }
                case CompressionType.Lz4:
                case CompressionType.Lz4HC:
                    {
                        if (compressSize == 0)
                            compressSize = LZ4Codec.MaximumOutputSize(uncompressBytes.Length);
                        byte[] compressBytes = new byte[compressSize];
                        compressSize = LZ4Codec.Encode(uncompressBytes, 0, uncompressBytes.Length, compressBytes, 0, compressSize, level);
                        return compressBytes;
                        // return LZ4.LZ4Codec.Encode(uncompressBytes, 0, uncompressBytes.Length);
                        // var uncompressedBytes = new byte[uncompressedSize];
                        // var numWrite = LZ4Codec.Decode(compressedBytes, 0, compressedBytes.Length, uncompressedBytes, 0, uncompressedBytes.Length, true);
                        // if (numWrite != uncompressedSize)
                        // {
                        //     throw new IOException($"Lz4 decompression error, write {numWrite} bytes but expected {uncompressedSize} bytes");
                        // }
                        // return uncompressedBytes;
                    }
                default:
                    throw new IOException($"Unsupported compression type {compressionType}");
            }
        }

        public static void Decompress7Zip(Stream compressedStream, Stream decompressedStream, long compressedSize, long decompressedSize)
        {
            var basePosition = compressedStream.Position;
            var decoder = new Decoder();
            var properties = new byte[5];
            if (compressedStream.Read(properties, 0, 5) != 5)
                throw new Exception("input .lzma is too short");
            decoder.SetDecoderProperties(properties);
            decoder.Code(compressedStream, decompressedStream, compressedSize - 5, decompressedSize, null);
            compressedStream.Position = basePosition + compressedSize;
        }

        public static void Compress7Zip(Stream compressedStream, Stream uncompressedStream, long compressedSize, long uncompressedSize)
        {
            var basePosition = compressedStream.Position;
            // var properties = new byte[5];
            // if (compressedStream.Read(properties, 0, 5) != 5)
            //     throw new Exception("input .lzma is too short");

            var encoder = new Encoder();
            encoder.WriteCoderProperties(compressedStream);
            compressedStream.Position = 5;

            encoder.Code(uncompressedStream, compressedStream, uncompressedSize, compressedSize - 5, null);
            compressedStream.Flush();

            compressedStream.Position = basePosition + compressedSize;
        }
    }
}