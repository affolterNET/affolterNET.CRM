using affolterNET.CRM.Configuration;
using affolterNET.AzureStorage;
using affolterNET.CRM.Entities;
using Azure.Data.Tables;

namespace affolterNET.CRM.Repositories;

/// <summary>
/// Role queries over the key scheme documented on <see cref="RoleEntity"/>: holders of a
/// role type+period are one partition, members of one org are a RowKey prefix range.
/// </summary>
public class RoleRepository<TRole>(IStorageClientFactory clientFactory, IStorageRegistry registry)
    : TableRepositoryBase<TRole>(clientFactory, registry.TableNameFor(typeof(TRole)))
    where TRole : RoleEntity, new()
{
    // '`' is the character after the reserved '_' separator — upper bound for prefix ranges.
    private const char AfterSeparator = (char)(RoleEntity.Separator + 1);

    /// <summary>The holder of an exclusive role for one org (and period); null when unassigned.</summary>
    public Task<TRole?> GetHolderAsync(
        RoleDefinition definition, string? period, string orgId, CancellationToken cancellationToken = default)
    {
        RequireExclusive(definition);
        return GetAsync(RoleEntity.BuildPartitionKey(definition, period), orgId, cancellationToken);
    }

    /// <summary>All holders of a role type (and period) across organizations — one partition.</summary>
    public Task<List<TRole>> ListByRoleAsync(
        RoleDefinition definition, string? period, CancellationToken cancellationToken = default) =>
        ListPartitionAsync(RoleEntity.BuildPartitionKey(definition, period), cancellationToken);

    /// <summary>All role rows of one organization (RowKey prefix range for non-exclusive roles).</summary>
    public async Task<List<TRole>> ListByOrgAsync(
        RoleDefinition definition, string orgId, string? period, CancellationToken cancellationToken = default)
    {
        var partitionKey = RoleEntity.BuildPartitionKey(definition, period);
        if (definition.ExclusivePerOrg)
        {
            var holder = await GetAsync(partitionKey, orgId, cancellationToken);
            return holder is null ? [] : [holder];
        }

        var filter = $"PartitionKey eq '{Escape(partitionKey)}'"
            + $" and RowKey ge '{Escape(orgId + RoleEntity.Separator)}'"
            + $" and RowKey lt '{Escape(orgId + AfterSeparator)}'";
        return await QueryAsync(filter, cancellationToken);
    }

    /// <summary>
    /// All roles of one person — a filtered full-table scan; fine at this library's scale
    /// but not a hot-path query.
    /// </summary>
    public Task<List<TRole>> ListByPersonAsync(string personId, CancellationToken cancellationToken = default) =>
        QueryAsync($"PersonId eq '{Escape(personId)}'", cancellationToken);

    /// <summary>The distinct periods a periodic role type has rows for (year-selector values).</summary>
    public async Task<List<string>> ListPeriodsAsync(
        RoleDefinition definition, CancellationToken cancellationToken = default)
    {
        if (!definition.HasPeriod)
        {
            throw new ArgumentException($"Role '{definition.RoleType}' has no period.", nameof(definition));
        }

        var prefix = definition.RoleType + RoleEntity.Separator;
        var filter = $"PartitionKey ge '{Escape(prefix)}'"
            + $" and PartitionKey lt '{Escape(definition.RoleType + AfterSeparator)}'";

        var periods = new HashSet<string>(StringComparer.Ordinal);
        await foreach (var row in Table.QueryAsync<TableEntity>(
                           filter, select: ["PartitionKey"], cancellationToken: cancellationToken))
        {
            periods.Add(row.PartitionKey[prefix.Length..]);
        }

        return periods.Order(StringComparer.Ordinal).ToList();
    }

    private async Task<List<TRole>> QueryAsync(string filter, CancellationToken cancellationToken)
    {
        var result = new List<TRole>();
        await foreach (var entity in Table.QueryAsync<TRole>(filter, cancellationToken: cancellationToken))
        {
            result.Add(entity);
        }

        return result;
    }

    private static void RequireExclusive(RoleDefinition definition)
    {
        if (!definition.ExclusivePerOrg)
        {
            throw new ArgumentException(
                $"Role '{definition.RoleType}' is not exclusive per organization — use ListByOrgAsync.",
                nameof(definition));
        }
    }

    private static string Escape(string value) => value.Replace("'", "''");
}
