// using UnityEngine;
using UnityFS;

namespace BundleCrafter
{
    public class BundleFileParser
    {

        HeaderParser m_Header;

        MetadataParser metaPaser;

        BlockStreamParser m_BlockStream;

        public void Load(EndianBinaryReader reader)
        {
            //Console.WriteLine($"reader. pos:{reader.Position} length:{reader.BaseStream.Length}");
            m_Header = new HeaderParser();
            m_Header.Parse(reader);

            metaPaser = new MetadataParser(m_Header);
            metaPaser.Parse(reader);

            m_BlockStream = new BlockStreamParser(metaPaser);
            m_BlockStream.Parse(reader);


        }

        public void Write(EndianBinaryWriter writer, CompareStream compareStream, long length)
        {
            m_Header.size = length;
            m_Header.Write(writer, compareStream);
            metaPaser.Write(writer, compareStream);
            m_BlockStream.Write(writer, compareStream);
        }

        public BundleFileInfo CreateBundleFileInfo()
        {
            return new BundleFileInfo
            {
                signature = m_Header.signature,
                version = m_Header.version,
                unityVersion = m_Header.unityVersion,
                unityRevision = m_Header.unityRevision,
                files = m_BlockStream.fileList.Select(f => new BundleSubFile { file = f.path, data = f.stream, outStream = f.writeStream }).ToList(),
            };
        }

        public void Repack()
        {
            // long sizeOffset = 0;
            // long originSize = m_BlockStream.CalcualteBlockDataSize();
            m_BlockStream.Calculate();
            // long repackedSize = m_BlockStream.CalcualteBlockDataSize();
            // sizeOffset += repackedSize - originSize;

            // originSize = metaPaser.GetCompressMetaSize();
            metaPaser.Calculate(m_BlockStream.blockData, m_BlockStream.fileList);
            // repackedSize = metaPaser.GetCompressMetaSize();
            // sizeOffset += repackedSize - originSize;

            m_Header.Calculate(metaPaser.compressMetadataBytes_changable, metaPaser.uncompressedBlocksInfoBytes);


            // 
            // 
        }
    }
}
