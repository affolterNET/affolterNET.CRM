using affolterNET.CRM.Core.Entities;
using affolterNET.CRM.Entities;
using affolterNET.CRM.Repositories;

namespace affolterNET.CRM.Services;

/// <summary>
/// Address data from a sync/import. Null fields mean "no information" and leave the stored
/// value untouched.
/// </summary>
public sealed record AddressData(
    string? CareOf = null,
    string? Street = null,
    string? StreetNr = null,
    string? Zip = null,
    string? City = null,
    string? CountryCode = null,
    string Source = Sources.Sync,
    string SyncOrigin = "");

/// <summary>
/// The single write path for addresses of persons and organizations (owner keys via
/// <see cref="AddressEntity.ForPerson"/> / <see cref="AddressEntity.ForOrganization"/>),
/// with the same ManualFields-guarded update semantics as the other services.
/// </summary>
public class AddressService<TAddress>(
    AddressRepository<TAddress> repository,
    TimeProvider timeProvider)
    where TAddress : AddressEntity, new()
{
    /// <summary>Creates or updates the owner's address for the given purpose.</summary>
    public async Task<TAddress> EnsureAsync(
        string ownerKey,
        AddressData data,
        string purpose = AddressEntity.MainPurpose,
        Action<TAddress>? applyExtensions = null,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var address = await repository.GetAsync(ownerKey, purpose, cancellationToken);

        if (address is null)
        {
            address = new TAddress
            {
                PartitionKey = ownerKey,
                RowKey = purpose,
                CareOf = data.CareOf ?? string.Empty,
                Street = data.Street ?? string.Empty,
                StreetNr = data.StreetNr ?? string.Empty,
                Zip = data.Zip ?? string.Empty,
                City = data.City ?? string.Empty,
                CountryCode = data.CountryCode ?? string.Empty,
                CreatedAt = now,
            };
        }
        else
        {
            ApplyOptional(address, nameof(AddressEntity.CareOf), data.CareOf, v => address.CareOf = v);
            ApplyOptional(address, nameof(AddressEntity.Street), data.Street, v => address.Street = v);
            ApplyOptional(address, nameof(AddressEntity.StreetNr), data.StreetNr, v => address.StreetNr = v);
            ApplyOptional(address, nameof(AddressEntity.Zip), data.Zip, v => address.Zip = v);
            ApplyOptional(address, nameof(AddressEntity.City), data.City, v => address.City = v);
            ApplyOptional(address, nameof(AddressEntity.CountryCode), data.CountryCode, v => address.CountryCode = v);
        }

        address.Deleted = false;
        address.LastSeenAt = now;
        address.UpdatedAt = now;
        address.Source = data.Source;
        if (data.SyncOrigin.Length > 0)
        {
            address.SyncOrigin = data.SyncOrigin;
        }

        applyExtensions?.Invoke(address);
        await repository.UpsertAsync(address, cancellationToken);
        return address;
    }

    /// <summary>
    /// Applies a manual edit and marks the field as manually protected. Returns the updated
    /// address, or null when it does not exist.
    /// </summary>
    public async Task<TAddress?> SetManualFieldAsync(
        string ownerKey,
        string purpose,
        string fieldName,
        Action<TAddress> apply,
        CancellationToken cancellationToken = default)
    {
        var address = await repository.GetAsync(ownerKey, purpose, cancellationToken);
        if (address is null)
        {
            return null;
        }

        apply(address);
        address.MarkFieldManual(fieldName);
        address.UpdatedAt = timeProvider.GetUtcNow();
        await repository.UpsertAsync(address, cancellationToken);
        return address;
    }

    private static void ApplyOptional(TAddress address, string fieldName, string? value, Action<string> apply)
    {
        if (value is not null)
        {
            address.SetUnlessManual(fieldName, () => apply(value));
        }
    }
}
