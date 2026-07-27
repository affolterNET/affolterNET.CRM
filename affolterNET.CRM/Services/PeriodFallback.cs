namespace affolterNET.CRM.Services;

/// <summary>Fallback direction when the target period has no role holder.</summary>
public enum PeriodFallbackOrder
{
    /// <summary>Target, then nearest newer periods, then nearest older (UI year-selector rule).</summary>
    NewerThenOlder,

    /// <summary>Target, then nearest older periods, then nearest newer (letters prior-period rule).</summary>
    OlderThenNewer,
}

/// <summary>
/// Pure ordering of period candidates around a target. Periods are opaque strings that must
/// sort ordinally (e.g. "2026-2027").
/// </summary>
public static class PeriodFallback
{
    public static IReadOnlyList<string> OrderCandidates(
        string targetPeriod, IReadOnlyCollection<string> availablePeriods, PeriodFallbackOrder order)
    {
        var distinct = availablePeriods.Distinct(StringComparer.Ordinal).ToList();

        // Nearest first in both directions: newer ascending, older descending.
        var newer = distinct
            .Where(p => string.CompareOrdinal(p, targetPeriod) > 0)
            .OrderBy(p => p, StringComparer.Ordinal);
        var older = distinct
            .Where(p => string.CompareOrdinal(p, targetPeriod) < 0)
            .OrderByDescending(p => p, StringComparer.Ordinal);

        var result = new List<string>();
        if (distinct.Contains(targetPeriod))
        {
            result.Add(targetPeriod);
        }

        result.AddRange(order == PeriodFallbackOrder.NewerThenOlder ? newer.Concat(older) : older.Concat(newer));
        return result;
    }
}
