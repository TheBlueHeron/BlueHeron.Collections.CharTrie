
namespace BlueHeron.Collections.Trie;

/// <summary>
/// A basic <see cref="INode{Node}"/>.
/// </summary>
public class Node : NodeBase<Node>
{
    #region Fields

    private readonly Dictionary<char, Node> mChildren = [];

    #endregion

    /// <inheritdoc/>
    public override Dictionary<char, Node> Children  => mChildren;

    /// <inheritdoc/>
    internal override Node Me => this;
}