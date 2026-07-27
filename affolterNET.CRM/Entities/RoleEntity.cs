using affolterNET.CRM.Configuration;
using affolterNET.CRM.Core.Entities;
using Azure;

namespace affolterNET.CRM.Entities;

/// <summary>
/// Table "roles": links person ⇄ organization for a role type and optional period.
/// PartitionKey = "{roleType}_{period}" (periodic) or "{roleType}"; RowKey = "{orgId}"
/// (exclusive roles — replacement for free) or "{orgId}_{personId}". '_' is the reserved
/// separator and rejected inside key components (periods like "2026-2027" are hyphenated).
/// Holders of a role type+period are one partition; members of one org are an efficient
/// RowKey prefix range. No person data is snapshotted here — resolvers join the person.
/// Past-period rows are never touched by syncs, so history stays displayable.
/// </summary>
public class RoleEntity : ICrmEntity, ITrackedEntity
{
    public static string DefaultTableName => "roles";

    /// <summary>Reserved key separator; rejected inside roleType, orgId and personId.</summary>
    public const char Separator = '_';

    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string RoleType { get; set; } = string.Empty;

    /// <summary>The period this role is scoped to (e.g. "2026-2027"); empty for periodless roles.</summary>
    public string Period { get; set; } = string.Empty;

    public string OrgType { get; set; } = string.Empty;
    public string OrgId { get; set; } = string.Empty;
    public string PersonId { get; set; } = string.Empty;

    /// <summary>The raw role label as parsed from the source (e.g. "Präsident/in 2026-2027").</summary>
    public string RoleText { get; set; } = string.Empty;

    /// <summary>Provenance link to the page this role was synced from; empty for manual entries.</summary>
    public string SourceUrl { get; set; } = string.Empty;

    public bool Deleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public string Source { get; set; } = string.Empty;
    public string SyncOrigin { get; set; } = string.Empty;
    public string ManualFields { get; set; } = string.Empty;

    public static string BuildPartitionKey(RoleDefinition definition, string? period)
    {
        RejectSeparator(definition.RoleType, nameof(definition));
        if (!definition.HasPeriod)
        {
            return KeySanitizer.EnsureValidKey(definition.RoleType);
        }

        if (string.IsNullOrWhiteSpace(period))
        {
            throw new ArgumentException($"Role '{definition.RoleType}' requires a period.", nameof(period));
        }

        RejectSeparator(period, nameof(period));
        return KeySanitizer.EnsureValidKey($"{definition.RoleType}{Separator}{period}");
    }

    public static string BuildRowKey(RoleDefinition definition, string orgId, string personId)
    {
        RejectSeparator(orgId, nameof(orgId));
        if (definition.ExclusivePerOrg)
        {
            return KeySanitizer.EnsureValidKey(orgId);
        }

        RejectSeparator(personId, nameof(personId));
        return KeySanitizer.EnsureValidKey($"{orgId}{Separator}{personId}");
    }

    private static void RejectSeparator(string value, string paramName)
    {
        if (value.Contains(Separator))
        {
            throw new ArgumentException($"'{value}' must not contain the reserved separator '{Separator}'.", paramName);
        }
    }
}
