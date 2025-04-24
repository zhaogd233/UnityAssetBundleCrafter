using UnityFS;

namespace BundleCrafter
{
    public class BundleFileInfo
    {
        public string signature;
        public uint version;
        public string unityVersion;
        public string unityRevision;
        public ArchiveFlags flags;
        public List<BundleSubFile> files;
    }
}