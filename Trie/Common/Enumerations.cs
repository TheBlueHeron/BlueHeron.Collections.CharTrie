
namespace BlueHeron.Collections.Trie.Search;

/// <summary>
/// Enumeration of possible ways to match a character with the <see cref="CharMatch"/>.
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
    Any = 1,
    /// <summary>
    /// All encountered characters are a match.
    /// </summary>
    All = 2
}

/// <summary>
/// Enumeration of possible ways to match a word with the <see cref="PatternMatch"/>.
/// </summary>
public enum PatternMatchType
{
    /// <summary>
    /// The pattern is a prefix (i.e the word starts with this pattern).
    /// </summary>
    IsPrefix = 0,
    /// <summary>
    /// The pattern is a fragment (i.e. the word contains this pattern).
    /// </summary>
    IsFragment = 1,
    /// <summary>
    /// The pattern and its length match with the word and its length.
    /// </summary>
    IsWord = 2
}