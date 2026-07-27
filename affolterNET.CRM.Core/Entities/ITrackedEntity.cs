namespace affolterNET.CRM.Core.Entities;

/// <summary>
/// Cross-cutting provenance and lifecycle fields shared by tracked entities.
/// Soft delete only: syncs flag missing rows via <see cref="Deleted"/> and never
/// physically delete, so historical views stay displayable.
/// </summary>
public interface ITrackedEntity
{
    /// <summary>Soft-delete flag — set when the row's source no longer contains it.</summary>
    bool Deleted { get; set; }

    /// <summary>When the row was first created.</summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>When the row was last modified (by sync, import or manual edit).</summary>
    DateTimeOffset UpdatedAt { get; set; }

    /// <summary>When a sync last saw this row in its source.</summary>
    DateTimeOffset? LastSeenAt { get; set; }

    /// <summary>Provenance of the row — one of <see cref="Sources"/>.</summary>
    string Source { get; set; }

    /// <summary>Free-text description of the sync/import that produced the row (e.g. a source URL or job name).</summary>
    string SyncOrigin { get; set; }

    /// <summary>
    /// Semicolon-separated names of properties that were manually edited. Sync writes skip
    /// these fields — see <see cref="ManualFieldExtensions"/>.
    /// </summary>
    string ManualFields { get; set; }
}

/// <summary>Provenance values for <see cref="ITrackedEntity.Source"/>.</summary>
public static class Sources
{
    public const string Sync = "sync";
    public const string Manual = "manual";
    public const string Import = "import";
    public const string Seed = "seed";

    public static readonly string[] All = [Sync, Manual, Import, Seed];
}
