using UnityFS;

namespace BundleCrafter
{
    public class StorageBlockInfoParser
    {
        /// <summary>
        /// 可能需要变化, 根本变化
        /// </summary>
        public uint compressedSize;
        public uint uncompressedSize;
        public StorageBlockFlags flags;
        public bool isChanged;
        public uint originCompressSize;

        public void Parse(EndianBinaryReader blocksInfoReader)
        {
            uncompressedSize = blocksInfoReader.ReadUInt32();
            compressedSize = blocksInfoReader.ReadUInt32();
            flags = (StorageBlockFlags)blocksInfoReader.ReadUInt16();
        }

        public void Write(EndianBinaryWriter blocksInfoWriter, CompareStream compareStream)
        {
            blocksInfoWriter.Write(uncompressedSize);
            compareStream.PauseAndRead(compressedSize);
            blocksInfoWriter.Write(compressedSize);
            compareStream.Continue();
            blocksInfoWriter.Write((ushort)flags);
        }

        public void SetCompressSize(uint compressSize)
        {
            this.originCompressSize = this.compressedSize;
            this.compressedSize = compressSize;

        }
    }
}