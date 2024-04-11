
namespace BlueHeron.Collections.Trie;

/// <summary>
/// A basic <see cref="INode{Node}"/>.
/// </summary>
public class Node : NodeBase<Node>
{
    #region Fields

    private readonly RobinHoodDictionary<char, Node> mChildren = [];

    #endregion

    /// <inheritdoc/>
    public override IDictionary<char, Node> Children  => mChildren;

    /// <inheritdoc/>
    internal override Node Me => this;
}