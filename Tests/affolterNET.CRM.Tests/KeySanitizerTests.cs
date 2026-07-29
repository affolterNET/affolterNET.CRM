using affolterNET.AzureStorage.Entities;
using Xunit;

namespace affolterNET.CRM.Tests;

public class KeySanitizerTests
{
    [Theory]
    [InlineData("person")]
    [InlineData("president_2026-2027")]
    [InlineData("mueller-peter-1961-04-07")]
    public void EnsureValidKey_ValidKeys_ReturnedUnchanged(string key)
    {
        Assert.Equal(key, KeySanitizer.EnsureValidKey(key));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("a#b")]
    [InlineData("a?b")]
    [InlineData("ab")]
    public void EnsureValidKey_InvalidKeys_Throw(string key)
    {
        Assert.Throws<ArgumentException>(() => KeySanitizer.EnsureValidKey(key));
    }

    [Fact]
    public void EnsureValidKey_TooLong_Throws()
    {
        Assert.Throws<ArgumentException>(() => KeySanitizer.EnsureValidKey(new string('a', 513)));
    }

    [Theory]
    [InlineData("Müller Peter", "mueller-peter")]
    [InlineData("André Böhler", "andre-boehler")]
    [InlineData("RC Thun-Niesen", "rc-thun-niesen")]
    [InlineData("Strauß", "strauss")]
    [InlineData("  Doppel  Leerzeichen  ", "doppel-leerzeichen")]
    public void ToSlug_FoldsUmlautsAndCollapses(string text, string expected)
    {
        Assert.Equal(expected, KeySanitizer.ToSlug(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("!!!")]
    public void ToSlug_NoDerivableSlug_Throws(string text)
    {
        Assert.Throws<ArgumentException>(() => KeySanitizer.ToSlug(text));
    }

    [Fact]
    public void InvertedTimestamp_NewerSortsFirst()
    {
        var older = KeySanitizer.InvertedTimestamp(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var newer = KeySanitizer.InvertedTimestamp(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Assert.True(string.CompareOrdinal(newer, older) < 0);
    }

    [Theory]
    [InlineData("René Müller", "rene-mueller")]
    [InlineData("Éric Côté", "eric-cote")]
    [InlineData("ÀÉÎÕÛ Č Š Ž", "aeiou-c-s-z")]
    [InlineData("Ñoño", "nono")]
    [InlineData("Ørsted", "rsted")] // no decomposition → separator, as under full ICU
    [InlineData("Łukasz", "ukasz")]
    public void ToSlug_FoldsDiacritics_CultureIndependent(string input, string expected)
    {
        // The fold table must behave identically under full ICU and in the
        // globalization-invariant container runtime (divergence observed 2026-07-30).
        Assert.Equal(expected, KeySanitizer.ToSlug(input));
    }
}
