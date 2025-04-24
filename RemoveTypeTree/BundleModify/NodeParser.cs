using UnityFS;

namespace BundleCrafter
{
    public class NodeParser
    {
        /// <summary>
        /// 解压缩数据中的偏移
        /// </summary>
        public long offset;
        /// <summary>
        /// 大小
        /// </summary>
        public long size;
        /// <summary>
        /// 标志位
        /// </summary>
        public uint flags;

        /// <summary>
        /// 文件路径
        /// </summary>
        public string path;

        public void Parse(EndianBinaryReader blocksInfoReader)
        {
            offset = blocksInfoReader.ReadInt64();
            size = blocksInfoReader.ReadInt64();
            flags = blocksInfoReader.ReadUInt32();
            path = blocksInfoReader.ReadStringToNull();
        }

        public void Write(EndianBinaryWriter blocksInfoWriter)
        {
            blocksInfoWriter.Write(offset);
            blocksInfoWriter.Write(size);
            blocksInfoWriter.Write(flags);
            blocksInfoWriter.WriteNullEndString(path);
        }
    }
}