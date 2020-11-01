using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using elFinder.NetCore.Drivers;
using elFinder.NetCore.Helpers;
using elFinder.NetCore.Models;

namespace elFinder.NetCore.AzureBlobStorage.Driver.Drivers.AzureBlob
{

    public class AzureBlobFile : IBlobItem, ICustomFile
    {
        private const char PathSeparator = '/';


        public string BlobItemName { get; }


        #region Constructors

        public AzureBlobFile(string fileName)
        {
            FullName = fileName;

            BlobItemName = FullName;

            // Remove the root directory if present
            if (BlobItemName.StartsWith($"{AzureBlobStorageApi.ContainerName}/"))
                BlobItemName = BlobItemName.Substring($"{AzureBlobStorageApi.ContainerName}/".Length);
        }


        public AzureBlobFile(BlobItem blobItem)
        {
            FullName = $"{AzureBlobStorageApi.ContainerName}/{blobItem.Name}";

            BlobItemName = FullName;

            // Remove root directory if present
            if (BlobItemName.StartsWith($"{AzureBlobStorageApi.ContainerName}/"))
                BlobItemName = BlobItemName.Substring($"{AzureBlobStorageApi.ContainerName}/".Length);
        }

        #endregion


        #region ICustomFile Members

        public IFile Open(string path)
        {
            return new AzureBlobFile(path);
        }


        public async Task<Stream> CreateAsync()
        {
            await AzureBlobStorageApi.MakeFileAsync(FullName);

            // todo: not tested
            return await AzureBlobStorageApi.FileStreamAsync(FullName);

            // Return empty memory string
            // return new MemoryStream();
        }


        public Task DeleteAsync()
        {
            return AzureBlobStorageApi.DeleteFileAsync(BlobItemName);
        }


        public Task<Stream> OpenReadAsync()
        {
            return AzureBlobStorageApi.FileStreamAsync(BlobItemName);
        }


        public Task PutAsync(string content)
        {
            return AzureBlobStorageApi.PutAsync(BlobItemName, content);
        }


        public Task PutAsync(Stream stream)
        {
            return AzureBlobStorageApi.PutAsync(BlobItemName, stream);
        }


        public FileAttributes Attributes
        {
            get => Name.StartsWith(".") ? FileAttributes.Hidden : FileAttributes.Normal;
            set { } // Azure Storage doesn't support setting attributes
        }

        public IDirectory Directory => new AzureBlobDirectory(DirectoryName);

        public string DirectoryName
        {
            get
            {
                var name = FullName;

                var length = name.Length;
                var startIndex = length;

                while (--startIndex >= 0)
                {
                    var ch = name[startIndex];

                    if (ch == PathSeparator) return name.Substring(0, startIndex);
                }

                return string.Empty;
            }
        }

        public Task<bool> ExistsAsync => AzureBlobStorageApi.FileExistsAsync(BlobItemName);

        public string Extension
        {
            get
            {
                var length = FullName.Length;
                var startIndex = length;

                while (--startIndex >= 0)
                {
                    var ch = FullName[startIndex];

                    if (ch == '.') return FullName.Substring(startIndex);
                }

                return string.Empty;
            }
        }

        public string FullName { get; }

        public Task<BlobProperties> PropertiesAsync => AzureBlobStorageApi.GetProperties(BlobItemName);

        public Task<DateTime> LastWriteTimeUtcAsync => AzureBlobStorageApi.FileLastModifiedTimeUtcAsync(BlobItemName);

        public Task<long> LengthAsync => AzureBlobStorageApi.FileLengthAsync(BlobItemName);

        public string Name
        {
            get
            {
                var length = FullName.Length;
                var startIndex = length;

                while (--startIndex >= 0)
                {
                    var ch = FullName[startIndex];

                    if (ch == PathSeparator) return FullName.Substring(startIndex + 1);
                }

                return FullName;
            }
        }

        public MimeType MimeType => MimeHelper.GetMimeType(Extension);

        #endregion


    }

}