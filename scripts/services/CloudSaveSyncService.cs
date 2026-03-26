using Godot;
using System.IO;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Cloud save bridge with Steam-first behavior.
    /// Uses reflection so project can run without direct Steamworks dependency.
    /// </summary>
    public partial class CloudSaveSyncService : Node
    {
        [Export] public string LocalSavePath = "user://save_state.cfg";
        [Export] public string CloudFileName = "save_state.cfg";

        private ISaveCloudProvider _provider = new NullCloudProvider();

        public override void _Ready()
        {
            _provider = CreateProvider();
            GD.Print($"CloudSaveSyncService: Steam cloud available = {_provider.IsAvailable}");
        }

        public bool TryDownloadToLocal(bool enabled)
        {
            if (!enabled || !_provider.IsAvailable)
            {
                return false;
            }

            string path = ProjectSettings.GlobalizePath(LocalSavePath);
            if (!_provider.TryDownload(path))
            {
                return false;
            }

            GD.Print("CloudSaveSyncService: downloaded cloud save to local.");
            return true;
        }

        public bool TryUploadLocal(bool enabled)
        {
            if (!enabled || !_provider.IsAvailable)
            {
                return false;
            }

            string path = ProjectSettings.GlobalizePath(LocalSavePath);
            bool ok = _provider.TryUpload(path);
            if (ok)
            {
                GD.Print("CloudSaveSyncService: uploaded local save to cloud.");
            }
            else
            {
                GD.PushWarning("CloudSaveSyncService: failed to upload local save.");
            }
            return ok;
        }

        private ISaveCloudProvider CreateProvider()
        {
            return SteamCloudProvider.CreateOrNull(CloudFileName);
        }
    }
}
