using System.Diagnostics;
using System.Text.Json.Serialization;

namespace BlueHeron.Collections.Trie.Search;

/// <summary>
/// Container for details on matching a character in a <see cref="PatternMatch"/>.
/// </summary>
[DebuggerStepThrough()]
public struct CharMatch : IEquatable<CharMatch>
{
    #region Objects and variables

    private const string _DOT = ".";
    private const string _PIPE = "|";

    private readonly bool mCheckAlternatives;
    private static readonly CharMatch mWildCard = new(null, null);

    #endregion

    #region Properties

    /// <summary>
    /// Gets an <see cref="IReadOnlyList{char}"/> to match if the <see cref="Primary"/> character was not matched.
    /// Is ignored when <see cref="Primary"/> is <see langword="null"/>, i.e. is a wildcard.
    /// </summary>
    [JsonPropertyName("a")]
    public IReadOnlyList<char>? Alternatives { get; }

    /// <summary>
    /// Returns a boolean, determining whether this <see cref="CharMatch"/> represents a wildcard.
    /// </summary>
    [JsonIgnore()]
    public readonly bool IsWildCard => Primary == null && Alternatives == null;

    /// <summary>
    /// Gets the character for which to find a match first.
    /// If <see langword="null"/>, this <see cref="CharMatch"/> functions as a wildcard (and the <see cref="Alternatives"/> are ignored).
    /// </summary>
    [JsonPropertyName("p")]
    public char? Primary { get; }

    /// <summary>
    /// Gets a <see cref="CharMatch"/> that will match any character.
    /// </summary>
    [JsonIgnore()]
    public static CharMatch Wildcard => mWildCard;

    #endregion

    #region Construction

    /// <summary>
    /// Creates a new <see cref="CharMatch"/>.
    /// </summary>
    /// <param name="primary">The primary character for which to find a match</param>
    [DebuggerStepThrough()]
    public CharMatch(char? primary)
    {
        Primary = primary;
    }

    /// <summary>
    /// Creates a new <see cref="CharMatch"/>.
    /// </summary>
    /// <param name="primary">The primary character for which to find a match first</param>
    /// <param name="alternatives">Optional <see cref="IReadOnlyList{char}"/> to match if the primary character was not matched</param>
    [JsonConstructor()]
    public CharMatch(char? primary, IReadOnlyList<char>? alternatives)
    {
        Primary = primary;
        Alternatives = alternatives;
        mCheckAlternatives = Alternatives != null && Alternatives.Count > 0;
    }

    #endregion

    #region Public methods and functions

    /// <summary>
    /// Returns a value, determining whether the given character is a match for this <see cref="CharMatch"/>.
    /// </summary>
    /// <param name="character">The character to match</param>
    /// <returns><see langword="bool"/>, <see langword="true"/> if the character is a match</returns>
    [DebuggerStepThrough()]
    public readonly bool IsMatch(char character)
    {
        if (Primary == null)
        {
            return true;
        }
        if (Primary == character)
        {
            return true;
        }
        if (mCheckAlternatives && Alternatives != null)
        {
            for (var i = 0; i < Alternatives.Count; i++)
            {
                if (Alternatives[i] == character)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Overridden to return this <see cref="CharMatch"/> as a regex string.
    /// </summary>
    /// <returns>A regex expression</returns>
    public readonly override string ToString()
    {
        if (Primary == null)
        {
            return _DOT;
        }
        if (Alternatives == null)
        {
            return $"{Primary}";
        }
        return $"[{string.Join(_PIPE, Primary, Alternatives)}]";
    }

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj)
    {
        return obj is CharMatch match && Equals(match);
    }

    /// <summary>
    /// Returns a value indicating whether this <see cref="CharMatch"/> is equal to the given <see cref="CharMatch"/>.
    /// </summary>
    /// <param name="left">The left <see cref="CharMatch"/></param>
    /// <returns><see langword="true"/> if this <see cref="CharMatch"/> is equal to the other; else <see langword="false"/></returns>
    public readonly bool Equals(CharMatch other) => this == other;

    /// <inheritdoc/>
    public readonly override int GetHashCode()
    {
        return ToString()?.GetHashCode(StringComparison.InvariantCulture) ?? 0;
    }

    /// <summary>
    /// Returns a value indicating whether the given <see cref="CharMatch"/>es are equal.
    /// </summary>
    /// <param name="left">The left <see cref="CharMatch"/></param>
    /// <param name="right">The right <see cref="CharMatch"/></param>
    /// <returns><see langword="true"/> if the <see cref="CharMatch"/>es are equal; else <see langword="false"/></returns>
    public static bool operator ==(CharMatch left, CharMatch right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Returns a value indicating whether the given <see cref="CharMatch"/>es are not equal.
    /// </summary>
    /// <param name="left">The left <see cref="CharMatch"/></param>
    /// <param name="right">The right <see cref="CharMatch"/></param>
    /// <returns><see langword="true"/> if the <see cref="CharMatch"/>es are not equal; else <see langword="false"/></returns>
    public static bool operator !=(CharMatch left, CharMatch right)
    {
        return !(left == right);
    }

    #endregion
}