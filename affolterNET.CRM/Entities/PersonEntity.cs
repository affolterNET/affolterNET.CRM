using affolterNET.AzureStorage.Entities;
using Azure;

namespace affolterNET.CRM.Entities;

/// <summary>
/// Table "persons": PartitionKey = the constant <see cref="Partition"/> (single partition —
/// the library targets thousands, not millions, of persons), RowKey = PersonId.
/// One row per real human; consumers derive a deterministic PersonId via
/// <see cref="BuildPersonKey"/> (birthdate distinguishes same-named people and unifies the
/// same person across contexts) or supply their own key. Fuzzy identity resolution is
/// deliberately outside this library.
/// </summary>
public class PersonEntity : IStorageEntity, ITrackedEntity
{
    public static string DefaultTableName => "persons";

    /// <summary>The single partition all persons live in.</summary>
    public const string Partition = "person";

    public string PartitionKey { get; set; } = Partition;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>Birthdate as "yyyy-MM-dd", empty when unknown.</summary>
    public string Birthdate { get; set; } = string.Empty;

    /// <summary>One of <see cref="Sexes"/>; derived from the firstname matchlist at creation.</summary>
    public string Sex { get; set; } = string.Empty;

    /// <summary>Salutation (Anrede) derived from <see cref="Sex"/>; manually overridable.</summary>
    public string Salutation { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;

    public bool Deleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public string Source { get; set; } = string.Empty;
    public string SyncOrigin { get; set; } = string.Empty;
    public string ManualFields { get; set; } = string.Empty;

    /// <summary>
    /// Deterministic PersonId: slug of "lastname firstname" plus the birthdate when known —
    /// e.g. "mueller-peter-1961-04-07". The same inputs always yield the same id, which makes
    /// person syncs idempotent.
    /// </summary>
    public static string BuildPersonKey(string lastName, string firstName, string? birthdate = null)
    {
        var slug = KeySanitizer.ToSlug($"{lastName} {firstName}");
        return string.IsNullOrWhiteSpace(birthdate) ? slug : $"{slug}-{KeySanitizer.ToSlug(birthdate)}";
    }
}
