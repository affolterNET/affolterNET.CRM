using affolterNET.CRM.Services;
using Xunit;

namespace affolterNET.CRM.Tests;

public class PeriodFallbackTests
{
    private static readonly string[] Periods =
        ["2022-2023", "2023-2024", "2024-2025", "2026-2027", "2027-2028"];

    [Fact]
    public void NewerThenOlder_TargetPresent_TargetFirstThenNearestNewer()
    {
        var ordered = PeriodFallback.OrderCandidates("2024-2025", Periods, PeriodFallbackOrder.NewerThenOlder);

        Assert.Equal(["2024-2025", "2026-2027", "2027-2028", "2023-2024", "2022-2023"], ordered);
    }

    [Fact]
    public void NewerThenOlder_TargetMissing_NearestNewerFirst()
    {
        // "2025-2026" has no data — the year-selector rule: nearest newer, then older.
        var ordered = PeriodFallback.OrderCandidates("2025-2026", Periods, PeriodFallbackOrder.NewerThenOlder);

        Assert.Equal(["2026-2027", "2027-2028", "2024-2025", "2023-2024", "2022-2023"], ordered);
    }

    [Fact]
    public void OlderThenNewer_TargetMissing_NearestOlderFirst()
    {
        // The letters rule: prior period preferred over future ones.
        var ordered = PeriodFallback.OrderCandidates("2025-2026", Periods, PeriodFallbackOrder.OlderThenNewer);

        Assert.Equal(["2024-2025", "2023-2024", "2022-2023", "2026-2027", "2027-2028"], ordered);
    }

    [Fact]
    public void OrderCandidates_EmptyAvailable_Empty()
    {
        Assert.Empty(PeriodFallback.OrderCandidates("2025-2026", [], PeriodFallbackOrder.NewerThenOlder));
    }

    [Fact]
    public void OrderCandidates_DuplicatesRemoved()
    {
        var ordered = PeriodFallback.OrderCandidates(
            "2024-2025", ["2023-2024", "2023-2024", "2024-2025"], PeriodFallbackOrder.NewerThenOlder);

        Assert.Equal(["2024-2025", "2023-2024"], ordered);
    }
}
