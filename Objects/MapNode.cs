
namespace BlueHeron.Collections.Trie;

/// <summary>
/// A node in a <see cref="ITrie{TNode, TValue}"/>, which represents a character.
/// Leaf nodes carry the value of type <typeparamref name="TValue"/>.
/// </summary>
/// <typeparam name="TValue">The type of the value that is carried by the leaf nodes</typeparam>
public sealed class MapNode<TValue> : NodeBase<MapNode<TValue>>, INode<MapNode<TValue>, TValue>
{
    #region Fields

    private readonly Dictionary<char, MapNode<TValue>> mChildren = [];

    #endregion

    #region Properties

    /// <inheritdoc/>
    public override Dictionary<char, MapNode<TValue>> Children => mChildren;

    /// <inheritdoc/>
    internal override MapNode<TValue> Me => this;

    /// <summary>
    /// The value of type <typeparamref name="TValue"/> represented by this <see cref="MapNode{TValue}"/>.
    /// </summary>
    public TValue? Value { get; set; }

    #endregion

    #region Public methods and functions

    

    #endregion


}