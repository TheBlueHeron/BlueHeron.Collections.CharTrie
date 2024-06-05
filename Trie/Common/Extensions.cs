using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using BlueHeron.Collections.Trie.Search;

namespace BlueHeron.Collections;

/// <summary>
/// Extension functions for use with a <see cref="Trie.Trie"/>.
/// </summary>
public static class GuidExtensions
{
    #region Fields

    private const string _MINUS = "-";
    private static readonly CompositeFormat fmtGuid = CompositeFormat.Parse("{0}-{1}-{2}-{3}-{4}");

    #endregion

    /// <summary>
    /// Returns this string as an array of <see cref="CharMatch"/> objects.
    /// </summary>
    /// <param name="text">This string</param>
    /// <returns>An <see cref="IEnumerable{CharMatch}"/></returns>
    [DebuggerStepThrough()]
    public static IEnumerable<CharMatch> ToCharMatchArray(this string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [CharMatch.Wildcard];
        }
        return text.ToCharArray().Select(c => new CharMatch(c));
    }

    /// <summary>
    /// Returns this array of characters as an array of <see cref="CharMatch"/> objects.
    /// A null value yields a wildcard.
    /// </summary>
    /// <param name="text">This string</param>
    /// <returns>An <see cref="IEnumerable{CharMatch}"/></returns>
    [DebuggerStepThrough()]
    public static IEnumerable<CharMatch> ToCharMatchArray(this IEnumerable<char?> pattern)
    {
        if (!pattern.Any())
        {
            return [CharMatch.Wildcard];
        }
        return pattern.Select(c => new CharMatch(c));
    }

    /// <summary>
    /// Returns this array of characters as a <see cref="Regex"/>.
    /// </summary>
    /// <param name="text">This string</param>
    /// <returns>An <see cref="IEnumerable{CharMatch}"/></returns>
    [DebuggerStepThrough()]
    public static Regex ToRegex(this IEnumerable<char?> pattern)
    {
        return new PatternMatch(pattern, PatternMatchType.IsFragment).ToRegex();
    }

    /// <summary>
    /// Returns this <see cref="PatternMatch"/> as a <see cref="Regex"/>.
    /// </summary>
    /// <param name="patternMatch">This <see cref="PatternMatch"/></param>
    /// <returns>A <see cref="Regex"/></returns>
    /// <exception cref="ArgumentException">The <see cref="PatternMatch"/> cannot be converted to a <see cref="Regex"/>, i.e. is empty</exception>
    public static Regex ToRegex(this PatternMatch patternMatch)
    {
        var strRegex = patternMatch.ToString();
        ArgumentException.ThrowIfNullOrEmpty(strRegex, nameof(patternMatch));
        return new Regex(strRegex);
    }

    /// <summary>
    /// Tries to create a <see cref="Guid"/> from the given string.
    /// </summary>
    /// <param name="id">The condensed string representation of the <see cref="Guid"/>, i.e. the guid without minus signs</param>
    /// <returns>A <see cref="Guid"/></returns>
    /// <exception cref="ArgumentOutOfRangeException">The given string cannot be converted into a <see cref="Guid"/></exception>
    [DebuggerStepThrough()]
    public static Guid ToGuid(this string id)
    {
        if (!string.IsNullOrEmpty(id) && id.Length == 32)
        {
            Guid guid;
            if (Guid.TryParse(string.Format(null, fmtGuid,
                id[..8],
                id.Substring(8, 4),
                id.Substring(12, 4),
                id.Substring(16, 4),
                id.Substring(20, 12)), out guid))
            {
                return guid;
            }
        }
        throw new ArgumentOutOfRangeException(nameof(id));
    }

    /// <summary>
    /// Converts this <see cref="Guid"/> to a string for use in a <see cref="Trie.Trie"/>.
    /// </summary>
    /// <param name="id">This <see cref="Guid"/></param>
    /// <returns>A <see cref="string"/> containing only numbers</returns>
    [DebuggerStepThrough()]
    public static string ToWord(this Guid id)
    {
        return id.ToString().Replace(_MINUS, string.Empty, StringComparison.InvariantCultureIgnoreCase);
    }
}