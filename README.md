# affolterNET.CRM

A generic **person / organization / role** CRM data layer on **Azure Table Storage**,
split into two NuGet packages:

| Package | Contents |
|---|---|
| **affolterNET.AzureStorage** | Generic Table-Storage primitives with zero CRM knowledge: `TableRepositoryBase<T>`, `KeySanitizer`, `StorageClientFactory` (connection string → user-assigned managed identity → `DefaultAzureCredential`), registry-driven `StorageInitializer`, `IStorageEntity`, `ITrackedEntity` + field-level manual-override protection. Usable by any project that stores entities in Azure Table Storage. |
| **affolterNET.CRM** | The party-role model on top of affolterNET.AzureStorage: `PersonEntity`, `OrganizationEntity`, `RoleEntity`, `AddressEntity`, the firstname→sex matchlist with `SalutationResolver` (Anrede derivation), period-aware role resolution with nearest-period fallback, and `Ensure*` services that respect manual edits. |

## Model

- **persons** — one row per real human (single partition). Deterministic `PersonId` via
  `PersonEntity.BuildPersonKey(lastName, firstName, birthdate?)`; birthdate distinguishes
  same-named people and unifies the same person across contexts. `Sex` and `Salutation`
  are derived from the matchlist **at creation only** and never re-derived.
- **organizations** — partitioned by consumer-registered `OrgType` (e.g. `club`,
  `district`, `organization`), with parent references for hierarchies.
- **roles** — person ⇄ organization links with a `RoleDefinition(roleType, hasPeriod,
  exclusivePerOrg)`. Holders of a role type+period are one partition; members of one org
  are a RowKey prefix range. Past-period rows are never touched by syncs.
- **addresses** — per owner (person or org) and purpose, with a `CareOf` line.
- **firstnamesex** — the firstname→sex matchlist. The library ships the mechanism and an
  idempotent `SeedAsync`; the data (e.g. an open-data firstname list) is consumer-supplied.

### Provenance & manual-override protection

Every tracked entity carries `Source` (sync/manual/import), `SyncOrigin`, `CreatedAt`,
`UpdatedAt`, `LastSeenAt`, a soft-delete `Deleted` flag (syncs never physically delete)
and `ManualFields` — a list of manually edited properties that **no sync write will ever
overwrite** (`SetUnlessManual`). Manual role rows are never replaced or removed by syncs.

## Usage

```csharp
var crm = services.AddCrmServices(configuration, o =>
{
    o.StorageSectionName = "affolterNET:MyApp:Storage"; // reuse existing config/env vars
    o.AddOrganizationType("club").AddOrganizationType("district");
    o.AddRole("member", hasPeriod: false, exclusivePerOrg: false);
    o.AddRole("president", hasPeriod: true, exclusivePerOrg: true);
    o.UsePersonType<MyPersonEntity>();          // consumer subclass with extra columns
    o.AddTable<MyDomainEntity>();               // consumer-owned tables, created at startup
});
```

Entities are extended by **inheritance** (`MyPersonEntity : PersonEntity`) — Table Storage
stores the extra properties in the same table; all repositories/services are generic over
the concrete type, so no write path can drop consumer columns.

Note: Azure Table Storage has no `decimal` EDM type — consumer entities with money
amounts use `double`.

## License

MIT
