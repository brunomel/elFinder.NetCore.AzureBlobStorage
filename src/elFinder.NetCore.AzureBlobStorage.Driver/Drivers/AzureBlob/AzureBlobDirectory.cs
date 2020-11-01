using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using elFinder.NetCore.Drivers;

namespace elFinder.NetCore.AzureBlobStorage.Driver.Drivers.AzureBlob
{

    public class AzureBlobDirectory : IBlobItem, IDirectory
    {
        private const char PathSeparator = '/';

        public string BlobItemName { get; }


        #region Constructors

        public AzureBlobDirectory(string dirName)
        {
            // Append '/' for not a root dir
            BlobItemName = dirName == string.Empty
                ? dirName
                : $"{dirName.TrimStart(PathSeparator)}/";

            // Remove the root directory if present
            if (BlobItemName.StartsWith($"{AzureBlobStorageApi.ContainerName}/"))
                BlobItemName = BlobItemName.Substring($"{AzureBlobStorageApi.ContainerName}/".Length);

            FullName = dirName;
        }


        public AzureBlobDirectory(BlobItem blobItem)
        {
            BlobItemName = blobItem.Name;

            FullName = $"{AzureBlobStorageApi.ContainerName}/{BlobItemName.TrimEnd(PathSeparator)}";
        }

        #endregion


        #region IDirectory Members

        public Task CreateAsync()
        {
            return AzureBlobStorageApi.CreateDirectoryAsync(BlobItemName);
        }


        public Task DeleteAsync()
        {
            return AzureBlobStorageApi.DeleteDirectoryAsync(BlobItemName);
        }


        public async Task<IEnumerable<IDirectory>> GetDirectoriesAsync()
        {
            var model = AzureBlobStorageApi.ListFilesAndDirectoriesAsync(BlobItemName);

            var directories = model.Where(i => i.Name.EndsWith("/"))
                .Select(i => new AzureBlobDirectory(i)).ToList();

            return directories;
        }


        public async Task<IEnumerable<IFile>> GetFilesAsync(IEnumerable<string> mimeTypes)
        {
            var result = AzureBlobStorageApi.ListFilesAndDirectoriesAsync(BlobItemName)
                .Where(i => !i.Name.EndsWith("/"))
                .Select(i => new AzureBlobFile(i))
                .ToList();

            var mimeTypesList = mimeTypes.ToList();

            return mimeTypesList.Any() ? result.Where(f => mimeTypesList.Contains(f.MimeType)) : result;
        }


        public FileAttributes Attributes
        {
            get => Name.StartsWith(".") ? FileAttributes.Hidden : FileAttributes.Directory;
            set { }
        }

        public Task<bool> ExistsAsync => AzureBlobStorageApi.FileExistsAsync(BlobItemName);

        public string FullName { get; }

        public Task<DateTime> LastWriteTimeUtcAsync => AzureBlobStorageApi.DirectoryLastModifiedTimeUtcAsync(BlobItemName);

        public string Name
        {
            get
            {
                var name = FullName;

                var length = name.Length;
                var startIndex = length;

                while (--startIndex >= 0)
                {
                    var ch = name[startIndex];

                    if (ch == PathSeparator) return name.Substring(startIndex + 1);
                }

                return name;
            }
        }

        public IDirectory Parent
        {
            get
            {
                if (string.IsNullOrEmpty(FullName) || FullName == PathSeparator.ToString()) return null;

                var name = FullName;

                var length = name.Length;
                var startIndex = length;

                while (--startIndex >= 0)
                {
                    var ch = name[startIndex];

                    if (ch == PathSeparator) return new AzureBlobDirectory(name.Substring(0, startIndex));
                }

                return new AzureBlobDirectory($"{AzureBlobStorageApi.ContainerName}/");
            }
        }

        #endregion


    }

}