using Azure.Data.Tables;

namespace affolterNET.CRM.Core.Entities;

/// <summary>
/// A table entity that knows its own default table name. Entities are registered with the
/// storage registry via <c>AddTable&lt;T&gt;()</c>; the effective table name is the registered
/// default plus an optional consumer-wide prefix. Subclasses inherit the implementation, so a
/// consumer extension entity (e.g. <c>MyPersonEntity : PersonEntity</c>) lands in the same table.
/// </summary>
public interface ICrmEntity : ITableEntity
{
    /// <summary>The table this entity is stored in when no prefix is configured.</summary>
    static abstract string DefaultTableName { get; }
}
