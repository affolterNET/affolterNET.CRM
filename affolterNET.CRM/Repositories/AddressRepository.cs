using affolterNET.AzureStorage;
using affolterNET.CRM.Entities;

namespace affolterNET.CRM.Repositories;

/// <summary>
/// Addresses partition by owner key (person or organization), RowKey = purpose — the base
/// <c>GetAsync(ownerKey, purpose)</c> is the point read.
/// </summary>
public class AddressRepository<TAddress>(IStorageClientFactory clientFactory, IStorageRegistry registry)
    : TableRepositoryBase<TAddress>(clientFactory, registry.TableNameFor(typeof(TAddress)))
    where TAddress : AddressEntity, new()
{
    public Task<List<TAddress>> ListForOwnerAsync(string ownerKey, CancellationToken cancellationToken = default) =>
        ListPartitionAsync(ownerKey, cancellationToken);
}
