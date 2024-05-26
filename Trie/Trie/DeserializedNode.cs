using System.Text.Json.Serialization;
using BlueHeron.Collections.Trie.Serialization;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// A <see cref="Node"/> that has a field that represents the expected number of child <see cref="Node"/>s, needed in deserialization.
/// </summary>
[JsonConverter(typeof(NodeDeserializer))]
internal sealed class DeserializedNode: Node
{
    #region Objects and variables

    private int? mNumChildren;

    #endregion

    #region Properties

    /// <summary>
    /// The expected number of <see cref="Node"/>s in the <see cref="Node.Children"/> collection.
    /// </summary>
    public int NumChildren
    {
        get
        {
            if (!mNumChildren.HasValue)
            {
                mNumChildren = Children.Count;
            }
            return mNumChildren.Value;
        }
        internal set => mNumChildren = value;
    }

    #endregion

    #region Construction

    /// <summary>
    /// Creates a new <see cref="DeserializedNode"/>.
    /// </summary>
    internal DeserializedNode() : base(true) { }

    #endregion
}