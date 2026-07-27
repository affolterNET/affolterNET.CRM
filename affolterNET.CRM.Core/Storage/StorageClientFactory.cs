using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace affolterNET.CRM.Core.Storage;

/// <summary>
/// Factory for creating Azure Storage clients with appropriate authentication.
/// Priority:
/// 1. ConnectionString - if set, uses connection string auth (Azurite or production)
/// 2. StorageClientId - if set, uses ManagedIdentityCredential with specific client ID
/// 3. Otherwise - uses DefaultAzureCredential (Azure CLI, VS, etc.)
/// </summary>
public class StorageClientFactory : IStorageClientFactory
{
    private readonly CrmStorageOptions _storageOptions;
    private readonly TokenCredential? _credential;
    private readonly bool _useConnectionString;

    public StorageClientFactory(IOptions<CrmStorageOptions> storageOptions)
    {
        _storageOptions = storageOptions.Value;
        _useConnectionString = !string.IsNullOrWhiteSpace(_storageOptions.ConnectionString);

        // Only create credential if not using connection string
        if (!_useConnectionString)
        {
            // Use ManagedIdentityCredential with specific client ID for user-assigned identity
            // Fall back to DefaultAzureCredential for local development (Azure CLI, VS, etc.)
            var useManagedIdentity = !string.IsNullOrWhiteSpace(_storageOptions.StorageClientId);
            _credential = useManagedIdentity
                ? new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(_storageOptions.StorageClientId))
                : new DefaultAzureCredential();
        }
    }

    /// <summary>
    /// Creates a TableServiceClient with the appropriate authentication.
    /// </summary>
    public TableServiceClient CreateTableServiceClient()
    {
        if (_useConnectionString)
        {
            return new TableServiceClient(_storageOptions.ConnectionString);
        }

        var tableUri = new Uri($"https://{_storageOptions.AccountName}.table.core.windows.net");
        return new TableServiceClient(tableUri, _credential);
    }

    /// <summary>
    /// Creates a BlobServiceClient with the appropriate authentication.
    /// </summary>
    public BlobServiceClient CreateBlobServiceClient()
    {
        if (_useConnectionString)
        {
            return new BlobServiceClient(_storageOptions.ConnectionString);
        }

        var blobUri = new Uri($"https://{_storageOptions.AccountName}.blob.core.windows.net");
        return new BlobServiceClient(blobUri, _credential);
    }

    /// <summary>
    /// Creates a BlobContainerClient with the appropriate authentication.
    /// </summary>
    public BlobContainerClient CreateBlobContainerClient(string containerName)
    {
        if (_useConnectionString)
        {
            return new BlobContainerClient(_storageOptions.ConnectionString, containerName);
        }

        var containerUri = new Uri($"https://{_storageOptions.AccountName}.blob.core.windows.net/{containerName}");
        return new BlobContainerClient(containerUri, _credential);
    }
}
