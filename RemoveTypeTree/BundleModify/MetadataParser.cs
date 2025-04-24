// using UnityFS;

using K4os.Compression.LZ4;
using UnityFS;

namespace BundleCrafter
{
    public class MetadataParser
    {
        public StorageBlockInfoParser[] m_BlocksInfo;
        public NodeParser[] m_DirectoryInfo;
        private HeaderParser m_Header;
        private LZ4Level lz4Lv; // 不同的包压缩还不同，没有参数可读，目前是遍历测试一致

        /// <summary>
        /// 现在都是0
        /// </summary>
        public byte[] uncompressedDataHash;

        public int originCompressMetadataBytesLength;

        /// <summary>
        /// 可能需要变化, 因为某几个BlockInfo的CompressedBytes发生了变化
        /// </summary>
        public byte[] compressMetadataBytes_changable;

        public byte[] uncompressedBlocksInfoBytes;

        public MetadataParser(HeaderParser header)
        {
            this.m_Header = header;
        }

        public void Parse(EndianBinaryReader reader)
        {
            compressMetadataBytes_changable = ReadBlocksInfoAndDirectoryMetadataUnCompressedBytes(reader);
            var compressType = (CompressionType)(m_Header.flags & BundleArchiveFlags.CompressionTypeMask);
            uncompressedBlocksInfoBytes = CompressUtils.DecompressBytes(compressType, compressMetadataBytes_changable,
                m_Header.uncompressedBlocksInfoSize);

            //遍历枚举LZ4Level
            bool bFindLevel = false;
            foreach (LZ4Level value in Enum.GetValues(typeof(LZ4Level)))
            {
                int size = (int)m_Header.compressedBlocksInfoSize;
                var curTestComBytes = CompressUtils.CompressBytes(compressType, uncompressedBlocksInfoBytes, ref size, value);
                if (compressMetadataBytes_changable.Length != curTestComBytes.Length)
                    continue;

                bool bEqual = true;
                for (int i = 0; i < curTestComBytes.Length; i++)
                {
                    if (curTestComBytes[i] != compressMetadataBytes_changable[i])
                    {
                        bEqual = false;
                        break;
                        // Console.WriteLine($"compressTest: {compressType} isEqual:{false} {i}");
                    }
                }

                if (bEqual)
                {
                    lz4Lv = value;
                    bFindLevel = true;
                    break;
                }
            }

            if (!bFindLevel)
                throw new Exception("no find lz4 level");
            else
                Console.WriteLine($"header blocksinfo lz4 level: {lz4Lv}");

            MemoryStream metadataStream = new MemoryStream(uncompressedBlocksInfoBytes);
            using (var blocksInfoReader = new EndianBinaryReader(metadataStream))
            {
                uncompressedDataHash = blocksInfoReader.ReadBytes(16);
                var blocksInfoCount = blocksInfoReader.ReadInt32();
                m_BlocksInfo = new StorageBlockInfoParser[blocksInfoCount];
                for (int i = 0; i < blocksInfoCount; i++)
                {
                    m_BlocksInfo[i] = new StorageBlockInfoParser();
                    m_BlocksInfo[i].Parse(blocksInfoReader);
                }

                var nodesCount = blocksInfoReader.ReadInt32();
                m_DirectoryInfo = new NodeParser[nodesCount];
                for (int i = 0; i < nodesCount; i++)
                {
                    m_DirectoryInfo[i] = new NodeParser();
                    m_DirectoryInfo[i].Parse(blocksInfoReader);
                }
            }
            if (m_Header.flags.HasFlag(BundleArchiveFlags.BlockInfoNeedPaddingAtStart))
            {
                reader.AlignStream(16);
            }
        }

        private byte[] ReadBlocksInfoAndDirectoryMetadataUnCompressedBytes(EndianBinaryReader reader)
        {
            byte[] metadataUncompressBytes;
            if (m_Header.version >= 7)
            {
                reader.AlignStream(16);
            }
            if ((m_Header.flags & BundleArchiveFlags.BlocksInfoAtTheEnd) != 0)
            {
                //证明BlockInfo在最后
                //跳到最后去读, 再变回来
                var position = reader.Position;
                reader.Position = reader.BaseStream.Length - m_Header.compressedBlocksInfoSize;
                metadataUncompressBytes = reader.ReadBytes((int)m_Header.compressedBlocksInfoSize);
                reader.Position = position;
            }
            else if ((m_Header.flags & BundleArchiveFlags.BlocksAndDirectoryInfoCombined) != 0) //0x40 BlocksAndDirectoryInfoCombined
            {
                //证明Block和DirectoryInfo在一起
                metadataUncompressBytes = reader.ReadBytes((int)m_Header.compressedBlocksInfoSize);
            }
            else
            {
                throw new Exception("不支持的Header flags");
            }
            return metadataUncompressBytes;
        }

        void WriteBlocksInfoAndDirectoryMetadataUnCompressedBytes(EndianBinaryWriter writer, CompareStream compareStream)
        {
            if (m_Header.version >= 7)
            {
                compareStream.PauseAndAlign(16);
                writer.AlignStream(16);
                compareStream.Continue();
            }
            if ((m_Header.flags & BundleArchiveFlags.BlocksInfoAtTheEnd) != 0)
            {
                //证明BlockInfo在最后
                //跳到最后去读, 再变回来
                var position = writer.Position;

                compareStream.PauseAndReadBytes(originCompressMetadataBytesLength);
                writer.Write(compressMetadataBytes_changable);
                compareStream.Continue();

                writer.Position = position;
            }
            else if ((m_Header.flags & BundleArchiveFlags.BlocksAndDirectoryInfoCombined) != 0) //0x40 BlocksAndDirectoryInfoCombined
            {
                //证明Block和DirectoryInfo在一起
                compareStream.PauseAndReadBytes(originCompressMetadataBytesLength);
                writer.Write(compressMetadataBytes_changable);
                compareStream.Continue();

            }
            else
            {
                throw new Exception("不支持的Header flags");
            }
        }

        public void Calculate(byte[][] blockData, StreamFile[] fileList)
        {
            MemoryStream uncompressStream = new MemoryStream();
            foreach (var block in blockData)
            {
                uncompressStream.Write(block, 0, block.Length);
            }

            // uncompressedDataHash = HashUtils.ComputeHash(uncompressStream);
            //uncompressedDataHash该版本应该全0
            uncompressStream.Write(uncompressedDataHash, 0, uncompressedDataHash.Length);

            //MemoryStream metadataStream = new MemoryStream(CompressUtils.DecompressBytes((CompressionType)(m_Header.flags & ArchiveFlags.CompressionTypeMask), compressMetadataBytes, m_Header.uncompressedBlocksInfoSize));
            MemoryStream metadataStream = new MemoryStream();
            var compareStream = new CompareStream(metadataStream, uncompressedBlocksInfoBytes);
            //using (var blocksInfoReader = new EndianBinaryReader(metadataStream))
            using (var blocksInfoWriter = new EndianBinaryWriter(compareStream))
            {
                blocksInfoWriter.Write(uncompressedDataHash);
                // var blocksInfoCount = blocksInfoReader.ReadInt32();
                blocksInfoWriter.Write(m_BlocksInfo.Length);
                // m_BlocksInfo = new StorageBlockParser[blocksInfoCount];
                // for (int i = 0; i < blocksInfoCount; i++)
                for (int i = 0; i < m_BlocksInfo.Length; i++)
                {
                    m_BlocksInfo[i].Write(blocksInfoWriter, compareStream);
                }

                // var nodesCount = blocksInfoReader.ReadInt32();
                // m_DirectoryInfo = new NodeParser[nodesCount];
                blocksInfoWriter.Write(m_DirectoryInfo.Length);
                // for (int i = 0; i < nodesCount; i++)
                long offset = 0;
                for (int i = 0; i < m_DirectoryInfo.Length; i++)
                {
                    // m_DirectoryInfo[i] = new NodeParser();
                    // m_DirectoryInfo[i].Parse(blocksInfoReader);
                    m_DirectoryInfo[i].offset = offset;
                    m_DirectoryInfo[i].size = fileList[i].writeStream.Length;
                    m_DirectoryInfo[i].Write(blocksInfoWriter);
                    offset += fileList[i].writeStream.Length;
                }
            }

            originCompressMetadataBytesLength = compressMetadataBytes_changable.Length;
            uncompressedBlocksInfoBytes = metadataStream.ToArray();
            var compressType = (CompressionType)(m_Header.flags & BundleArchiveFlags.CompressionTypeMask);
            int size = 0;
            compressMetadataBytes_changable = CompressUtils.CompressBytes(compressType, uncompressedBlocksInfoBytes, ref size, lz4Lv);
            if (compressMetadataBytes_changable.Length != size)
            {
                byte[] comByte = new byte[size];
                Array.Copy(compressMetadataBytes_changable, 0, comByte, 0, size);
                compressMetadataBytes_changable = comByte;
            }

        }

        public void Write(EndianBinaryWriter writer, CompareStream compareStream)
        {
            // byte[] compressMetadataBytes = WriteBlocksInfoAndDirectoryMetadataUnCompressedBytes(writer);

            //需要重新计算metadataUncompressBytes
            WriteBlocksInfoAndDirectoryMetadataUnCompressedBytes(writer, compareStream);

            // writer.Write(compressMetadataBytes);
            // m_Header.uncompressedBlocksInfoSize = (uint)uncompressBytes.Length;

            if (m_Header.flags.HasFlag(BundleArchiveFlags.BlockInfoNeedPaddingAtStart))
            {
                compareStream.PauseAndAlign(16);
                writer.AlignStream(16);
                compareStream.Continue();
            }
        }


        public long GetCompressMetaSize()
        {
            return compressMetadataBytes_changable.LongLength;
        }
    }
}