using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace BlueHeron.Collections.Trie.Search;

/// <summary>
/// Container for details on matching a character in a <see cref="PatternMatch"/>.
/// </summary>
[DebuggerStepThrough()]
public class CharMatch
{
    #region Objects and variables

    internal const string _DOT = ".";
    internal const string _PIPE = "|";

    private readonly bool mCheckAlternatives;
    private static readonly CharMatch mWildCard = new(null, null);

    #endregion

    #region Properties

    /// <summary>
    /// Gets an array of characters to match if the <see cref="Primary"/> character was not matched.
    /// Is ignored when <see cref="Primary"/> is <see langword="null"/>.
    /// </summary>
    [JsonPropertyName("a")]
    public char[]? Alternatives { get; }

    /// <summary>
    /// Gets the character for which to find a match first.
    /// If <see langword="null"/>, this <see cref="CharMatch"/> functions as a wildcard (and <see cref="Alternatives"/> are ignored).
    /// </summary>
    [JsonPropertyName("p")]
    public char? Primary { get; }

    /// <summary>
    /// Gets a <see cref="CharMatch"/> that matches any character.
    /// </summary>
    [JsonIgnore()]
    public static CharMatch Wildcard => mWildCard;

    #endregion

    #region Construction

    /// <summary>
    /// Creates a new <see cref="CharMatch"/>.
    /// </summary>
    /// <param name="primary">The primary character for which to find a match</param>
    public CharMatch(char? primary)
    {
        Primary = primary;
    }

    /// <summary>
    /// Creates a new <see cref="CharMatch"/>.
    /// </summary>
    /// <param name="primary">The primary character for which to find a match first</param>
    /// <param name="alternatives">Optional array of characters to match if the primary character was not matched</param>
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
#nullable disable
            for (var i = 0; i < Alternatives.Length; i++)
            {
                if (Alternatives[i] == character)
                {
                    return true;
                }
            }
#nullable enable
        }
        return false;
    }

    /// <summary>
    /// Overridden to return this <see cref="CharMatch"/> as a regex string.
    /// </summary>
    /// <returns>A regex expression</returns>
    public override string ToString()
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

    #endregion
}