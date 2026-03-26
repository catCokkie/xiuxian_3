namespace Xiuxian.Scripts.Services
{
    public sealed class NullCloudProvider : ISaveCloudProvider
    {
        public bool IsAvailable => false;

        public bool TryUpload(string localPath)
        {
            return false;
        }

        public bool TryDownload(string localPath)
        {
            return false;
        }
    }
}
