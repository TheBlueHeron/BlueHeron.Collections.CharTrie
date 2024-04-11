
namespace BlueHeron.Collections.Trie;

/// <summary>
/// Base class for nodes in a <see cref="ITrie{Node}"/>, that represent a character.
/// </summary>
public abstract class NodeBase<TNode> : INode<TNode> where TNode : INode<TNode>, new()
{
    #region Properties

    /// <summary>
    /// Gets the collection of <see cref="TNode"/>s that immediately follow this <see cref="INode{TNode}"/>.
    /// </summary>
    public abstract IDictionary<char, TNode> Children { get; }

    /// <summary>
    /// Determines whether this <see cref="TNode"/> finishes a word.
    /// </summary>
    /// <remarks>If a node is a word, it may still have children that form other words.</remarks>
    public bool IsWord { get; set; }

    /// <summary>
    /// Gets this <see cref="NodeBase{TNode}"/> inheritor, i.e. a <typeparamref name="TNode"/>.
    /// </summary>
    internal abstract TNode Me { get; }

    /// <summary>
    /// Returns the number of words of which this <see cref="TNode"/> is a part.
    /// </summary>
    public int NumWords => (IsWord ? 1 : 0) + (Children.Count == 0 ? 0 : Children.Sum(c => c.Value.NumWords));

    #endregion

    #region Public methods and functions

    /// <summary>
    /// Removes all child <see cref="TNode"/>s from this <see cref="TNode"/>.
    /// </summary>
    public void Clear()
    {
        Children.Clear();
    }

    /// <summary>
    /// Returns the child <see cref="TNode"/> that represent the given <see cref="char"/> if it exists, else <see langword="null"/>.
    /// </summary>
    /// <param name="character">The <see cref="char"/> to match</param>
    /// <returns>The <see cref="TNode"/> representing the given <see cref="char"/> if it exists; else <see langword="null"/></returns>
    public TNode? GetNode(char character)
    {
        TNode? node;
        Children.TryGetValue(character, out node);
        return node;
    }

    /// <summary>
    /// Returns the child <see cref="TNode"/> that represent the given prefix if it exists, else <see langword="null"/>.
    /// </summary>
    /// <param name="character">The <see cref="string"/> prefix to match</param>
    /// <returns>The <see cref="TNode"/> representing the given <see cref="string"/> if it exists; else <see langword="null"/></returns>
    public TNode? GetNode(string prefix)
    {
        var node = Me;
        foreach (var prefixChar in prefix)
        {
            node = node.GetNode(prefixChar);
            if (node == null)
            {
                break;
            }
        }
        return node;
    }

    /// <summary>
    /// Returns all child <see cref="INode"/>s as an <see cref="IEnumerable{INode}"/>.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{INode}"/></returns>
    public IEnumerable<TNode> GetNodes()
    {
        return Children.Values;
    }

    #endregion
}