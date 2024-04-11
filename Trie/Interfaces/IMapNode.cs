
namespace BlueHeron.Collections.Trie;

/// <summary>
/// Interface definition for nodes in a <see cref="ITrie"/> that represent a value.
/// </summary>
/// <typeparam name="TNode">The type of the node</typeparam>
/// <typeparam name="TValue">The type of the value, represented by this node</typeparam>
public interface INode<TNode, TValue> : INode<TNode> where TNode: INode<TNode, TValue>, new()
{
    /// <summary>
    /// The value of type <typeparamref name="TValue"/> represented by this <typeparamref name="TNode"/>.
    /// </summary>
    TValue? Value { get; set; }
}