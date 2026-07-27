namespace affolterNET.AzureStorage.Entities;

/// <summary>
/// Field-level manual-override protection: a property listed in
/// <see cref="ITrackedEntity.ManualFields"/> was edited by a human and must never be
/// overwritten by a sync or import. Manual edit paths call <see cref="MarkFieldManual"/>;
/// sync writes go through <see cref="SetUnlessManual"/>.
/// </summary>
public static class ManualFieldExtensions
{
    private const char Separator = ';';

    /// <summary>True when the field was manually edited and is protected from sync writes.</summary>
    public static bool IsFieldManual(this ITrackedEntity entity, string fieldName)
    {
        return entity.ManualFields.Split(Separator, StringSplitOptions.RemoveEmptyEntries)
            .Contains(fieldName, StringComparer.Ordinal);
    }

    /// <summary>Marks the field as manually edited (idempotent).</summary>
    public static void MarkFieldManual(this ITrackedEntity entity, string fieldName)
    {
        if (fieldName.Contains(Separator))
        {
            throw new ArgumentException($"Field name '{fieldName}' must not contain '{Separator}'.", nameof(fieldName));
        }

        if (!entity.IsFieldManual(fieldName))
        {
            entity.ManualFields = entity.ManualFields.Length == 0
                ? fieldName
                : entity.ManualFields + Separator + fieldName;
        }
    }

    /// <summary>Applies the change only when the field is not manually protected.</summary>
    public static void SetUnlessManual(this ITrackedEntity entity, string fieldName, Action apply)
    {
        if (!entity.IsFieldManual(fieldName))
        {
            apply();
        }
    }
}
