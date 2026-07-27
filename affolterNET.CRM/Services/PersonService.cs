using affolterNET.CRM.Core.Entities;
using affolterNET.CRM.Entities;
using affolterNET.CRM.Repositories;

namespace affolterNET.CRM.Services;

/// <summary>
/// Person data from a sync/import. Null optional fields mean "no information" and leave the
/// stored value untouched; non-null values (including empty strings) are applied.
/// </summary>
public sealed record PersonData(
    string PersonId,
    string FirstName,
    string LastName,
    string? Birthdate = null,
    string? Email = null,
    string? Phone = null,
    string? PartnerName = null,
    string Source = Sources.Sync,
    string SyncOrigin = "");

/// <summary>
/// The single write path for persons from syncs and imports. New persons get sex/salutation
/// from the <see cref="ISalutationResolver"/>; existing persons are updated field-by-field,
/// skipping manually-edited fields, and their sex/salutation is never re-derived (so a manual
/// Anrede survives every later sync). Manual edits go through <see cref="SetManualFieldAsync"/>.
/// </summary>
public class PersonService<TPerson>(
    PersonRepository<TPerson> repository,
    ISalutationResolver salutations,
    TimeProvider timeProvider)
    where TPerson : PersonEntity, new()
{
    /// <summary>Creates or updates the person and returns the stored entity.</summary>
    public async Task<TPerson> EnsureAsync(
        PersonData data, Action<TPerson>? applyExtensions = null, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var person = await repository.GetAsync(data.PersonId, cancellationToken);

        if (person is null)
        {
            var derived = await salutations.ResolveForNewPersonAsync(data.FirstName, cancellationToken);
            person = new TPerson
            {
                RowKey = data.PersonId,
                FirstName = data.FirstName,
                LastName = data.LastName,
                Birthdate = data.Birthdate ?? string.Empty,
                Email = data.Email ?? string.Empty,
                Phone = data.Phone ?? string.Empty,
                PartnerName = data.PartnerName ?? string.Empty,
                Sex = derived.Sex,
                Salutation = derived.Salutation,
                CreatedAt = now,
            };
        }
        else
        {
            person.SetUnlessManual(nameof(PersonEntity.FirstName), () => person.FirstName = data.FirstName);
            person.SetUnlessManual(nameof(PersonEntity.LastName), () => person.LastName = data.LastName);
            ApplyOptional(person, nameof(PersonEntity.Birthdate), data.Birthdate, v => person.Birthdate = v);
            ApplyOptional(person, nameof(PersonEntity.Email), data.Email, v => person.Email = v);
            ApplyOptional(person, nameof(PersonEntity.Phone), data.Phone, v => person.Phone = v);
            ApplyOptional(person, nameof(PersonEntity.PartnerName), data.PartnerName, v => person.PartnerName = v);

            // Safety net for rows created before sex/salutation existed — never a re-derivation.
            if (person.Sex.Length == 0)
            {
                var derived = await salutations.ResolveForNewPersonAsync(person.FirstName, cancellationToken);
                person.Sex = derived.Sex;
                person.SetUnlessManual(nameof(PersonEntity.Salutation), () => person.Salutation = derived.Salutation);
            }
        }

        person.Deleted = false;
        person.LastSeenAt = now;
        person.UpdatedAt = now;
        person.Source = data.Source;
        if (data.SyncOrigin.Length > 0)
        {
            person.SyncOrigin = data.SyncOrigin;
        }

        applyExtensions?.Invoke(person);
        await repository.UpsertAsync(person, cancellationToken);
        return person;
    }

    /// <summary>
    /// Applies a manual edit and marks the field as manually protected. Returns the updated
    /// person, or null when no person with this id exists.
    /// </summary>
    public async Task<TPerson?> SetManualFieldAsync(
        string personId, string fieldName, Action<TPerson> apply, CancellationToken cancellationToken = default)
    {
        var person = await repository.GetAsync(personId, cancellationToken);
        if (person is null)
        {
            return null;
        }

        apply(person);
        person.MarkFieldManual(fieldName);
        person.UpdatedAt = timeProvider.GetUtcNow();
        await repository.UpsertAsync(person, cancellationToken);
        return person;
    }

    /// <summary>Manually overrides the salutation (Anrede); protected from all later syncs.</summary>
    public Task<TPerson?> SetSalutationAsync(
        string personId, string salutation, CancellationToken cancellationToken = default) =>
        SetManualFieldAsync(personId, nameof(PersonEntity.Salutation), p => p.Salutation = salutation, cancellationToken);

    /// <summary>Sets or clears the soft-delete flag. Returns false when the person does not exist.</summary>
    public async Task<bool> SetDeletedAsync(
        string personId, bool deleted, CancellationToken cancellationToken = default)
    {
        var person = await repository.GetAsync(personId, cancellationToken);
        if (person is null)
        {
            return false;
        }

        person.Deleted = deleted;
        person.UpdatedAt = timeProvider.GetUtcNow();
        await repository.UpsertAsync(person, cancellationToken);
        return true;
    }

    private static void ApplyOptional(TPerson person, string fieldName, string? value, Action<string> apply)
    {
        if (value is not null)
        {
            person.SetUnlessManual(fieldName, () => apply(value));
        }
    }
}
