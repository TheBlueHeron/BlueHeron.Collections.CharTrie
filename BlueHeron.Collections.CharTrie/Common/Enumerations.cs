namespace BlueHeron.Collections.Trie.Search;

/// <summary>
/// Enumeration of possible ways to match a word using a <see cref="PatternMatch"/>.
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
    IsWord = 2,
    /// <summary>
    /// The pattern is a suffix (i.e. the word ends with this pattern).
    /// </summary>
    IsSuffix = 3
}

/// <summary>
/// Enumeration of possible validation states of a <see cref="PatternMatch"/>.
/// </summary>
public enum ValidationStatus
{
    /// <summary>
    /// The <see cref="PatternMatch"/> is valid.
    /// </summary>
    Valid = 0,
    /// <summary>
    /// The <see cref="PatternMatch"/> is invalid, because a wildcard as first match is not allowed.
    /// </summary>
    InvalidStartingWildCard = 1,
    /// <summary>
    /// The <see cref="PatternMatch"/> is invalid, because a wildcard as last match is not allowed.
    /// </summary>
    InvalidEndingWildCard = 2,
}