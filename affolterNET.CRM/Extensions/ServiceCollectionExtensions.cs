using affolterNET.CRM.Configuration;
using affolterNET.AzureStorage.Extensions;
using affolterNET.CRM.Repositories;
using affolterNET.CRM.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace affolterNET.CRM.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the CRM data layer: storage (via affolterNET.AzureStorage), the person /
    /// organization / role / address repositories and services for the configured concrete
    /// entity types, the firstname→sex matchlist and the salutation resolver. All singletons.
    /// Call <c>StorageInitializer.InitializeAsync</c> at startup.
    /// </summary>
    public static CrmOptions AddCrmServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<CrmOptions>? configure = null)
    {
        var options = new CrmOptions();
        configure?.Invoke(options);

        services.AddAzureStorage(configuration, core =>
        {
            core.StorageSectionName = options.StorageSectionName;
            core.TablePrefix = options.TablePrefix;
            options.ApplyTableRegistrations(core);
        });

        services.AddSingleton(options);
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<FirstnameSexRepository>();
        services.AddSingleton<IFirstnameSexLookup>(sp => sp.GetRequiredService<FirstnameSexRepository>());
        services.AddSingleton<ISalutationResolver, SalutationResolver>();
        options.ApplyServiceRegistrations(services);

        return options;
    }
}
