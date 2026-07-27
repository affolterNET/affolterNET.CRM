using Microsoft.Extensions.Logging;

namespace affolterNET.CRM.Core.Storage;

/// <summary>
/// Creates every table and blob container listed in the <see cref="IStorageRegistry"/>.
/// Idempotent — safe to run at every app/job start.
/// </summary>
public sealed class StorageInitializer(
    IStorageClientFactory clientFactory,
    IStorageRegistry registry,
    ILogger<StorageInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var tableService = clientFactory.CreateTableServiceClient();
        foreach (var tableName in registry.TableNames)
        {
            await tableService.CreateTableIfNotExistsAsync(tableName, cancellationToken);
        }

        var blobService = clientFactory.CreateBlobServiceClient();
        foreach (var containerName in registry.BlobContainerNames)
        {
            await blobService.GetBlobContainerClient(containerName).CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        }

        logger.LogInformation(
            "Storage initialized: {Tables} tables, {Containers} blob containers",
            registry.TableNames.Count, registry.BlobContainerNames.Count);
    }
}
