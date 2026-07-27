using affolterNET.CRM.Core.Entities;
using Azure;

namespace affolterNET.CRM.Entities;

/// <summary>
/// Table "addresses": PartitionKey = owner key ("person_{personId}" or
/// "org_{orgType}_{orgId}"), RowKey = purpose (default "main"). One address per
/// (owner, purpose); persons and organizations share the table.
/// </summary>
public class AddressEntity : ICrmEntity, ITrackedEntity
{
    public static string DefaultTableName => "addresses";

    /// <summary>The default address purpose.</summary>
    public const string MainPurpose = "main";

    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = MainPurpose;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    /// <summary>c/o line (e.g. the club a president letter is addressed care of); empty when unused.</summary>
    public string CareOf { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;
    public string StreetNr { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;

    public bool Deleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public string Source { get; set; } = string.Empty;
    public string SyncOrigin { get; set; } = string.Empty;
    public string ManualFields { get; set; } = string.Empty;

    /// <summary>Owner key for a person's addresses.</summary>
    public static string ForPerson(string personId) => KeySanitizer.EnsureValidKey($"person_{personId}");

    /// <summary>Owner key for an organization's addresses.</summary>
    public static string ForOrganization(string orgType, string orgId) =>
        KeySanitizer.EnsureValidKey($"org_{orgType}_{orgId}");
}
