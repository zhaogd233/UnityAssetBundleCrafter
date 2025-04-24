using K4os.Compression.LZ4;
using UnityFS;

namespace BundleCrafter
{
    public class BlockStreamParser
    {
        private MetadataParser metaPaser;
        public StreamFile[] fileList;
        public byte[][] blockData;
        public LZ4Level lv4Lv;

        public long CalcualteBlockDataSize()
        {
            long size = 0;
            foreach (var block in blockData)
            {
                size += block.LongLength;
            }

            return size;
        }

        public BlockStreamParser(MetadataParser metaParser)
        {
            this.metaPaser = metaParser;
        }

        public void Parse(EndianBinaryReader reader)
        {
            using (var uncompressBlocksStream = CreateBlocksStream())
            {
                ReadBlocks(reader, uncompressBlocksStream);
                ReadFiles(uncompressBlocksStream);
            }
        }

        private Stream CreateBlocksStream()
        {
            Stream blocksStream;
            var uncompressedSizeSum = metaPaser.m_BlocksInfo.Sum(x => x.uncompressedSize);
            if (uncompressedSizeSum >= int.MaxValue)
            {
                throw new Exception($"too fig file");
            }
            else
            {
                blocksStream = new MemoryStream((int)uncompressedSizeSum);
            }
            return blocksStream;
        }

        public void ReadFiles(Stream blocksStream)
        {
            fileList = new StreamFile[metaPaser.m_DirectoryInfo.Length];
            for (int i = 0; i < metaPaser.m_DirectoryInfo.Length; i++)
            {
                var node = metaPaser.m_DirectoryInfo[i];
                var file = new StreamFile();
                fileList[i] = file;
                file.path = node.path;
                file.writeStream = new MemoryStream();
                file.fileName = Path.GetFileName(node.path);
                if (node.size >= int.MaxValue)
                {
                    throw new Exception($"exceed max file size");
                    /*var memoryMappedFile = MemoryMappedFile.CreateNew(null, entryinfo_size);
                    file.stream = memoryMappedFile.CreateViewStream();*/
                    //var extractPath = path + "_unpacked" + Path.DirectorySeparatorChar;
                    //Directory.CreateDirectory(extractPath);
                    //file.stream = new FileStream(extractPath + file.fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                }
                file.stream = new MemoryStream((int)node.size);
                blocksStream.Position = node.offset;
                blocksStream.CopyTo(file.stream, node.size);
                file.stream.Position = 0;
            }
        }

        public void Write(EndianBinaryWriter writer, CompareStream compareStream)
        {
            for (var index = 0; index < metaPaser.m_BlocksInfo.Length; index++)
            {
                var blockInfo = metaPaser.m_BlocksInfo[index];
                // var compressedSize = (int) blockInfo.compressedSize;
                if (blockInfo.isChanged)
                {
                    compareStream.PauseAndReadBytes((int)blockInfo.originCompressSize);
                }

                writer.Write(blockData[index]);

                if (blockInfo.isChanged)
                {
                    compareStream.Continue();
                }

                // byte[] compressedBlockBytes = reader.ReadBytes(compressedSize);
                // var compressionType = (CompressionType) (blockInfo.flags & StorageBlockFlags.CompressionTypeMask);
                // byte[] uncompressedBlockBytes = CompressUtils.DecompressBytes(compressionType, compressedBlockBytes,
                //     blockInfo.uncompressedSize);
                // blocksStream.Write(uncompressedBlockBytes, 0, uncompressedBlockBytes.Length);
                //
                // blockData[index] = compressedBlockBytes;
            }
        }

        private void ReadBlocks(EndianBinaryReader reader, Stream blocksStream)
        {
            blockData = new byte[metaPaser.m_BlocksInfo.Length][];
            for (var index = 0; index < metaPaser.m_BlocksInfo.Length; index++)
            {
                var blockInfo = metaPaser.m_BlocksInfo[index];
                var compressedSize = (int)blockInfo.compressedSize;
                byte[] compressedBlockBytes = reader.ReadBytes(compressedSize);
                var compressionType = (CompressionType)(blockInfo.flags & StorageBlockFlags.CompressionTypeMask);
                byte[] uncompressedBlockBytes = CompressUtils.DecompressBytes(compressionType, compressedBlockBytes,
                    blockInfo.uncompressedSize);

                //遍历枚举LZ4Level
                bool bFindLevel = false;
                foreach (LZ4Level value in Enum.GetValues(typeof(LZ4Level)))
                {
                    int needComSize = compressedSize;
                    var curTestComBytes = CompressUtils.CompressBytes(compressionType, uncompressedBlockBytes, ref needComSize, value);
                    if (compressedSize < 0 || compressedBlockBytes.Length != curTestComBytes.Length)
                        continue;

                    bool bEqual = true;
                    for (int i = 0; i < curTestComBytes.Length; i++)
                    {
                        if (curTestComBytes[i] != compressedBlockBytes[i])
                        {
                            bEqual = false;
                            break;
                            // Console.WriteLine($"compressTest: {compressType} isEqual:{false} {i}");
                        }
                    }

                    if (bEqual)
                    {
                        lv4Lv = value;
                        bFindLevel = true;
                        break;
                    }
                }
                if (!bFindLevel)
                    throw new Exception("no find lz4 level");
                else
                    Console.WriteLine($"meta blocksinfo lz4 level: {lv4Lv}");

                blocksStream.Write(uncompressedBlockBytes, 0, uncompressedBlockBytes.Length);

                blockData[index] = compressedBlockBytes;
            }

            blocksStream.Position = 0;
        }

        public void Calculate()
        {
            MemoryStream memStream = new MemoryStream();
            foreach (var streamFile in fileList)
            {
                streamFile.writeStream.Position = 0;
                streamFile.writeStream.CopyTo(memStream);
            }

            var uncompressBytes = memStream.ToArray();

            //向上取整，每个block 128k
            // 128k = 128 * 1024
            int size = (uncompressBytes.Length + 131071) / (128 * 1024);
            // 移除操作，只可能少
            if (metaPaser.m_BlocksInfo.Length != size)
            {
                var orginBlockInfos = metaPaser.m_BlocksInfo;
                var originBlockData = blockData;

                metaPaser.m_BlocksInfo = new StorageBlockInfoParser[size];
                blockData = new byte[size][];

                Array.Copy(orginBlockInfos, 0, metaPaser.m_BlocksInfo, 0, size);
                Array.Copy(originBlockData, 0, blockData, 0, size);
            }

            long offset = 0;
            int leftSize = uncompressBytes.Length;
            for (var index = 0; index < metaPaser.m_BlocksInfo.Length; index++)
            {
                var blockInfo = metaPaser.m_BlocksInfo[index];
                /* if (!blockInfo.isChanged)
                 {
                     offset += blockInfo.uncompressedSize;
                     continue;
                 }*/

                int leftUncompressSize = Math.Min(131072, leftSize);
                byte[] uncompressBlockBytes = new byte[leftUncompressSize];
                for (long i = 0; i < leftUncompressSize; i++)
                {
                    uncompressBlockBytes[i] = uncompressBytes[offset + i];
                }
                var compressionType = (CompressionType)(blockInfo.flags & StorageBlockFlags.CompressionTypeMask);
                int compareSize = 0;
                blockData[index] = CompressUtils.CompressBytes(compressionType, uncompressBlockBytes, ref compareSize, lv4Lv);

                if (blockData[index].Length != compareSize)
                {
                    byte[] comByte = new byte[compareSize];
                    Array.Copy(blockData[index], 0, comByte, 0, compareSize);
                    blockData[index] = comByte;
                }
                metaPaser.m_BlocksInfo[index].SetCompressSize((uint)compareSize);
                metaPaser.m_BlocksInfo[index].uncompressedSize = (uint)leftUncompressSize;
                metaPaser.m_BlocksInfo[index].isChanged = true;
                offset += blockInfo.uncompressedSize;
                leftSize = leftSize - leftUncompressSize;
            }

        }
    }
}