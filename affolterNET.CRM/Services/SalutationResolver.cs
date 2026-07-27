using affolterNET.CRM.Configuration;

namespace affolterNET.CRM.Services;

/// <inheritdoc />
public sealed class SalutationResolver(IFirstnameSexLookup lookup, CrmOptions options) : ISalutationResolver
{
    public async Task<SexAndSalutation> ResolveForNewPersonAsync(
        string firstName, CancellationToken cancellationToken = default)
    {
        var sex = await lookup.LookupSexAsync(firstName, cancellationToken) ?? options.DefaultSex;
        return new SexAndSalutation(sex, SalutationFor(sex));
    }

    public string SalutationFor(string sex) =>
        options.SalutationBySex.TryGetValue(sex, out var salutation)
            ? salutation
            : options.SalutationBySex[options.DefaultSex];
}
