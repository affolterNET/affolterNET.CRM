using affolterNET.CRM.Core.Entities;
using Azure;

namespace affolterNET.CRM.Entities;

/// <summary>
/// Table "organizations": PartitionKey = OrgType (consumer-registered, e.g. "club",
/// "district", "organization"), RowKey = OrgId (slug). Hierarchies are expressed via the
/// parent reference (e.g. club → district).
/// </summary>
public class OrganizationEntity : ICrmEntity, ITrackedEntity
{
    public static string DefaultTableName => "organizations";

    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>OrgType of the parent organization; empty for roots.</summary>
    public string ParentOrgType { get; set; } = string.Empty;

    /// <summary>OrgId of the parent organization; empty for roots.</summary>
    public string ParentOrgId { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public bool Deleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public string Source { get; set; } = string.Empty;
    public string SyncOrigin { get; set; } = string.Empty;
    public string ManualFields { get; set; } = string.Empty;
}
