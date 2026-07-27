using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace affolterNET.AzureStorage.Entities;

/// <summary>
/// Validation and sanitization helpers for Azure Table Storage keys.
/// PartitionKey/RowKey must not contain '/', '\', '#', '?' or control characters.
/// </summary>
public static class KeySanitizer
{
    private const int MaxKeyLength = 512;

    /// <summary>
    /// Validates a PartitionKey/RowKey value and returns it unchanged.
    /// Throws <see cref="ArgumentException"/> for empty keys, keys longer than 512 characters,
    /// or keys containing characters that are invalid in Table Storage keys.
    /// </summary>
    public static string EnsureValidKey(string key, [CallerArgumentExpression(nameof(key))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Table Storage key must not be empty.", paramName);
        }

        if (key.Length > MaxKeyLength)
        {
            throw new ArgumentException($"Table Storage key exceeds {MaxKeyLength} characters.", paramName);
        }

        foreach (var c in key)
        {
            if (c is '/' or '\\' or '#' or '?')
            {
                throw new ArgumentException($"Table Storage key '{key}' contains invalid character '{c}'.", paramName);
            }

            // Covers U+0000-U+001F and U+007F-U+009F
            if (char.IsControl(c))
            {
                throw new ArgumentException($"Table Storage key '{key}' contains a control character (U+{(int)c:X4}).", paramName);
            }
        }

        return key;
    }

    /// <summary>
    /// Builds a key-safe slug from free text: lowercase, German umlaut folding
    /// (ä→ae, ö→oe, ü→ue, ß→ss), diacritics stripped, everything else collapsed to single hyphens.
    /// Throws <see cref="ArgumentException"/> if no slug can be derived.
    /// </summary>
    public static string ToSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Cannot build a slug from empty text.", nameof(text));
        }

        var folded = text.ToLowerInvariant()
            .Replace("ä", "ae")
            .Replace("ö", "oe")
            .Replace("ü", "ue")
            .Replace("ß", "ss");

        var normalized = folded.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        var lastWasHyphen = true; // suppresses leading hyphens

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue; // strip combining diacritics (é → e)
            }

            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                sb.Append(c);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen)
            {
                sb.Append('-');
                lastWasHyphen = true;
            }
        }

        var slug = sb.ToString().TrimEnd('-');
        if (slug.Length == 0)
        {
            throw new ArgumentException($"Cannot build a slug from '{text}'.", nameof(text));
        }

        return slug;
    }

    /// <summary>
    /// Builds an inverted-timestamp RowKey so that newer entries sort first
    /// (Table Storage returns rows in ascending key order).
    /// </summary>
    public static string InvertedTimestamp(DateTimeOffset timestamp)
    {
        return (DateTimeOffset.MaxValue.UtcTicks - timestamp.UtcTicks).ToString("D19");
    }
}
