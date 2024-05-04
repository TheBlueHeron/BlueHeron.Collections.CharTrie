using System.Text.Json.Serialization;
using BlueHeron.Collections.Trie.Serialization;
using Faster.Map.DenseMap;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// A node in the <see cref="Trie"/>, which represents a character.
/// </summary>
[JsonConverter(typeof(NodeConverter))]
public class Node
{
    #region Objects and variables

    private int mNumChildren;
    private int mNumWords;
    private int mRemainingDepth;
    private readonly DenseMap<char, Node> mChildren;
    private static readonly EqualityComparer<char> mComparer = EqualityComparer<char>.Default;

    #endregion

    #region Construction

    /// <summary>
    /// Creates a new <see cref="Node"/> with an empty <see cref="Nodes"/> collection, <see cref="NumWords"/>, <see cref="NumChildren"/> and <see cref="RemainingDepth"/> set to 0 if <paramref name="isDeserialized"/> is <see langword="true"/>, else to -1 (i.e. unset, which causes calculation of these values upon first access).
    /// </summary>
    internal Node(bool isDeserialized)
    {
        var num = isDeserialized ? 0 : -1;

        mChildren = new DenseMap<char, Node>(8, 0.5, mComparer);
        mNumChildren = num;
        mRemainingDepth = num;
        mNumWords = num;
    }

    /// <summary>
    /// Creates a new <see cref="Node"/> with an empty <see cref="Nodes"/> collection and <see cref="NumWords"/> set to -1 (i.e. unset).
    /// </summary>
    public Node() : this(false) { }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the collection of <see cref="Node"/>s that immediately follow this <see cref="Node"/>.
    /// </summary>
    public IReadOnlyDictionary<char, Node> Nodes => mChildren.Entries.ToDictionary();

    /// <summary>
    /// Gets the internally used <see cref="RobinHoodDictionary{char, Node}"/>.
    /// </summary>
    internal DenseMap<char, Node> Children => mChildren;

    /// <summary>
    /// Gets a boolean, determining whether this <see cref="Node"/> finishes a word.
    /// </summary>
    /// <remarks>If a node is a word, it may still have children that form other words.</remarks>
    public bool IsWord { get; internal set; }

    /// <summary>
    /// Gets the number of child nodes of this <see cref="Node"/>.
    /// </summary>
    public int NumChildren
    {
        get
        {
            if (mNumChildren < 0)
            {
                mNumChildren = mChildren.Count;
            }
            return mNumChildren;
        }
        internal set => mNumChildren = value;
    }

    /// <summary>
    /// Gets the number of words of which this <see cref="Node"/> is a part.
    /// </summary>
    public int NumWords
    {
        get
        {
            if (mNumWords < 0)
            {
                mNumWords = (IsWord ? 1 : 0) + (mChildren.Count == 0 ? 0 : Nodes.Sum(c => c.Value.NumWords));
            }
            return mNumWords;
        }
        internal set => mNumWords = value;
    }

    /// <summary>
    /// Gets the maximum depth of the remaining hierarchy of nodes.
    /// </summary>
    public int RemainingDepth
    {
        get
        {
            if (mRemainingDepth < 0)
            {
                mRemainingDepth = mChildren.Count == 0 ? 0 : 1 + Nodes.Max(c => c.Value.RemainingDepth);
            }
            return mRemainingDepth;
        }
        internal set => mRemainingDepth = value;
    }

    /// <summary>
    /// Gets the index of the <see cref="Type"/> of the <see cref="Value"/> in the <see cref="Trie.RegisteredTypes"/> list.
    /// </summary>
    public int TypeIndex { get; internal set; } = -1;

    /// <summary>
    /// Gets the value that is represented by this node.
    /// </summary>
    public object? Value { get; internal set; }

    #endregion

    #region Public methods and functions

    /// <summary>
    /// Returns the child <see cref="Node"/> that represent the given <see cref="char"/> if it exists, else <see langword="null"/>.
    /// </summary>
    /// <param name="character">The <see cref="char"/> to match</param>
    /// <returns>The <see cref="Node"/> representing the given <see cref="char"/> if it exists; else <see langword="null"/></returns>
    public Node GetNode(char character)
    {
        Node? node;
        if (mChildren.Get(character, out node))
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

    #endregion
}