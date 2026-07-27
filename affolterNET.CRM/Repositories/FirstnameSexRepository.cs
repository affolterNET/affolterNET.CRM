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
    /// are refreshed. Returns the number of rows written.
    /// </summary>
    public async Task<int> SeedAsync(
        IEnumerable<(string Name, string Sex)> pairs, CancellationToken cancellationToken = default)
    {
        var manualNames = (await ListAllAsync(cancellationToken))
            .Where(e => e.Source == Sources.Manual)
            .Select(e => e.RowKey)
            .ToHashSet(StringComparer.Ordinal);

        var toWrite = new Dictionary<string, FirstnameSexEntity>(StringComparer.Ordinal);
        foreach (var (name, sex) in pairs)
        {
            if (!Sexes.IsValid(sex))
            {
                throw new ArgumentException($"Invalid sex '{sex}' for name '{name}'.", nameof(pairs));
            }

            if (!FirstnameSexEntity.TryNormalize(name, out var normalized) || manualNames.Contains(normalized))
            {
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
