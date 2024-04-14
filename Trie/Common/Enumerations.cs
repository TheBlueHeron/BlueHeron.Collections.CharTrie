
namespace BlueHeron.Collections.Trie;

/// <summary>
/// Enumeration of possible ways to match a character using a <see cref="CharMatch"/>.
/// </summary>
public enum CharMatchType
{
    /// <summary>
    /// Only the first character matched is considered when proceeding down the trie.
    /// </summary>
    First = 0,
    /// <summary>
    /// All matched characters are considered when proceeding down the trie.
    /// </summary>
    Any = 1
}