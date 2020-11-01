using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;

namespace elFinder.NetCore.AzureBlobStorage.Driver.Drivers.AzureBlob
{

    public static class AzureBlobStorageApi
    {

        private static BlobServiceClient BlobServiceClient { get; set; }

        public static string ConnectionString { get; set; }

        public static string ContainerName { get; set; }
        
        public static string OriginHostName { get; set; }


        public static async Task CopyDirectoryAsync(string source, string destination)
        {
            source = RemoveContainerFromPathIfPresent(source);
            destination = RemoveContainerFromPathIfPresent(destination);

            foreach (var blobItem in BlobContainerClient.GetBlobs())
            {
                if (blobItem.Deleted || !blobItem.Name.StartsWith(source)) continue;

                var fileName = blobItem.Name.Substring(source.Length);

                var sourceBlobClient = BlobContainerClient.GetBlobClient(source);
                var destinationBlobClient = BlobContainerClient.GetBlobClient($"{destination}{fileName}");

                await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
            }
        }


        public static async Task CopyFileAsync(string source, string destination)
        {
            source = RemoveContainerFromPathIfPresent(source);
            destination = RemoveContainerFromPathIfPresent(destination);

            var sourceBlobClient = BlobContainerClient.GetBlobClient(source);
            var destinationBlobClient = BlobContainerClient.GetBlobClient(destination);

            await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
        }


        public static async Task CreateDirectoryAsync(string dir)
        {
            dir = RemoveContainerFromPathIfPresent(dir);

            var blobClient = BlobContainerClient.GetBlobClient(dir);

            var byteArray = Encoding.ASCII.GetBytes(dir);

            await using var stream = new MemoryStream(byteArray);

            await blobClient.UploadAsync(stream);
        }


        public static async Task DeleteDirectoryAsync(string dir)
        {
            dir = RemoveContainerFromPathIfPresent(dir);

            foreach (var blobItem in BlobContainerClient.GetBlobs())
                if (blobItem.Name.StartsWith(dir))
                    await BlobContainerClient.DeleteBlobAsync(blobItem.Name);
        }


        public static async Task DeleteDirectoryIfExistsAsync(string dir)
        {
            dir = RemoveContainerFromPathIfPresent(dir);

            try
            {
                await DeleteDirectoryAsync(dir);
            }
            catch (RequestFailedException)
            {
                // ignore
            }
        }


        public static async Task DeleteFileAsync(string file)
        {
            file = RemoveContainerFromPathIfPresent(file);

            var blobClient = BlobContainerClient.GetBlobClient(file);

            await blobClient.DeleteAsync();
        }


        public static async Task DeleteFileIfExistsAsync(string file)
        {
            file = RemoveContainerFromPathIfPresent(file);

            try
            {
                await DeleteFileAsync(file);
            }
            catch (RequestFailedException)
            {
                // ignore
            }
        }


        public static async Task<bool> DirectoryExistsAsync(string dir)
        {
            dir = RemoveContainerFromPathIfPresent(dir);

            try
            {
                await DirectoryLastModifiedTimeUtcAsync(dir);

                return true;
            }
            catch (RequestFailedException)
            {
                // ignore
                return false;
            }
        }


        public static async Task<DateTime> DirectoryLastModifiedTimeUtcAsync(string dir)
        {
            dir = RemoveContainerFromPathIfPresent(dir);

            var blobClient = BlobContainerClient.GetBlobClient(dir);

            var response = await blobClient.GetPropertiesAsync();

            return response.Value.LastModified.DateTime;
        }


        public static async Task<byte[]> FileBytesAsync(string file)
        {
            var stream = await FileStreamAsync(file);

            await using var ms = new MemoryStream();

            await stream.CopyToAsync(ms);

            return ms.ToArray();
        }


        public static async Task<bool> FileExistsAsync(string file)
        {
            file = RemoveContainerFromPathIfPresent(file);

            if (file == string.Empty)

                // the root always exists
                return true;

            try
            {
                var blobClient = BlobContainerClient.GetBlobClient(file);

                await blobClient.GetPropertiesAsync();

                return true;
            }
            catch (RequestFailedException)
            {
                // ignore
                return false;
            }
        }


        public static async Task<BlobProperties> GetProperties(string file)
        {
            file = RemoveContainerFromPathIfPresent(file);

            var blobClient = BlobContainerClient.GetBlobClient(file);

            return await blobClient.GetPropertiesAsync();
        }


        public static async Task<DateTime> FileLastModifiedTimeUtcAsync(string file)
        {
            file = RemoveContainerFromPathIfPresent(file);

            var blobClient = BlobContainerClient.GetBlobClient(file);

            var response = await blobClient.GetPropertiesAsync();

            return response.Value.LastModified.DateTime;
        }


        public static async Task<long> FileLengthAsync(string file)
        {
            file = RemoveContainerFromPathIfPresent(file);

            var blobClient = BlobContainerClient.GetBlobClient(file);

            var response = await blobClient.GetPropertiesAsync();

            return response.Value.ContentLength;
        }


        public static async Task<Stream> FileStreamAsync(string file)
        {
            file = RemoveContainerFromPathIfPresent(file);

            var blobClient = BlobContainerClient.GetBlobClient(file);

            var stream = new MemoryStream();
            await blobClient.DownloadToAsync(stream);

            // reset stream position
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }


        public static async Task GetAsync(string file, Stream stream)
        {
            file = RemoveContainerFromPathIfPresent(file);

            var blobClient = BlobContainerClient.GetBlobClient(file);

            var properties = await blobClient.GetPropertiesAsync();
            if (properties.Value.ContentLength != 0L) await blobClient.DownloadToAsync(stream);
        }


        public static IEnumerable<BlobItem> ListFilesAndDirectoriesAsync(string dir)
        {
            dir = dir == string.Empty ? dir : $"{dir.TrimEnd('/')}/";
            dir = RemoveContainerFromPathIfPresent(dir);

            var items = BlobContainerClient.GetBlobs();

            var list = items.Where(i =>
            {
                if (i.Name == dir || !i.Name.StartsWith(dir)) return false;

                // omit sub-subdirectories and their files
                return !Regex.IsMatch(i.Name.Substring(dir.Length), @".+?/.+?");
            }).ToList();

            return list;
        }


        public static async Task MakeFileAsync(string file)
        {
            file = RemoveContainerFromPathIfPresent(file);

            var blobClient = BlobContainerClient.GetBlobClient(file);

            // create an empty blob
            var byteArray = new byte[0];

            await using var stream = new MemoryStream(byteArray);

            await blobClient.UploadAsync(stream);
        }


        public static async Task MoveDirectoryAsync(string source, string destination)
        {
            source = RemoveContainerFromPathIfPresent(source);
            destination = RemoveContainerFromPathIfPresent(destination);

            var src = $"{source}/";
            var dsc = $"{destination}/";

            await CopyDirectoryAsync(src, dsc);
            await DeleteDirectoryAsync(src);
        }


        public static async Task MoveFileAsync(string source, string destination)
        {
            source = RemoveContainerFromPathIfPresent(source);
            destination = RemoveContainerFromPathIfPresent(destination);

            await CopyFileAsync(source, destination);
            await DeleteFileAsync(source);
        }


        public static async Task PutAsync(string file, string content)
        {
            file = RemoveContainerFromPathIfPresent(file);

            var blobClient = BlobContainerClient.GetBlobClient(file);

            var byteArray = Encoding.ASCII.GetBytes(content);

            await using var stream = new MemoryStream(byteArray);

            await blobClient.UploadAsync(stream, true);
        }


        public static async Task PutAsync(string file, Stream stream)
        {
            file = RemoveContainerFromPathIfPresent(file);

            var blobClient = BlobContainerClient.GetBlobClient(file);

            await blobClient.UploadAsync(stream);
        }


        public static async Task UploadAsync(IFormFile file, string path)
        {
            path = RemoveContainerFromPathIfPresent(path);

            var blobClient = BlobContainerClient.GetBlobClient(path);

            await using var fileStream = file.OpenReadStream();

            var blobHttpHeader = new BlobHttpHeaders {ContentType = file.ContentType};

            await blobClient.UploadAsync(fileStream, blobHttpHeader);
        }


        public static string PathCombine(string path1, string path2)
        {
            // Force forward slash as path separator
            var result = Path.Combine(path1, path2);

            return Path.DirectorySeparatorChar == '/' ? result : result.Replace(Path.DirectorySeparatorChar, '/');
        }


        #region private methods

        private static BlobContainerClient BlobContainerClient
        {
            get
            {
                BlobServiceClient ??= new BlobServiceClient(ConnectionString);

                return BlobServiceClient.GetBlobContainerClient(ContainerName);
            }
        }


        private static string RemoveContainerFromPathIfPresent(string path)
        {
            // remove root directory if present
            return path.StartsWith($"{ContainerName}/") ? path.Substring($"{ContainerName}/".Length) : path;
        }

        #endregion


    }

}