namespace Xiuxian.Scripts.Services
{
    public interface ISaveCloudProvider
    {
        bool IsAvailable { get; }
        bool TryUpload(string localPath);
        bool TryDownload(string localPath);
    }
}
