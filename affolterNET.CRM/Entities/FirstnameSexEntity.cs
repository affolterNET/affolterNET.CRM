using affolterNET.AzureStorage.Entities;
using Azure;

namespace affolterNET.CRM.Entities;

/// <summary>
/// Table "firstnamesex": the firstname→sex matchlist. PartitionKey = first character of the
/// normalized name (a–z/0–9 spread), RowKey = normalized firstname (first whitespace token,
/// slugged — "Hans Peter" → "hans", "André" → "andre"). The library ships the mechanism;
/// the seed DATA is consumer-supplied (e.g. an open-data firstname list) plus manual entries.
/// </summary>
public class FirstnameSexEntity : IStorageEntity
{
    public static string DefaultTableName => "firstnamesex";

    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    /// <summary>One of <see cref="Sexes"/>.</summary>
    public string Sex { get; set; } = string.Empty;

    /// <summary><see cref="Sources.Seed"/> (from the seeded list) or <see cref="Sources.Manual"/>.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Normalizes a firstname for lookup/storage: first whitespace token, slugged.
    /// Returns false when no usable token can be derived (empty or symbol-only input).
    /// </summary>
    public static bool TryNormalize(string firstName, out string normalized)
    {
        normalized = string.Empty;
        var token = firstName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (token is null)
        {
            return false;
        }

        try
        {
            normalized = KeySanitizer.ToSlug(token);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>Partition for a normalized name: its first character.</summary>
    public static string BuildPartitionKey(string normalizedName) =>
        KeySanitizer.EnsureValidKey(normalizedName[..1]);
}
