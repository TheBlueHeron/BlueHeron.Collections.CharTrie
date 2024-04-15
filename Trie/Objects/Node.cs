using System.Text.Json.Serialization;
using BlueHeron.Collections.Trie.Serialization;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// A basic node, implemented by subclassing <see cref="NodeBase{Node}"/>.
/// </summary>
[JsonConverter(typeof(NodeConverter))]
public class Node
{
    #region Objects and variables

    private int mNumWords = -1; // -1: unset

    private readonly RobinHoodDictionary<char, Node> mChildren = [];

    #endregion

    #region Properties

    /// <summary>
    /// Gets the collection of <see cref="Node"/>s that immediately follow this <see cref="Node"/>.
    /// </summary>
    public IDictionary<char, Node> Children => mChildren;

    /// <summary>
    /// Determines whether this <see cref="Node"/> finishes a word.
    /// </summary>
    /// <remarks>If a node is a word, it may still have children that form other words.</remarks>
    public bool IsWord { get; set; }

    /// <summary>
    /// Returns the number of words of which this <see cref="Node"/> is a part.
    /// </summary>
    public int NumWords
    {
        get
        {
            if (mNumWords < 0)
            {
                mNumWords = (IsWord ? 1 : 0) + (Children.Count == 0 ? 0 : Children.Sum(c => c.Value.NumWords));
            }
            return mNumWords;
        }
        set => mNumWords = value;
    }

    /// <summary>
    /// The index of the <see cref="Type"/> of the <see cref="Value"/> in the <see cref="Trie.RegisteredTypes"/> list.
    /// </summary>
    public int TypeIndex { get; set; } = -1;

    /// <summary>
    /// The value that is represented by this node.
    /// </summary>
    public object? Value { get; set; }

    #endregion

    #region Public methods and functions

    /// <summary>
    /// Removes all child <see cref="Node"/>s from this <see cref="Node"/>.
    /// </summary>
    public void Clear()
    {
        Children.Clear();
    }

    /// <summary>
    /// Returns the child <see cref="Node"/> that represent the given <see cref="char"/> if it exists, else <see langword="null"/>.
    /// </summary>
    /// <param name="character">The <see cref="char"/> to match</param>
    /// <returns>The <see cref="Node"/> representing the given <see cref="char"/> if it exists; else <see langword="null"/></returns>
    public Node GetNode(char character)
    {
        Node? node;
        if (Children.TryGetValue(character, out node))
        {
            return node;
        }
        throw new ArgumentOutOfRangeException(nameof(character));
    }

    /// <summary>
    /// Returns the child <see cref="Node"/> that represent the given prefix if it exists, else <see langword="null"/>.
    /// </summary>
    /// <param name="character">The <see cref="string"/> prefix to match</param>
    /// <returns>The <see cref="Node"/> representing the given <see cref="string"/> if it exists; else <see langword="null"/></returns>
    public Node GetNode(string prefix)
    {
        var node = this;
        foreach (var prefixChar in prefix)
        {
            node = node.GetNode(prefixChar);
            if (node == null)
            {
                throw new ArgumentOutOfRangeException(nameof(prefix));
            }
        }
        return node;
    }

    /// <summary>
    /// Returns all child <see cref="Node"/>s as an <see cref="IEnumerable{Node}"/>.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{Node}"/></returns>
    public IEnumerable<Node> GetNodes()
    {
        return Children.Values;
    }

    #endregion
}