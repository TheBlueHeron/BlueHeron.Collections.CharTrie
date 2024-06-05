using BlueHeron.Collections.Trie.Serialization;
using System.Text.Json.Serialization;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// Wrapper for a <see cref="Trie.Node"/> that is used in deserialization and <see cref="Search.PatternMatchType.IsFragment"/> searches.
/// </summary>
/// <param name="node">The <see cref="Trie.Node"/></param>
[JsonConverter(typeof(NodeDeserializer))]
internal class NodeWrapper(Trie.Node node)
{
    #region Objects and variables

    private int? mNumChildren;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the wrapped <see cref="Trie.Node"/>.
    /// </summary>
    public Trie.Node Node = node;

    /// <summary>
    /// Gets the expected number of <see cref="Trie.Node"/>s in the <see cref="Node"/>'s <see cref="Trie.Node.Children"/> array.
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
    public bool Visited;

    #endregion
}