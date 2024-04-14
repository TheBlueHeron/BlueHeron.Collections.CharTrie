
using System.Diagnostics;

namespace BlueHeron.Collections.Trie;

[DebuggerDisplay("{PrimaryCharacter} ({AlternativeCharacters})")]
public class CharMatch(char character, char[] alternatives, CharMatchType matchType = CharMatchType.First)
{
    #region Properties

    /// <summary>
    /// Array of characters to match if the primary character was not matched.
    /// </summary>
    public char[] AlternativeCharacters { get; } = alternatives;

    /// <summary>
    /// The character for which to find a match first.
    /// </summary>
    public char PrimaryCharacter { get; } = character;

    /// <summary>
    /// A <see cref="CharMatchType"/> determining the way to match characters.
    /// </summary>
    public CharMatchType Type { get; } = matchType;

    #endregion
}