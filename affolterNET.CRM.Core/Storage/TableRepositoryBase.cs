using affolterNET.CRM.Core.Entities;
using Azure.Data.Tables;

namespace affolterNET.CRM.Core.Storage;

/// <summary>
/// Small shared base for per-aggregate repositories: upsert, point-get, per-partition list,
/// delete, full-table list and batch-upsert. Tables are created by <see cref="StorageInitializer"/>.
/// Note: Table Storage has no decimal EDM type — entities with money amounts use <c>double</c>.
/// </summary>
public abstract class TableRepositoryBase<T> where T : class, ITableEntity, new()
{
    private const int MaxBatchSize = 100;

    private readonly Lazy<TableClient> _table;

    /// <summary>Binds the repository to an explicit table name.</summary>
    protected TableRepositoryBase(IStorageClientFactory clientFactory, string tableName)
    {
        _table = new Lazy<TableClient>(() => clientFactory.CreateTableServiceClient().GetTableClient(tableName));
    }

    protected TableClient Table => _table.Value;

    /// <summary>
    /// Inserts or replaces the entity. Keys are validated against Table Storage key rules.
    /// </summary>
    public async Task UpsertAsync(T entity, CancellationToken cancellationToken = default)
    {
        ValidateKeys(entity);
        await Table.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }

    /// <summary>
    /// Point-read; returns null if the entity does not exist.
    /// </summary>
    public async Task<T?> GetAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default)
    {
        var response = await Table.GetEntityIfExistsAsync<T>(partitionKey, rowKey, cancellationToken: cancellationToken);
        return response.HasValue ? response.Value : null;
    }

    /// <summary>
    /// Lists all entities of one partition. Returns an empty list for an unknown partition.
    /// </summary>
    public async Task<List<T>> ListPartitionAsync(string partitionKey, CancellationToken cancellationToken = default)
    {
        var result = new List<T>();
        await foreach (var entity in Table.QueryAsync<T>(e => e.PartitionKey == partitionKey, cancellationToken: cancellationToken))
        {
            result.Add(entity);
        }

        return result;
    }

    /// <summary>
    /// Lists every entity in the table (full scan — fine for the small tables this library targets).
    /// </summary>
    public async Task<List<T>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<T>();
        await foreach (var entity in Table.QueryAsync<T>(cancellationToken: cancellationToken))
        {
            result.Add(entity);
        }

        return result;
    }

    /// <summary>
    /// Deletes the entity; no-op if it does not exist.
    /// </summary>
    public async Task DeleteAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default)
    {
        await Table.DeleteEntityAsync(partitionKey, rowKey, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Upserts many entities via table transactions (grouped by partition, chunked to the 100-action limit).
    /// </summary>
    public async Task BatchUpsertAsync(IReadOnlyCollection<T> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            ValidateKeys(entity);
        }

        foreach (var partition in entities.GroupBy(e => e.PartitionKey))
        {
            foreach (var chunk in partition.Chunk(MaxBatchSize))
            {
                var actions = chunk
                    .Select(e => new TableTransactionAction(TableTransactionActionType.UpsertReplace, e))
                    .ToList();
                await Table.SubmitTransactionAsync(actions, cancellationToken);
            }
        }
    }

    private static void ValidateKeys(T entity)
    {
        KeySanitizer.EnsureValidKey(entity.PartitionKey, $"{typeof(T).Name}.PartitionKey");
        KeySanitizer.EnsureValidKey(entity.RowKey, $"{typeof(T).Name}.RowKey");
    }
}
