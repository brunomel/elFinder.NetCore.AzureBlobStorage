using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using elFinder.NetCore.AzureBlobStorage.Driver.Drivers.AzureBlob;
using elFinder.NetCore.Helpers;
using elFinder.NetCore.Models;

namespace elFinder.NetCore.AzureBlobStorage.Driver.Models
{

    public class CustomFileModel : CustomBaseModel
    {
        [JsonPropertyName("phash")] public string ParentHash { get; set; }

        [JsonPropertyName("tmb")] public object Thumbnail { get; set; }

        [JsonPropertyName("dim")] public string Dimension { get; set; }
    }


    /// <summary>
    ///     Replace the BaseModel improving speed:
    ///     <para>1. it does not read the files from Azure to create the thumbnails on the fly</para>
    ///     <para>2. it does only one Azure single call to get the file properties</para>
    /// </summary>
    public abstract class CustomBaseModel : BaseModel
    {
        public static async Task<CustomFileModel> CustomCreateAsync(ICustomFile file, RootVolume volume)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (volume == null)
                throw new ArgumentNullException(nameof(volume));

            var parentPath = file.DirectoryName.Substring(volume.RootDirectory.Length);
            var relativePath = file.FullName.Substring(volume.RootDirectory.Length);

            var fileProperties = await file.PropertiesAsync;

            var response = new CustomFileModel
            {
                UnixTimeStamp = (long) (fileProperties.LastModified.DateTime - unixOrigin).TotalSeconds,
                Read = 1,
                Write = volume.IsReadOnly ? (byte) 0 : (byte) 1,
                Locked = volume.LockedFolders != null && volume.LockedFolders.Any(f => f == file.Directory.Name) || volume.IsLocked ? (byte) 1 : (byte) 0,
                Name = file.Name,
                Size = fileProperties.ContentLength,
                Mime = MimeHelper.GetMimeType(file.Extension),
                Hash = volume.VolumeId + HttpEncoder.EncodePath(relativePath),
                ParentHash = volume.VolumeId + HttpEncoder.EncodePath(parentPath.Length > 0 ? parentPath : file.Directory.Name)
            };

            // We don't download and create thumbnails for files bigger than 2Mb
            if (!volume.CanCreateThumbnail(file) || fileProperties.ContentLength <= 0L || fileProperties.ContentLength > 2000000) return response;

            var filePath = $"{file.Directory.FullName}/{Path.GetFileNameWithoutExtension(file.Name)}";

            // Remove first segment of the path before the first '/'
            filePath = filePath.Substring(filePath.IndexOf('/'));

            // Add ticks to be sure that the thumbnail will be re-created if an image with the same filename will be uploaded again
            var str = filePath + "_" + fileProperties.CreatedOn.Ticks + file.Extension;

            response.Thumbnail = HttpEncoder.EncodePath(str);

            return response;
        }
    }

}