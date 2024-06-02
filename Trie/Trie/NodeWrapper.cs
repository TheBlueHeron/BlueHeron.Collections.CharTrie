
namespace BlueHeron.Collections.Trie;

/// <summary>
/// Wrapper for a <see cref="Trie2.Node"/>.
/// </summary>
/// <param name="node">The <see cref="Trie2.Node"/></param>
internal class NodeWrapper(Trie2.Node node)
{
    #region Objects and variables

    private int? mNumChildren;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the wrapped <see cref="Trie2.Node"/>.
    /// </summary>
    public Trie2.Node Node { get; } = node;

    /// <summary>
    /// The expected number of <see cref="Trie2.Node"/>s in the <see cref="Trie2.Node.Children"/> collection.
    /// </summary>
    public int NumChildren
    {
        get
        {
            if (!mNumChildren.HasValue)
            {
                mNumChildren = Node.Children.Length;
            }
            return mNumChildren.Value;
        }
        internal set => mNumChildren = value;
    }

    /// <summary>
    /// Determines whether this node already served as the start node of a walk.
    /// This prevents identical retries from occurring multiple times.
    /// </summary>
    public bool Visited { get; set; }

    #endregion
}