using affolterNET.CRM.Configuration;
using affolterNET.AzureStorage.Entities;
using affolterNET.AzureStorage;
using affolterNET.CRM.Entities;
using affolterNET.CRM.Extensions;
using affolterNET.CRM.Repositories;
using affolterNET.CRM.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace affolterNET.CRM.Tests;

/// <summary>
/// End-to-end tests against a real (test-)Azurite: DI wiring, initializer, the Ensure flows
/// with manual-override protection, role queries and the matchlist seeding. Each test class
/// instance uses a unique table prefix, so runs never interfere.
/// </summary>
public class CrmIntegrationTests : IAsyncLifetime
{
    private sealed class TestPerson : PersonEntity
    {
        public string Language { get; set; } = string.Empty;
    }

    private ServiceProvider _provider = null!;
    private PersonService<TestPerson> _persons = null!;
    private RoleService<RoleEntity> _roles = null!;

    public async Task InitializeAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable(AzuriteFactAttribute.EnvVar);
        if (connectionString is null)
        {
            return; // every test is [AzuriteFact]-skipped
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Test:Storage:ConnectionString"] = connectionString,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddCrmServices(configuration, o =>
        {
            o.StorageSectionName = "Test:Storage";
            o.TablePrefix = "t" + Guid.NewGuid().ToString("N")[..8];
            o.AddOrganizationType("club").AddOrganizationType("district");
            o.AddRole("member", hasPeriod: false, exclusivePerOrg: false);
            o.AddRole("president", hasPeriod: true, exclusivePerOrg: true);
            o.UsePersonType<TestPerson>();
        });

        _provider = services.BuildServiceProvider();
        await _provider.GetRequiredService<StorageInitializer>().InitializeAsync();
        _persons = _provider.GetRequiredService<PersonService<TestPerson>>();
        _roles = _provider.GetRequiredService<RoleService<RoleEntity>>();
    }

    public Task DisposeAsync()
    {
        _provider?.Dispose();
        return Task.CompletedTask;
    }

    [AzuriteFact]
    public async Task EnsurePerson_New_DerivesSexFromSeededMatchlist()
    {
        await _provider.GetRequiredService<FirstnameSexRepository>().SeedAsync([("Vera", Sexes.Female)]);

        var person = await _persons.EnsureAsync(
            new PersonData("mueller-vera", "Vera", "Müller"),
            p => p.Language = "de");

        Assert.Equal(Sexes.Female, person.Sex);
        Assert.Equal("Frau", person.Salutation);
        Assert.Equal("de", person.Language);
        Assert.False(person.Deleted);

        var unknown = await _persons.EnsureAsync(new PersonData("meier-xaq", "Xaq", "Meier"));
        Assert.Equal(Sexes.Male, unknown.Sex);
        Assert.Equal("Herr", unknown.Salutation);
    }

    [AzuriteFact]
    public async Task EnsurePerson_ManualSalutation_SurvivesLaterSync()
    {
        await _persons.EnsureAsync(new PersonData("huber-kim", "Kim", "Huber"));

        var edited = await _persons.SetSalutationAsync("huber-kim", "Frau");
        Assert.NotNull(edited);

        // A later sync updates other fields but must not clobber the manual Anrede.
        var resynced = await _persons.EnsureAsync(
            new PersonData("huber-kim", "Kim", "Huber", Email: "kim@example.ch"));

        Assert.Equal("Frau", resynced.Salutation);
        Assert.Equal("kim@example.ch", resynced.Email);
    }

    [AzuriteFact]
    public async Task EnsurePerson_SubclassColumns_SurviveRoundtrip()
    {
        await _persons.EnsureAsync(new PersonData("roth-lea", "Lea", "Roth"), p => p.Language = "fr");

        var reloaded = await _provider.GetRequiredService<PersonRepository<TestPerson>>().GetAsync("roth-lea");

        Assert.NotNull(reloaded);
        Assert.Equal("fr", reloaded.Language);
    }

    [AzuriteFact]
    public async Task Roles_ExclusiveAssignReplaces_ManualBlocksSync()
    {
        var assignment = new RoleAssignment("president", "club", "thun", "a-a", Period: "2026-2027");
        await _roles.AssignAsync(assignment);

        // Sync replacement works.
        var replaced = await _roles.AssignAsync(assignment with { PersonId = "b-b" });
        Assert.False(replaced.ManualConflict);
        Assert.Equal("b-b", replaced.Role.PersonId);

        // A manual entry blocks later sync writes.
        await _roles.AssignAsync(assignment with { PersonId = "c-c", Source = Sources.Manual });
        var blocked = await _roles.AssignAsync(assignment with { PersonId = "d-d" });
        Assert.True(blocked.ManualConflict);
        Assert.Equal("c-c", blocked.Role.PersonId);
    }

    [AzuriteFact]
    public async Task Roles_MemberPrefixRange_ListsOnlyTheOrg()
    {
        await _roles.AssignAsync(new RoleAssignment("member", "club", "thun", "a-a"));
        await _roles.AssignAsync(new RoleAssignment("member", "club", "thun", "b-b"));
        await _roles.AssignAsync(new RoleAssignment("member", "club", "thun-niesen", "c-c"));

        var repository = _provider.GetRequiredService<RoleRepository<RoleEntity>>();
        var definition = _provider.GetRequiredService<CrmOptions>().GetRole("member");
        var thun = await repository.ListByOrgAsync(definition, "thun", period: null);

        // "thun-niesen" members must NOT leak into the "thun" prefix range.
        Assert.Equal(["a-a", "b-b"], thun.Select(r => r.PersonId).Order().ToList());
    }

    [AzuriteFact]
    public async Task Roles_PeriodsAndFallback_NearestNewerThenOlder()
    {
        await _roles.AssignAsync(new RoleAssignment("president", "club", "bern", "a-a", Period: "2023-2024"));
        await _roles.AssignAsync(new RoleAssignment("president", "club", "bern", "b-b", Period: "2026-2027"));

        Assert.Equal(["2023-2024", "2026-2027"], await _roles.ListPeriodsAsync("president"));

        // Target year has no holder → nearest NEWER wins in the UI order, flagged as fallback.
        var resolved = await _roles.ResolveWithFallbackAsync(
            "president", "bern", "2024-2025", PeriodFallbackOrder.NewerThenOlder);

        Assert.NotNull(resolved);
        Assert.Equal("2026-2027", resolved.Period);
        Assert.False(resolved.IsExact);

        var letters = await _roles.ResolveWithFallbackAsync(
            "president", "bern", "2024-2025", PeriodFallbackOrder.OlderThenNewer);

        Assert.Equal("2023-2024", letters!.Period);
    }

    [AzuriteFact]
    public async Task Roles_RemoveMissing_KeepsManualRows()
    {
        await _roles.AssignAsync(new RoleAssignment("member", "club", "olten", "a-a"));
        await _roles.AssignAsync(new RoleAssignment("member", "club", "olten", "b-b"));
        await _roles.AssignAsync(new RoleAssignment("member", "club", "olten", "c-c", Source: Sources.Manual));

        var removed = await _roles.RemoveMissingAsync("member", "olten", period: null, seenPersonIds: new HashSet<string> { "a-a" });

        Assert.Equal(1, removed); // b-b removed; a-a seen; manual c-c kept
        var repository = _provider.GetRequiredService<RoleRepository<RoleEntity>>();
        var definition = _provider.GetRequiredService<CrmOptions>().GetRole("member");
        var remaining = await repository.ListByOrgAsync(definition, "olten", period: null);
        Assert.Equal(["a-a", "c-c"], remaining.Select(r => r.PersonId).Order().ToList());
    }

    [AzuriteFact]
    public async Task FirstnameSeed_Idempotent_ManualEntriesPreserved()
    {
        var repository = _provider.GetRequiredService<FirstnameSexRepository>();

        Assert.Equal(2, await repository.SeedAsync([("Anna", Sexes.Female), ("Beat", Sexes.Male)]));

        // Manual correction wins over a later re-seed.
        Assert.True(FirstnameSexEntity.TryNormalize("Anna", out var anna));
        await repository.UpsertAsync(new FirstnameSexEntity
        {
            PartitionKey = FirstnameSexEntity.BuildPartitionKey(anna),
            RowKey = anna,
            Sex = Sexes.Male,
            Source = Sources.Manual,
        });

        // Anna is manual, Beat unchanged — the re-seed writes NOTHING (2026-08-08: the old
        // full rewrite of every unchanged row cost minutes per deploy).
        Assert.Equal(0, await repository.SeedAsync([("Anna", Sexes.Female), ("Beat", Sexes.Male)]));
        Assert.Equal(Sexes.Male, await repository.LookupSexAsync("Anna"));

        // A CHANGED seed value is still written — only unchanged rows are skipped.
        Assert.Equal(1, await repository.SeedAsync([("Beat", Sexes.Female)]));
        Assert.Equal(Sexes.Female, await repository.LookupSexAsync("Beat"));
    }
}
