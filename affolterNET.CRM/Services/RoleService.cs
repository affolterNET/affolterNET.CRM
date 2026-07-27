using affolterNET.CRM.Configuration;
using affolterNET.AzureStorage.Entities;
using affolterNET.CRM.Entities;
using affolterNET.CRM.Repositories;

namespace affolterNET.CRM.Services;

/// <summary>A role to assign; Period is required for periodic role types.</summary>
public sealed record RoleAssignment(
    string RoleType,
    string OrgType,
    string OrgId,
    string PersonId,
    string? Period = null,
    string RoleText = "",
    string SourceUrl = "",
    string Source = Sources.Sync,
    string SyncOrigin = "");

/// <summary>
/// The assignment outcome: <c>ManualConflict</c> is true when a manually entered holder
/// blocked a sync write (the manual row is returned unchanged).
/// </summary>
public sealed record RoleAssignResult<TRole>(TRole Role, bool ManualConflict);

/// <summary>A role resolved for a target period; <c>IsExact</c> is false on period fallback.</summary>
public sealed record ResolvedRole<TRole>(TRole Role, string Period, bool IsExact);

/// <summary>
/// Role assignment and period-aware resolution. Syncs manage only the current period —
/// past-period rows are never touched, so history stays displayable. Manual role rows
/// (Source = manual) are never overwritten or removed by sync writes.
/// </summary>
public class RoleService<TRole>(
    RoleRepository<TRole> repository,
    CrmOptions options,
    TimeProvider timeProvider)
    where TRole : RoleEntity, new()
{
    /// <summary>
    /// Creates or updates a role row. For exclusive roles this replaces the current holder
    /// (unless the existing row is manual and the assignment is not).
    /// </summary>
    public async Task<RoleAssignResult<TRole>> AssignAsync(
        RoleAssignment assignment, Action<TRole>? applyExtensions = null, CancellationToken cancellationToken = default)
    {
        var definition = options.GetRole(assignment.RoleType);
        var partitionKey = RoleEntity.BuildPartitionKey(definition, assignment.Period);
        var rowKey = RoleEntity.BuildRowKey(definition, assignment.OrgId, assignment.PersonId);

        var now = timeProvider.GetUtcNow();
        var role = await repository.GetAsync(partitionKey, rowKey, cancellationToken);

        if (role is not null && role.Source == Sources.Manual && assignment.Source != Sources.Manual)
        {
            return new RoleAssignResult<TRole>(role, ManualConflict: true);
        }

        role ??= new TRole { PartitionKey = partitionKey, RowKey = rowKey, CreatedAt = now };
        role.RoleType = assignment.RoleType;
        role.Period = assignment.Period ?? string.Empty;
        role.OrgType = assignment.OrgType;
        role.OrgId = assignment.OrgId;
        role.PersonId = assignment.PersonId;
        role.RoleText = assignment.RoleText;
        role.SourceUrl = assignment.SourceUrl;
        role.Source = assignment.Source;
        role.SyncOrigin = assignment.SyncOrigin;
        role.Deleted = false;
        role.LastSeenAt = now;
        role.UpdatedAt = now;

        applyExtensions?.Invoke(role);
        await repository.UpsertAsync(role, cancellationToken);
        return new RoleAssignResult<TRole>(role, ManualConflict: false);
    }

    /// <summary>
    /// Resolves the holder of an exclusive periodic role for a target period, falling back to
    /// the nearest available period in the given order. Null when no period has a holder.
    /// </summary>
    public async Task<ResolvedRole<TRole>?> ResolveWithFallbackAsync(
        string roleType,
        string orgId,
        string targetPeriod,
        PeriodFallbackOrder order,
        CancellationToken cancellationToken = default)
    {
        var definition = options.GetRole(roleType);
        var periods = await repository.ListPeriodsAsync(definition, cancellationToken);

        foreach (var period in PeriodFallback.OrderCandidates(targetPeriod, periods, order))
        {
            var holder = await repository.GetHolderAsync(definition, period, orgId, cancellationToken);
            if (holder is not null && !holder.Deleted)
            {
                return new ResolvedRole<TRole>(holder, period, period == targetPeriod);
            }
        }

        return null;
    }

    /// <summary>All holders of a role type for one period, keyed by OrgId.</summary>
    public async Task<Dictionary<string, TRole>> MapByOrgAsync(
        string roleType, string? period, CancellationToken cancellationToken = default)
    {
        var definition = options.GetRole(roleType);
        var roles = await repository.ListByRoleAsync(definition, period, cancellationToken);
        return roles.Where(r => !r.Deleted).ToDictionary(r => r.OrgId, StringComparer.Ordinal);
    }

    /// <summary>The periods a periodic role type has rows for (e.g. the year-selector values).</summary>
    public Task<List<string>> ListPeriodsAsync(string roleType, CancellationToken cancellationToken = default) =>
        repository.ListPeriodsAsync(options.GetRole(roleType), cancellationToken);

    /// <summary>
    /// Mirror semantics for one org and period: deletes role rows whose person was not seen by
    /// the sync (manual rows are kept). Persons themselves are only ever soft-deleted — this
    /// removes relationship rows, not people. Returns the number of removed rows.
    /// </summary>
    public async Task<int> RemoveMissingAsync(
        string roleType,
        string orgId,
        string? period,
        IReadOnlySet<string> seenPersonIds,
        CancellationToken cancellationToken = default)
    {
        var definition = options.GetRole(roleType);
        var removed = 0;
        foreach (var role in await repository.ListByOrgAsync(definition, orgId, period, cancellationToken))
        {
            if (!seenPersonIds.Contains(role.PersonId) && role.Source != Sources.Manual)
            {
                await repository.DeleteAsync(role.PartitionKey, role.RowKey, cancellationToken);
                removed++;
            }
        }

        return removed;
    }
}
