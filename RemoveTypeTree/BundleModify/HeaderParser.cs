using UnityFS;

namespace BundleCrafter
{
    public class HeaderParser
    {
        public string signature;
        public uint version;
        public string unityVersion;
        public string unityRevision;
        /// <summary>
        /// 文件长度
        /// 可能需要变化, 因为总文件长度发生了变化
        /// </summary>
        public long size;
        /// <summary>
        /// BlockInfo压缩后的大小, BlockInfo可能在Bundle的最后
        /// 可能需要变化, 因为BlockInfo发生了变化后压缩大小可能变化
        /// </summary>
        public uint compressedBlocksInfoSize;
        /// <summary>
        /// 解压后Bundle大小
        /// </summary>
        public uint uncompressedBlocksInfoSize;
        /// <summary>
        /// 压缩Flags
        /// </summary>
        public BundleArchiveFlags flags;

        private byte unusedByte;

        public void Parse(EndianBinaryReader reader)
        {
            signature = reader.ReadStringToNull();
            version = reader.ReadUInt32();
            unityVersion = reader.ReadStringToNull();
            unityRevision = reader.ReadStringToNull();
            System.Diagnostics.Debug.Assert(signature == "UnityFS");

            size = reader.ReadInt64();
            //Console.WriteLine($"header size:{size}");
            compressedBlocksInfoSize = reader.ReadUInt32();
            uncompressedBlocksInfoSize = reader.ReadUInt32();
            flags = (BundleArchiveFlags)reader.ReadUInt32();
            if (signature != "UnityFS")
            {
                unusedByte = reader.ReadByte();
            }
        }

        public void Write(EndianBinaryWriter writer, CompareStream compareStream)
        {
            // signature = reader.ReadStringToNull();
            writer.WriteNullEndString(signature);
            // version = reader.ReadUInt32();
            writer.Write(version);
            // unityVersion = reader.ReadStringToNull();
            writer.WriteNullEndString(unityVersion);
            // unityRevision = reader.ReadStringToNull();
            writer.WriteNullEndString(unityRevision);
            // System.Diagnostics.Debug.Assert(signature == "UnityFS");

            // size = reader.ReadInt64();
            compareStream.PauseAndRead(size);
            writer.Write(size);
            compareStream.Continue();
            // UnityEngine.Console.WriteLine($"header size:{size}");
            // compressedBlocksInfoSize = reader.ReadUInt32();
            compareStream.PauseAndRead(compressedBlocksInfoSize);
            writer.Write(compressedBlocksInfoSize);
            compareStream.Continue();
            // uncompressedBlocksInfoSize = reader.ReadUInt32();
            writer.Write(uncompressedBlocksInfoSize);
            // flags = (ArchiveFlags)reader.ReadUInt32();
            writer.Write((uint)flags);
            if (signature != "UnityFS")
            {
                // reader.ReadByte();
                writer.Write(unusedByte);
            }
        }



        public void Calculate(byte[] metaPaserCompressMetadataBytes, byte[] metaPaserUncompressedBlocksInfoBytes)
        {
            compressedBlocksInfoSize = (uint)metaPaserCompressMetadataBytes.Length;
            uncompressedBlocksInfoSize = (uint)metaPaserUncompressedBlocksInfoBytes.Length;
        }
    }
}