using affolterNET.CRM.Core.Entities;
using affolterNET.CRM.Core.Extensions;
using affolterNET.CRM.Core.Storage;
using affolterNET.CRM.Entities;
using affolterNET.CRM.Repositories;
using affolterNET.CRM.Services;
using Microsoft.Extensions.DependencyInjection;

namespace affolterNET.CRM.Configuration;

/// <summary>
/// Registration options for <c>AddCrmServices</c>: storage binding, organization types,
/// role definitions, the concrete entity types (consumer subclasses of the core entities),
/// additional consumer tables/containers and the salutation rules.
/// </summary>
public sealed class CrmOptions
{
    private readonly Dictionary<string, RoleDefinition> _roles = new(StringComparer.Ordinal);
    private readonly HashSet<string> _organizationTypes = new(StringComparer.Ordinal);
    private readonly List<Action<CrmCoreStorageOptions>> _consumerTables = [];
    private readonly List<string> _blobContainers = [];

    private Action<IServiceCollection> _personServices = RegisterPerson<PersonEntity>;
    private Action<CrmCoreStorageOptions> _personTable = core => core.AddTable<PersonEntity>();
    private Action<IServiceCollection> _organizationServices = RegisterOrganization<OrganizationEntity>;
    private Action<CrmCoreStorageOptions> _organizationTable = core => core.AddTable<OrganizationEntity>();
    private Action<IServiceCollection> _roleServices = RegisterRole<RoleEntity>;
    private Action<CrmCoreStorageOptions> _roleTable = core => core.AddTable<RoleEntity>();
    private Action<IServiceCollection> _addressServices = RegisterAddress<AddressEntity>;
    private Action<CrmCoreStorageOptions> _addressTable = core => core.AddTable<AddressEntity>();

    /// <summary>Configuration section the storage options are bound from.</summary>
    public string StorageSectionName { get; set; } = CrmStorageOptions.DefaultSectionName;

    /// <summary>Optional prefix applied to every table name.</summary>
    public string TablePrefix { get; set; } = string.Empty;

    /// <summary>Sex assumed when the firstname matchlist has no entry. Default: male.</summary>
    public string DefaultSex { get; set; } = Sexes.Male;

    /// <summary>Salutation per sex; the library core is language-neutral, defaults are German.</summary>
    public Dictionary<string, string> SalutationBySex { get; } = new(StringComparer.Ordinal)
    {
        [Sexes.Male] = "Herr",
        [Sexes.Female] = "Frau",
    };

    /// <summary>All registered role definitions.</summary>
    public IReadOnlyCollection<RoleDefinition> Roles => _roles.Values;

    /// <summary>Declares a role type (see <see cref="RoleDefinition"/> for the semantics).</summary>
    public CrmOptions AddRole(string roleType, bool hasPeriod, bool exclusivePerOrg)
    {
        if (roleType.Contains(RoleEntity.Separator))
        {
            throw new ArgumentException($"Role type '{roleType}' must not contain '{RoleEntity.Separator}'.", nameof(roleType));
        }

        KeySanitizer.EnsureValidKey(roleType, nameof(roleType));
        _roles[roleType] = new RoleDefinition(roleType, hasPeriod, exclusivePerOrg);
        return this;
    }

    /// <summary>Declares an organization type (the organizations table partition, e.g. "club").</summary>
    public CrmOptions AddOrganizationType(string orgType)
    {
        KeySanitizer.EnsureValidKey(orgType, nameof(orgType));
        _organizationTypes.Add(orgType);
        return this;
    }

    /// <summary>Registers a consumer-owned entity type (its table is created and resolvable).</summary>
    public CrmOptions AddTable<T>() where T : ICrmEntity
    {
        _consumerTables.Add(core => core.AddTable<T>());
        return this;
    }

    /// <summary>Registers a blob container to create at startup.</summary>
    public CrmOptions AddBlobContainer(string containerName)
    {
        _blobContainers.Add(containerName);
        return this;
    }

    /// <summary>Uses a consumer subclass of <see cref="PersonEntity"/> as the person type.</summary>
    public CrmOptions UsePersonType<TPerson>() where TPerson : PersonEntity, new()
    {
        _personServices = RegisterPerson<TPerson>;
        _personTable = core => core.AddTable<TPerson>();
        return this;
    }

    /// <summary>Uses a consumer subclass of <see cref="OrganizationEntity"/> as the organization type.</summary>
    public CrmOptions UseOrganizationType<TOrg>() where TOrg : OrganizationEntity, new()
    {
        _organizationServices = RegisterOrganization<TOrg>;
        _organizationTable = core => core.AddTable<TOrg>();
        return this;
    }

    /// <summary>Uses a consumer subclass of <see cref="RoleEntity"/> as the role type.</summary>
    public CrmOptions UseRoleType<TRole>() where TRole : RoleEntity, new()
    {
        _roleServices = RegisterRole<TRole>;
        _roleTable = core => core.AddTable<TRole>();
        return this;
    }

    /// <summary>Uses a consumer subclass of <see cref="AddressEntity"/> as the address type.</summary>
    public CrmOptions UseAddressType<TAddress>() where TAddress : AddressEntity, new()
    {
        _addressServices = RegisterAddress<TAddress>;
        _addressTable = core => core.AddTable<TAddress>();
        return this;
    }

    /// <summary>The definition for a registered role type; throws for unknown types.</summary>
    public RoleDefinition GetRole(string roleType)
    {
        if (!_roles.TryGetValue(roleType, out var definition))
        {
            throw new InvalidOperationException(
                $"Role type '{roleType}' is not registered. Known: {string.Join(", ", _roles.Keys)}.");
        }

        return definition;
    }

    /// <summary>True when the organization type was registered via <see cref="AddOrganizationType"/>.</summary>
    public bool IsOrganizationTypeRegistered(string orgType) => _organizationTypes.Contains(orgType);

    internal void ApplyTableRegistrations(CrmCoreStorageOptions core)
    {
        _personTable(core);
        _organizationTable(core);
        _roleTable(core);
        _addressTable(core);
        core.AddTable<FirstnameSexEntity>();
        foreach (var register in _consumerTables)
        {
            register(core);
        }

        foreach (var container in _blobContainers)
        {
            core.AddBlobContainer(container);
        }
    }

    internal void ApplyServiceRegistrations(IServiceCollection services)
    {
        _personServices(services);
        _organizationServices(services);
        _roleServices(services);
        _addressServices(services);
    }

    private static void RegisterPerson<TPerson>(IServiceCollection services) where TPerson : PersonEntity, new()
    {
        services.AddSingleton<PersonRepository<TPerson>>();
        services.AddSingleton<PersonService<TPerson>>();
    }

    private static void RegisterOrganization<TOrg>(IServiceCollection services) where TOrg : OrganizationEntity, new()
    {
        services.AddSingleton<OrganizationRepository<TOrg>>();
        services.AddSingleton<OrganizationService<TOrg>>();
    }

    private static void RegisterRole<TRole>(IServiceCollection services) where TRole : RoleEntity, new()
    {
        services.AddSingleton<RoleRepository<TRole>>();
        services.AddSingleton<RoleService<TRole>>();
    }

    private static void RegisterAddress<TAddress>(IServiceCollection services) where TAddress : AddressEntity, new()
    {
        services.AddSingleton<AddressRepository<TAddress>>();
        services.AddSingleton<AddressService<TAddress>>();
    }
}
