
namespace BlueHeron.Collections.Trie;

/// <summary>
/// Interface definition for nodes in a <see cref="ITrie{TNode}"/>.
/// </summary>
/// <typeparam name="TNode">The type of the node</typeparam>
public interface INode<TNode> where TNode : INode<TNode>, new()
{
    /// <summary>
    /// Gets a boolean, signifying whether this <see cref="INode{TNode}"/> finishes a word.
    /// </summary>
    bool IsWord { get; internal set; }

    /// <summary>
    /// Gets the number of words of which this <see cref="INode{TNode}"/> is a part.
    /// </summary>
    int NumWords { get; }

    /// <summary>
    /// Gets the collection of <see cref="INode{TNode}"/>s that immediately follow this <see cref="INode{TNode}"/>.
    /// </summary>
    IDictionary<char, TNode> Children { get; }

    /// <summary>
    /// Removes all child <see cref="INode{TNode}"/>s from this <see cref="INode{TNode}"/>.
    /// </summary>
    void Clear();

    /// <summary>
    /// Returns the child <see cref="INode{TNode}"/> that represent the given <see cref="char"/> if it exists, else <see langword="null"/>.
    /// </summary>
    /// <param name="character">The <see cref="char"/> to match</param>
    /// <returns>The <see cref="INode{TNode}"/> representing the given <see cref="char"/> if it exists; else <see langword="null"/></returns>
    TNode? GetNode(char character);

    /// <summary>
    /// Returns the child <see cref="INode{TNode}"/> that represent the given prefix if it exists, else <see langword="null"/>.
    /// </summary>
    /// <param name="character">The <see cref="string"/> prefix to match</param>
    /// <returns>The <see cref="INode{TNode}"/> representing the given <see cref="string"/> if it exists; else <see langword="null"/></returns>
    TNode? GetNode(string prefix);

    /// <summary>
    /// Returns all child <see cref="INode{TNode}"/>s as an <see cref="IEnumerable{TNode}"/>.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{TNode}"/></returns>
    IEnumerable<TNode> GetNodes();
}