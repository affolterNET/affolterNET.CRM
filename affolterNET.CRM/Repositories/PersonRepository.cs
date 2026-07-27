using affolterNET.CRM.Core.Storage;
using affolterNET.CRM.Entities;

namespace affolterNET.CRM.Repositories;

/// <summary>Persons live in a single partition — point reads by PersonId, list-all for joins.</summary>
public class PersonRepository<TPerson>(IStorageClientFactory clientFactory, IStorageRegistry registry)
    : TableRepositoryBase<TPerson>(clientFactory, registry.TableNameFor(typeof(TPerson)))
    where TPerson : PersonEntity, new()
{
    public Task<TPerson?> GetAsync(string personId, CancellationToken cancellationToken = default) =>
        GetAsync(PersonEntity.Partition, personId, cancellationToken);

    /// <summary>All persons including soft-deleted ones (filter on <c>Deleted</c> where needed).</summary>
    public Task<List<TPerson>> ListAsync(CancellationToken cancellationToken = default) =>
        ListPartitionAsync(PersonEntity.Partition, cancellationToken);
}
