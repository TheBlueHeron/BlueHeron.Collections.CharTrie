using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace BlueHeron.Collections.Trie.Search;

/// <summary>
/// Container for details on matching a character in a <see cref="PatternMatch"/>.
/// </summary>
[DebuggerDisplay("{Primary} ({Alternatives})")]
[DebuggerStepThrough()]
public class CharMatch
{
    #region Objects and variables

    private readonly bool mCheckAlternatives;
    private static readonly CharMatch mWildCard = new(null, null);

    #endregion

    #region Properties

    /// <summary>
    /// Gets an array of characters to match if the <see cref="Primary"/> character was not matched.
    /// </summary>
    [JsonPropertyName("a")]
    public char[]? Alternatives { get; }

    /// <summary>
    /// Gets the character for which to find a match first.
    /// If <see langword="null"/>, this <see cref="CharMatch"/> functions as a wildcard (and <see cref="Alternatives"/>`are ignored).
    /// </summary>
    [JsonPropertyName("p")]
    public char? Primary { get; }

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
    public CharMatch(char? primary)
    {
        Primary = primary;
    }

    /// <summary>
    /// Creates a new <see cref="CharMatch"/>.
    /// </summary>
    /// <param name="primary">The primary for which to find a match first</param>
    /// <param name="alternatives">Optional array of characters to match if the primary primary was not matched</param>
    /// <param name="type">A <see cref="CharMatchType"/> determining the way to match characters</param>
    [JsonConstructor()]
    public CharMatch(char? primary, char[]? alternatives)
    {
        Primary = primary;
        Alternatives = alternatives;
        mCheckAlternatives = Alternatives != null && Alternatives.Length > 0;
    }

    #endregion

    #region Public methods and functions

    /// <summary>
    /// Returns a boolean, determining whether the given character is a match for this <see cref="CharMatch"/>.
    /// </summary>
    /// <param name="character">The character to match</param>
    /// <returns>Boolean, <see langword="true"/> if the character is a match</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsMatch(char character)
    {
        if (Primary == null)
        {
            return true;
        }
        if (Primary == character)
        {
            return true;
        }
        if (mCheckAlternatives)
        {
            for (var i = 0; i < Alternatives?.Length; i++)
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