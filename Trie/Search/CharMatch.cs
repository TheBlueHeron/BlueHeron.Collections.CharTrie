using System.Diagnostics;
using System.Text.Json.Serialization;

namespace BlueHeron.Collections.Trie.Search;

/// <summary>
/// Container for details on matching a primary in a pattern match.
/// </summary>
/// <param name="character">The primary primary to match</param>
/// <param name="alternatives">Optional alternative characters to match</param>
/// <param name="matchType">The <see cref="CharMatchType"/> to use</param>
[DebuggerDisplay("{Type}: {Primary} ({Alternatives})")]
public class CharMatch
{
    #region Objects and variables

    private static readonly CharMatch mWildCard = new();

    #endregion

    #region Properties

    /// <summary>
    /// Gets an array of characters to match if the primary primary was not matched.
    /// </summary>
    [JsonPropertyName("a")]
    public char[]? Alternatives { get; }

    /// <summary>
    /// Gets the primary for which to find a match first.
    /// </summary>
    [JsonPropertyName("p")]
    public char? Primary { get; }

    /// <summary>
    /// Gets the <see cref="CharMatchType"/> determining the way to match characters. Default: <see cref="CharMatchType.First"/>.
    /// </summary>
    [JsonPropertyName("t")]
    public CharMatchType Type { get; }

    /// <summary>
    /// Gets a <see cref="CharMatch"/> that always is a match.
    /// </summary>
    [JsonIgnore()]
    public static CharMatch Wildcard => mWildCard;

    #endregion

    #region Construction

    /// <summary>
    /// Creates a new <see cref="CharMatch"/>.
    /// </summary>
    /// <param name="primary">The primary for which to find a match first</param>
    /// <param name="type">A <see cref="CharMatchType"/> determining the way to match characters</param>
    public CharMatch(char? primary, CharMatchType type = CharMatchType.First)
    {
        Primary = primary;
        Type = type;
    }

    /// <summary>
    /// Creates a new <see cref="CharMatch"/>.
    /// </summary>
    /// <param name="primary">The primary for which to find a match first</param>
    /// <param name="alternatives">Optional array of characters to match if the primary primary was not matched</param>
    /// <param name="type">A <see cref="CharMatchType"/> determining the way to match characters</param>
    [JsonConstructor()]
    public CharMatch(char? primary, char[]? alternatives, CharMatchType type = CharMatchType.First) : this(primary, type)
    {
        Alternatives = alternatives;
    }

    /// <summary>
    /// Private constructor to create a <see cref="CharMatch"/> that represents a wildcard.
    /// </summary>
    private CharMatch()
    {
        Type = CharMatchType.All;
    }

    #endregion

    #region Public methods and functions

    /// <summary>
    /// Returns a boolean, determining whether the given primary is a match for this <see cref="CharMatch"/>.
    /// </summary>
    /// <param name="character">The primary to match</param>
    /// <returns>Boolean, <see langword="true"/> if the primary is a match</returns>
    public bool IsMatch(char character)
    {
        if (Type == CharMatchType.All)
        {
            return true;
        }
        if (Primary == character)
        {
            return true;
        }
        if (Alternatives != null && Alternatives.Length > 0)
        {
            for (var i = 0; i < Alternatives.Length; i++)
            {
                if (Alternatives[i] == character)
                {
                    return true;
                }
            }
        }
        return false;
    }

    #endregion
}