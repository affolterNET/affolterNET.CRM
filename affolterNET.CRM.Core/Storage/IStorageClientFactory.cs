using Azure.Data.Tables;
using Azure.Storage.Blobs;

namespace affolterNET.CRM.Core.Storage;

/// <summary>
/// Factory for creating Azure Storage clients with the configured authentication.
/// </summary>
public interface IStorageClientFactory
{
    /// <summary>
    /// Creates a TableServiceClient with the appropriate authentication.
    /// </summary>
    TableServiceClient CreateTableServiceClient();

    /// <summary>
    /// Creates a BlobServiceClient with the appropriate authentication.
    /// </summary>
    BlobServiceClient CreateBlobServiceClient();

    /// <summary>
    /// Creates a BlobContainerClient for the given container with the appropriate authentication.
    /// </summary>
    BlobContainerClient CreateBlobContainerClient(string containerName);
}
