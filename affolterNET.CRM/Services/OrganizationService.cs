using affolterNET.CRM.Configuration;
using affolterNET.CRM.Core.Entities;
using affolterNET.CRM.Entities;
using affolterNET.CRM.Repositories;

namespace affolterNET.CRM.Services;

/// <summary>
/// Organization data from a sync/import. Null optional fields mean "no information" and leave
/// the stored value untouched.
/// </summary>
public sealed record OrganizationData(
    string OrgType,
    string OrgId,
    string Name,
    string? ParentOrgType = null,
    string? ParentOrgId = null,
    string? Url = null,
    string? Email = null,
    string? Phone = null,
    string Source = Sources.Sync,
    string SyncOrigin = "");

/// <summary>
/// The single write path for organizations — same ManualFields-guarded update semantics as
/// <see cref="PersonService{TPerson}"/>. The OrgType must be registered in the options.
/// </summary>
public class OrganizationService<TOrg>(
    OrganizationRepository<TOrg> repository,
    CrmOptions options,
    TimeProvider timeProvider)
    where TOrg : OrganizationEntity, new()
{
    /// <summary>Creates or updates the organization and returns the stored entity.</summary>
    public async Task<TOrg> EnsureAsync(
        OrganizationData data, Action<TOrg>? applyExtensions = null, CancellationToken cancellationToken = default)
    {
        if (!options.IsOrganizationTypeRegistered(data.OrgType))
        {
            throw new InvalidOperationException(
                $"Organization type '{data.OrgType}' is not registered. Register it via AddOrganizationType().");
        }

        var now = timeProvider.GetUtcNow();
        var org = await repository.GetAsync(data.OrgType, data.OrgId, cancellationToken);

        if (org is null)
        {
            org = new TOrg
            {
                PartitionKey = data.OrgType,
                RowKey = data.OrgId,
                Name = data.Name,
                ParentOrgType = data.ParentOrgType ?? string.Empty,
                ParentOrgId = data.ParentOrgId ?? string.Empty,
                Url = data.Url ?? string.Empty,
                Email = data.Email ?? string.Empty,
                Phone = data.Phone ?? string.Empty,
                CreatedAt = now,
            };
        }
        else
        {
            org.SetUnlessManual(nameof(OrganizationEntity.Name), () => org.Name = data.Name);
            ApplyOptional(org, nameof(OrganizationEntity.ParentOrgType), data.ParentOrgType, v => org.ParentOrgType = v);
            ApplyOptional(org, nameof(OrganizationEntity.ParentOrgId), data.ParentOrgId, v => org.ParentOrgId = v);
            ApplyOptional(org, nameof(OrganizationEntity.Url), data.Url, v => org.Url = v);
            ApplyOptional(org, nameof(OrganizationEntity.Email), data.Email, v => org.Email = v);
            ApplyOptional(org, nameof(OrganizationEntity.Phone), data.Phone, v => org.Phone = v);
        }

        org.Deleted = false;
        org.LastSeenAt = now;
        org.UpdatedAt = now;
        org.Source = data.Source;
        if (data.SyncOrigin.Length > 0)
        {
            org.SyncOrigin = data.SyncOrigin;
        }

        applyExtensions?.Invoke(org);
        await repository.UpsertAsync(org, cancellationToken);
        return org;
    }

    /// <summary>
    /// Applies a manual edit and marks the field as manually protected. Returns the updated
    /// organization, or null when it does not exist.
    /// </summary>
    public async Task<TOrg?> SetManualFieldAsync(
        string orgType, string orgId, string fieldName, Action<TOrg> apply, CancellationToken cancellationToken = default)
    {
        var org = await repository.GetAsync(orgType, orgId, cancellationToken);
        if (org is null)
        {
            return null;
        }

        apply(org);
        org.MarkFieldManual(fieldName);
        org.UpdatedAt = timeProvider.GetUtcNow();
        await repository.UpsertAsync(org, cancellationToken);
        return org;
    }

    /// <summary>Sets or clears the soft-delete flag. Returns false when the organization does not exist.</summary>
    public async Task<bool> SetDeletedAsync(
        string orgType, string orgId, bool deleted, CancellationToken cancellationToken = default)
    {
        var org = await repository.GetAsync(orgType, orgId, cancellationToken);
        if (org is null)
        {
            return false;
        }

        org.Deleted = deleted;
        org.UpdatedAt = timeProvider.GetUtcNow();
        await repository.UpsertAsync(org, cancellationToken);
        return true;
    }

    private static void ApplyOptional(TOrg org, string fieldName, string? value, Action<string> apply)
    {
        if (value is not null)
        {
            org.SetUnlessManual(fieldName, () => apply(value));
        }
    }
}
