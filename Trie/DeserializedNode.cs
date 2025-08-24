using System.Text.Json.Serialization;
using BlueHeron.Collections.Trie.Serialization;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// Wrapper for a <see cref="TrieNode"/> that is used in deserialization and <see cref="Search.PatternMatchType.IsFragment"/> searches.
/// </summary>
/// <param name="node">The <see cref="TrieNode"/> to wrap</param>
[JsonConverter(typeof(NodeDeserializer))]
internal sealed class DeserializedNode(TrieNode node)
{
    #region Objects and variables

    private int? mNumChildren;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the wrapped <see cref="TrieNode"/>.
    /// </summary>
    public TrieNode Node = node;

    /// <summary>
    /// Gets the expected number of <see cref="TrieNode"/>s in the <see cref="TrieNode"/>'s <see cref="TrieNode.Children"/> array.
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

    #endregion
}