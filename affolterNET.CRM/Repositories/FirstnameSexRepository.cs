using affolterNET.AzureStorage.Entities;
using affolterNET.AzureStorage;
using affolterNET.CRM.Entities;
using affolterNET.CRM.Services;

namespace affolterNET.CRM.Repositories;

/// <summary>
/// The firstname→sex matchlist. Seeding is idempotent and never overwrites manual entries.
/// </summary>
public class FirstnameSexRepository(IStorageClientFactory clientFactory, IStorageRegistry registry)
    : TableRepositoryBase<FirstnameSexEntity>(clientFactory, registry.TableNameFor(typeof(FirstnameSexEntity))),
        IFirstnameSexLookup
{
    /// <summary>The matchlist sex for a firstname, or null when the name is unknown.</summary>
    public async Task<string?> LookupSexAsync(string firstName, CancellationToken cancellationToken = default)
    {
        if (!FirstnameSexEntity.TryNormalize(firstName, out var normalized))
        {
            return null;
        }

        var entry = await GetAsync(FirstnameSexEntity.BuildPartitionKey(normalized), normalized, cancellationToken);
        return entry?.Sex;
    }

    /// <summary>
    /// Seeds (name, sex) pairs. Existing manual entries are preserved; seed-sourced entries
    /// are refreshed. Returns the number of rows written — only NEW or CHANGED rows are:
    /// rewriting the ~11k unchanged BFS rows on every run cost minutes per deploy for a
    /// byte-identical table (measured 2026-08-08), and the rows are already in hand from
    /// the manual-entry scan.
    /// </summary>
    public async Task<int> SeedAsync(
        IEnumerable<(string Name, string Sex)> pairs, CancellationToken cancellationToken = default)
    {
        var existing = (await ListAllAsync(cancellationToken))
            .ToDictionary(e => e.RowKey, StringComparer.Ordinal);

        var toWrite = new Dictionary<string, FirstnameSexEntity>(StringComparer.Ordinal);
        foreach (var (name, sex) in pairs)
        {
            if (!Sexes.IsValid(sex))
            {
                throw new ArgumentException($"Invalid sex '{sex}' for name '{name}'.", nameof(pairs));
            }

            if (!FirstnameSexEntity.TryNormalize(name, out var normalized))
            {
                continue;
            }

            if (existing.TryGetValue(normalized, out var row)
                && (row.Source == Sources.Manual || (row.Source == Sources.Seed && row.Sex == sex)))
            {
                // Manual wins forever; an unchanged seed row needs no rewrite.
                continue;
            }

            toWrite[normalized] = new FirstnameSexEntity
            {
                PartitionKey = FirstnameSexEntity.BuildPartitionKey(normalized),
                RowKey = normalized,
                Sex = sex,
                Source = Sources.Seed,
            };
        }

        await BatchUpsertAsync(toWrite.Values.ToList(), cancellationToken);
        return toWrite.Count;
    }
}
