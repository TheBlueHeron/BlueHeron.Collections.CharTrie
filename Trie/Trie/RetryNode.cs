namespace BlueHeron.Collections.Trie;

/// <summary>
/// A <see cref="Node"/> that serves a a starting point of a retry in a <see cref="Search.PatternMatchType.IsFragment"/> search.
/// </summary>
internal sealed class RetryNode : Node
{
    #region Properties

    /// <summary>
    /// Determines whether this node already served as the start node of a walk.
    /// This prevents identical retries from occurring multiple times.
    /// </summary>
    public bool Visited { get; set; }

    #endregion

    #region Construction

    /// <summary>
    /// Creastes a new <see cref="RetryNode"/>.
    /// </summary>
    /// <param name="children">The <see cref="HashSet{(char, Node)}"/> containing the child nodes</param>
    /// <param name="remainingDepth">The <see cref="Node.RemainingDepth"/> value</param>
    /// <param name="isWord">The <see cref="Node.IsWord"/> value</param>
    public RetryNode(HashSet<(char, Node)> children, int remainingDepth, bool isWord)
    {
        mChildren = children;
        mRemainingDepth = remainingDepth;
        IsWord = isWord;
    }

    #endregion
}