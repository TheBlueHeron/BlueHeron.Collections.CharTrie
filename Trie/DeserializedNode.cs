using System.Text.Json.Serialization;
using BlueHeron.Collections.Trie.Serialization;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// Wrapper for a <see cref="TrieNode"/> that is used in deserialization.
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
    /// This is needed because the nodes are serialized as a flat array of nodes, where a parent node is immediately followed by its children and the expected number of children is not apparent.
    /// Therefore, this number must be serialized too (during <see cref="TrieNode"/> serialization).
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