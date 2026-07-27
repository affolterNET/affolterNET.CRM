using affolterNET.AzureStorage.Entities;
using affolterNET.AzureStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace affolterNET.AzureStorage.Extensions;

/// <summary>Registration options for <see cref="ServiceCollectionExtensions.AddAzureStorage"/>.</summary>
public sealed class AzureStorageOptions
{
    /// <summary>Configuration section the <see cref="StorageOptions"/> are bound from.</summary>
    public string StorageSectionName { get; set; } = StorageOptions.DefaultSectionName;

    /// <summary>Optional prefix applied to every registered table name.</summary>
    public string TablePrefix { get; set; } = string.Empty;

    internal Dictionary<Type, string> Tables { get; } = [];

    internal List<string> BlobContainers { get; } = [];

    /// <summary>Registers an entity type so its table is created and resolvable via the registry.</summary>
    public AzureStorageOptions AddTable<T>() where T : IStorageEntity
    {
        Tables[typeof(T)] = T.DefaultTableName;
        return this;
    }

    /// <summary>Registers a blob container to create at startup.</summary>
    public AzureStorageOptions AddBlobContainer(string containerName)
    {
        if (!BlobContainers.Contains(containerName))
        {
            BlobContainers.Add(containerName);
        }

        return this;
    }
}

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the generic storage layer: options, client factory, registry and initializer.
    /// All singletons. Call <see cref="StorageInitializer.InitializeAsync"/> at startup.
    /// </summary>
    public static IServiceCollection AddAzureStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AzureStorageOptions>? configure = null)
    {
        var options = new AzureStorageOptions();
        configure?.Invoke(options);

        services.Configure<StorageOptions>(configuration.GetSection(options.StorageSectionName));
        services.AddSingleton<IStorageClientFactory, StorageClientFactory>();
        services.AddSingleton<IStorageRegistry>(new StorageRegistry(options.TablePrefix, options.Tables, options.BlobContainers));
        services.AddSingleton<StorageInitializer>();
        return services;
    }
}
