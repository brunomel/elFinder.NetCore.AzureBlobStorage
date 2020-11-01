using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using elFinder.NetCore.Drivers;

namespace elFinder.NetCore.AzureBlobStorage.Driver.Drivers.AzureBlob
{

    /// <summary>
    ///     Adds some additional methods to extend IFile interface
    /// </summary>
    public interface ICustomFile : IFile
    {
        /// <summary>
        ///     Used to get all the file properties from Azure
        /// </summary>
        Task<BlobProperties> PropertiesAsync { get; }
    }

}