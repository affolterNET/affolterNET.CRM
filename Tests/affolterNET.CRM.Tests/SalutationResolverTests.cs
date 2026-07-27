using affolterNET.CRM.Configuration;
using affolterNET.CRM.Entities;
using affolterNET.CRM.Services;
using Xunit;

namespace affolterNET.CRM.Tests;

public class SalutationResolverTests
{
    private sealed class FakeLookup(Dictionary<string, string> entries) : IFirstnameSexLookup
    {
        public Task<string?> LookupSexAsync(string firstName, CancellationToken cancellationToken = default)
        {
            FirstnameSexEntity.TryNormalize(firstName, out var normalized);
            return Task.FromResult(entries.TryGetValue(normalized, out var sex) ? sex : null);
        }
    }

    private static readonly Dictionary<string, string> Matchlist = new()
    {
        ["vera"] = Sexes.Female,
        ["peter"] = Sexes.Male,
    };

    [Fact]
    public async Task KnownFemaleName_ResolvesFrau()
    {
        var resolver = new SalutationResolver(new FakeLookup(Matchlist), new CrmOptions());

        var result = await resolver.ResolveForNewPersonAsync("Vera");

        Assert.Equal(Sexes.Female, result.Sex);
        Assert.Equal("Frau", result.Salutation);
    }

    [Fact]
    public async Task UnknownName_DefaultsToMaleHerr()
    {
        var resolver = new SalutationResolver(new FakeLookup(Matchlist), new CrmOptions());

        var result = await resolver.ResolveForNewPersonAsync("Xyzzles");

        Assert.Equal(Sexes.Male, result.Sex);
        Assert.Equal("Herr", result.Salutation);
    }

    [Fact]
    public async Task CompositeFirstname_UsesFirstToken()
    {
        var resolver = new SalutationResolver(new FakeLookup(Matchlist), new CrmOptions());

        var result = await resolver.ResolveForNewPersonAsync("Vera Maria");

        Assert.Equal(Sexes.Female, result.Sex);
    }

    [Fact]
    public void SalutationFor_UnknownSex_FallsBackToDefaultSexSalutation()
    {
        var resolver = new SalutationResolver(new FakeLookup([]), new CrmOptions());

        Assert.Equal("Herr", resolver.SalutationFor("unknown"));
    }

    [Fact]
    public async Task ConfigurableSalutations_AreApplied()
    {
        var options = new CrmOptions();
        options.SalutationBySex[Sexes.Female] = "Madame";
        var resolver = new SalutationResolver(new FakeLookup(Matchlist), options);

        var result = await resolver.ResolveForNewPersonAsync("Vera");

        Assert.Equal("Madame", result.Salutation);
    }
}
