using affolterNET.AzureStorage.Entities;

namespace affolterNET.AzureStorage;

/// <summary>
/// Maps registered entity types to their effective table names (default name plus an optional
/// consumer-wide prefix) and holds the blob containers to create.
/// <see cref="StorageInitializer"/> creates everything listed here at startup.
/// </summary>
public interface IStorageRegistry
{
    /// <summary>The effective table name for a registered entity type; throws for unregistered types.</summary>
    string TableNameFor(Type entityType);

    /// <summary>All effective table names to create.</summary>
    IReadOnlyCollection<string> TableNames { get; }

    /// <summary>All blob container names to create.</summary>
    IReadOnlyCollection<string> BlobContainerNames { get; }
}

/// <inheritdoc />
public sealed class StorageRegistry : IStorageRegistry
{
    private readonly Dictionary<Type, string> _tables;
    private readonly string[] _containers;

    public StorageRegistry(string tablePrefix, IReadOnlyDictionary<Type, string> tablesByType, IEnumerable<string> blobContainers)
    {
        _tables = tablesByType.ToDictionary(
            pair => pair.Key,
            pair => KeySanitizer.EnsureValidKey(tablePrefix + pair.Value, $"table name for {pair.Key.Name}"));
        _containers = blobContainers.ToArray();
    }

    public string TableNameFor(Type entityType)
    {
        // Walk up the hierarchy: a consumer subclass (MyPersonEntity : PersonEntity) resolves to
        // the table its registered base — or itself — was registered under.
        for (var type = entityType; type is not null; type = type.BaseType)
        {
            if (_tables.TryGetValue(type, out var name))
            {
                return name;
            }
        }

        throw new InvalidOperationException(
            $"Entity type '{entityType.Name}' is not registered. Register it via AddTable<{entityType.Name}>() (or a Use*Type<> call).");
    }

    public IReadOnlyCollection<string> TableNames => _tables.Values.Distinct().ToArray();

    public IReadOnlyCollection<string> BlobContainerNames => _containers;
}
