using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Xiuxian.Scripts.Services
{
    public sealed class SteamCloudProvider : ISaveCloudProvider
    {
        private readonly Type _remoteStorageType;
        private readonly MethodInfo _fileWrite;
        private readonly MethodInfo _fileRead;
        private readonly MethodInfo _fileExists;
        private readonly MethodInfo _getFileSize;
        private readonly string _cloudFileName;

        public bool IsAvailable => true;

        private SteamCloudProvider(
            Type remoteStorageType,
            MethodInfo fileWrite,
            MethodInfo fileRead,
            MethodInfo fileExists,
            MethodInfo getFileSize,
            string cloudFileName)
        {
            _remoteStorageType = remoteStorageType;
            _fileWrite = fileWrite;
            _fileRead = fileRead;
            _fileExists = fileExists;
            _getFileSize = getFileSize;
            _cloudFileName = cloudFileName;
        }

        public static ISaveCloudProvider CreateOrNull(string cloudFileName)
        {
            Type? remoteStorageType = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(a => a.GetType("Steamworks.SteamRemoteStorage"))
                .FirstOrDefault(t => t != null);

            if (remoteStorageType == null)
            {
                return new NullCloudProvider();
            }

            MethodInfo? fileWrite = remoteStorageType.GetMethod("FileWrite", new[] { typeof(string), typeof(byte[]), typeof(int) });
            MethodInfo? fileRead = remoteStorageType.GetMethod("FileRead", new[] { typeof(string), typeof(byte[]), typeof(int) });
            MethodInfo? fileExists = remoteStorageType.GetMethod("FileExists", new[] { typeof(string) });
            MethodInfo? getFileSize = remoteStorageType.GetMethod("GetFileSize", new[] { typeof(string) });

            if (fileWrite == null || fileRead == null || fileExists == null || getFileSize == null)
            {
                return new NullCloudProvider();
            }

            return new SteamCloudProvider(remoteStorageType, fileWrite, fileRead, fileExists, getFileSize, cloudFileName);
        }

        public bool TryUpload(string localPath)
        {
            if (!File.Exists(localPath))
            {
                return false;
            }

            byte[] data = File.ReadAllBytes(localPath);
            object? result = _fileWrite.Invoke(_remoteStorageType, new object[] { _cloudFileName, data, data.Length });
            return result is bool ok && ok;
        }

        public bool TryDownload(string localPath)
        {
            object? existsResult = _fileExists.Invoke(_remoteStorageType, new object[] { _cloudFileName });
            if (existsResult is not bool exists || !exists)
            {
                return false;
            }

            object? sizeResult = _getFileSize.Invoke(_remoteStorageType, new object[] { _cloudFileName });
            int size = sizeResult is int n ? n : 0;
            if (size <= 0)
            {
                return false;
            }

            byte[] buffer = new byte[size];
            object? readResult = _fileRead.Invoke(_remoteStorageType, new object[] { _cloudFileName, buffer, size });
            int read = readResult is int r ? r : 0;
            if (read <= 0)
            {
                return false;
            }

            File.WriteAllBytes(localPath, buffer);
            return true;
        }
    }
}
