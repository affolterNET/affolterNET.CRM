namespace affolterNET.CRM.Services;

/// <summary>Lookup abstraction over the firstname→sex matchlist (implemented by the repository).</summary>
public interface IFirstnameSexLookup
{
    /// <summary>The matchlist sex for a firstname, or null when the name is unknown.</summary>
    Task<string?> LookupSexAsync(string firstName, CancellationToken cancellationToken = default);
}

/// <summary>The derived sex and salutation for a new person.</summary>
public sealed record SexAndSalutation(string Sex, string Salutation);

/// <summary>
/// Derives sex and salutation for NEW persons: firstname found in the matchlist → that sex;
/// not found → the configured default (male); sex → salutation via the configured map
/// (default Herr/Frau). Existing persons are never re-derived.
/// </summary>
public interface ISalutationResolver
{
    Task<SexAndSalutation> ResolveForNewPersonAsync(string firstName, CancellationToken cancellationToken = default);

    /// <summary>The salutation for a sex value (falls back to the default sex's salutation).</summary>
    string SalutationFor(string sex);
}
