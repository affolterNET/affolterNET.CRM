using affolterNET.CRM.Core.Entities;
using affolterNET.CRM.Core.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace affolterNET.CRM.Core.Extensions;

/// <summary>Registration options for <see cref="ServiceCollectionExtensions.AddCrmCoreStorage"/>.</summary>
public sealed class CrmCoreStorageOptions
{
    /// <summary>Configuration section the <see cref="CrmStorageOptions"/> are bound from.</summary>
    public string StorageSectionName { get; set; } = CrmStorageOptions.DefaultSectionName;

    /// <summary>Optional prefix applied to every registered table name.</summary>
    public string TablePrefix { get; set; } = string.Empty;

    internal Dictionary<Type, string> Tables { get; } = [];

    internal List<string> BlobContainers { get; } = [];

    /// <summary>Registers an entity type so its table is created and resolvable via the registry.</summary>
    public CrmCoreStorageOptions AddTable<T>() where T : ICrmEntity
    {
        Tables[typeof(T)] = T.DefaultTableName;
        return this;
    }

    /// <summary>Registers a blob container to create at startup.</summary>
    public CrmCoreStorageOptions AddBlobContainer(string containerName)
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
    public static IServiceCollection AddCrmCoreStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<CrmCoreStorageOptions>? configure = null)
    {
        var options = new CrmCoreStorageOptions();
        configure?.Invoke(options);

        services.Configure<CrmStorageOptions>(configuration.GetSection(options.StorageSectionName));
        services.AddSingleton<IStorageClientFactory, StorageClientFactory>();
        services.AddSingleton<IStorageRegistry>(new StorageRegistry(options.TablePrefix, options.Tables, options.BlobContainers));
        services.AddSingleton<StorageInitializer>();
        return services;
    }
}
