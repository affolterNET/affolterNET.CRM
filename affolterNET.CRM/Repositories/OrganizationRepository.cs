using affolterNET.CRM.Core.Storage;
using affolterNET.CRM.Entities;

namespace affolterNET.CRM.Repositories;

/// <summary>Organizations partition by OrgType — "all clubs" / "all districts" are one partition.</summary>
public class OrganizationRepository<TOrg>(IStorageClientFactory clientFactory, IStorageRegistry registry)
    : TableRepositoryBase<TOrg>(clientFactory, registry.TableNameFor(typeof(TOrg)))
    where TOrg : OrganizationEntity, new()
{
    public Task<List<TOrg>> ListByTypeAsync(string orgType, CancellationToken cancellationToken = default) =>
        ListPartitionAsync(orgType, cancellationToken);

    /// <summary>Children of one parent (e.g. the clubs of a district).</summary>
    public async Task<List<TOrg>> ListChildrenAsync(
        string childOrgType, string parentOrgId, CancellationToken cancellationToken = default)
    {
        var children = await ListPartitionAsync(childOrgType, cancellationToken);
        return children.Where(o => o.ParentOrgId == parentOrgId).ToList();
    }
}
