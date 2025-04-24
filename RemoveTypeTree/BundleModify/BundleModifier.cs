using AssetStudio;
using BundleCrafter;
using UnityFS;
using BundleFileInfo = BundleCrafter.BundleFileInfo;

namespace BundleCrafter

{
    public class BundleModifier
    {
        public static bool RemoveBundleTypeTree(string bundleFilePath, string newBundleFilePath)
        {
            if (!File.Exists(bundleFilePath))
            {
                return false;
            }

            Console.WriteLine("处理中：" + bundleFilePath);

            BundleModifier modifier = new BundleModifier();
            modifier.ReadBundle(bundleFilePath);
            bool hasChanged = modifier.RemoveFilesTypeTree(bundleFilePath);
            if (hasChanged)
            {
                modifier.WriteBundle(newBundleFilePath);
            }
            Console.WriteLine("处理结束：" + bundleFilePath);
            return true;
        }

        private BundleFileInfo info;
        private BundleFileParser parser;
        private byte[] originBytes;

        public List<(byte[], byte[])> ReplaceList = new List<(byte[], byte[])>();

        private void WriteBundle(string newBundleFilePath)
        {
            parser.Repack();

            var output = new MemoryStream();
            var compareStream = new CompareStream(output, originBytes);
            var streamWriter = new EndianBinaryWriter(compareStream);

            parser.Write(streamWriter, compareStream, 0);

            long resultLength = output.Length;
            output.Seek(0, SeekOrigin.Begin);
            compareStream.ReInit();
            parser.Write(streamWriter, compareStream, resultLength);

            File.WriteAllBytes(newBundleFilePath, output.ToArray());
        }

        private void ReadBundle(string bundleFile)
        {
            originBytes = File.ReadAllBytes(bundleFile);
            parser = new BundleCrafter.BundleFileParser();
            var originStream = new MemoryStream(originBytes);
            using (var fs = new EndianBinaryReader(originStream))
            {
                parser.Load(fs);
            }

            info = parser.CreateBundleFileInfo();
        }

        private bool RemoveFilesTypeTree(string bundleFile)
        {
            foreach (var file in info.files)
            {
                var subReader = new FileReader(bundleFile, file.data);
                if (subReader.FileType == FileType.AssetsFile)
                {
                    var assetsFile = new SerializedFile(subReader);
                    assetsFile.RemoveTypeTree();
                    new SerializedFileSerializer().Serialize(assetsFile, file.outStream);
                }
                else
                {
                    //file.outStream = file.data;
                    //将 file.data流的内容 写入 file.outStream 流
                    file.outStream.Seek(0, SeekOrigin.Begin);
                    file.data.Seek(0, SeekOrigin.Begin);
                    var buffer = file.data.GetBuffer();
                    for (int i = 0; i < file.data.Length; i++)
                    {
                        file.outStream.WriteByte(buffer[i]);
                    }
                }
            }
            return true;
        }
    }
}