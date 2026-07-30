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
    /// CULTURE-INDEPENDENT by design: no ICU casing or Unicode normalization — in the
    /// globalization-invariant runtime of the Alpine container images, ToLowerInvariant
    /// only cases ASCII and Normalize does not decompose, so the same name produced
    /// DIFFERENT slugs in the container than on a dev machine (observed 2026-07-30:
    /// 2'056 of 62'876 BFS firstnames diverged). The explicit fold table reproduces the
    /// former full-ICU behavior, so existing host-generated keys stay stable.
    /// </summary>
    public static string ToSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Cannot build a slug from empty text.", nameof(text));
        }

        var sb = new StringBuilder(text.Length);
        var lastWasHyphen = true; // suppresses leading hyphens

        foreach (var c in text)
        {
            var mapped = FoldChar(c);
            if (mapped.Length > 0)
            {
                sb.Append(mapped);
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
    /// Ordinal char fold: a-z/0-9 pass, A-Z lowercase, German specials fold, Latin letters
    /// with combining diacritics map to their base letter (matching what NFD + mark-strip
    /// produced under full ICU). Characters WITHOUT a decomposition (ø, æ, ł, đ, ı, …)
    /// return empty — they became separators before and must stay separators.
    /// </summary>
    private static string FoldChar(char c)
    {
        if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
        {
            return c.ToString();
        }

        if (c is >= 'A' and <= 'Z')
        {
            return ((char)(c + 32)).ToString();
        }

        return c switch
        {
            'ä' or 'Ä' => "ae",
            'ö' or 'Ö' => "oe",
            'ü' or 'Ü' => "ue",
            'ß' => "ss",
            'à' or 'á' or 'â' or 'ã' or 'å' or 'ā' or 'ă' or 'ą'
                or 'À' or 'Á' or 'Â' or 'Ã' or 'Å' or 'Ā' or 'Ă' or 'Ą' => "a",
            'è' or 'é' or 'ê' or 'ë' or 'ē' or 'ĕ' or 'ė' or 'ę' or 'ě'
                or 'È' or 'É' or 'Ê' or 'Ë' or 'Ē' or 'Ĕ' or 'Ė' or 'Ę' or 'Ě' => "e",
            'ì' or 'í' or 'î' or 'ï' or 'ĩ' or 'ī' or 'ĭ' or 'į'
                or 'Ì' or 'Í' or 'Î' or 'Ï' or 'Ĩ' or 'Ī' or 'Ĭ' or 'Į' or 'İ' => "i",
            'ò' or 'ó' or 'ô' or 'õ' or 'ō' or 'ŏ' or 'ő'
                or 'Ò' or 'Ó' or 'Ô' or 'Õ' or 'Ō' or 'Ŏ' or 'Ő' => "o",
            'ù' or 'ú' or 'û' or 'ũ' or 'ū' or 'ŭ' or 'ů' or 'ű' or 'ų'
                or 'Ù' or 'Ú' or 'Û' or 'Ũ' or 'Ū' or 'Ŭ' or 'Ů' or 'Ű' or 'Ų' => "u",
            'ç' or 'ć' or 'ĉ' or 'ċ' or 'č' or 'Ç' or 'Ć' or 'Ĉ' or 'Ċ' or 'Č' => "c",
            'ñ' or 'ń' or 'ņ' or 'ň' or 'Ñ' or 'Ń' or 'Ņ' or 'Ň' => "n",
            'ý' or 'ÿ' or 'Ý' or 'Ÿ' => "y",
            'ś' or 'ŝ' or 'ş' or 'š' or 'Ś' or 'Ŝ' or 'Ş' or 'Š' => "s",
            'ź' or 'ż' or 'ž' or 'Ź' or 'Ż' or 'Ž' => "z",
            'ĝ' or 'ğ' or 'ġ' or 'ģ' or 'Ĝ' or 'Ğ' or 'Ġ' or 'Ģ' => "g",
            'ĥ' or 'Ĥ' => "h",
            'ĵ' or 'Ĵ' => "j",
            'ķ' or 'Ķ' => "k",
            'ĺ' or 'ļ' or 'ľ' or 'Ĺ' or 'Ļ' or 'Ľ' => "l",
            'ŕ' or 'ŗ' or 'ř' or 'Ŕ' or 'Ŗ' or 'Ř' => "r",
            'ţ' or 'ť' or 'Ţ' or 'Ť' => "t",
            'ŵ' or 'Ŵ' => "w",
            'ď' or 'Ď' => "d",
            _ => string.Empty,
        };
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
