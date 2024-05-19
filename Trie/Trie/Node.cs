using System.Text.Json.Serialization;
using BlueHeron.Collections.Trie.Serialization;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// A node in the <see cref="Trie"/>, which represents a character.
/// </summary>
[JsonConverter(typeof(NodeSerializer))]
public class Node
{
    #region Objects and variables

    private int mRemainingDepth;
    private readonly HashSet<(char, Node)> mChildren;

    #endregion

    #region Construction

    /// <summary>
    /// Creates a new <see cref="Node"/> with an empty <see cref="Nodes"/> collection, <see cref="NumWords"/>, <see cref="NumChildren"/> and <see cref="RemainingDepth"/> set to 0 if <paramref name="isDeserialized"/> is <see langword="true"/>, else to -1 (i.e. unset, which causes calculation of these values upon first access).
    /// </summary>
    internal Node(bool isDeserialized)
    {
        mChildren = [];
        mRemainingDepth = isDeserialized ? 0 : -1;
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
    public IReadOnlyDictionary<char, Node> Nodes => mChildren.ToDictionary();

    /// <summary>
    /// Gets the internally used <see cref="HashSet{Tuple{char, Node}}"/>.
    /// </summary>
    internal HashSet<(char, Node)> Children => mChildren;

    /// <summary>
    /// Gets a boolean, determining whether this <see cref="Node"/> finishes a word.
    /// </summary>
    /// <remarks>If a node is a word, it may still have children that form other words.</remarks>
    public bool IsWord { get; internal set; }

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
    public Node? GetNode(char character)
    {
        return mChildren.FirstOrDefault(kv => kv.Item1 == character).Item2;
    }

    /// <summary>
    /// Returns the child <see cref="Node"/> that represent the given prefix if it exists, else <see langword="null"/>.
    /// </summary>
    /// <param name="character">The <see cref="string"/> prefix to match</param>
    /// <returns>The <see cref="Node"/> representing the given <see cref="string"/> if it exists; else <see langword="null"/></returns>
    public Node? GetNode(string prefix)
    {
        var node = this;
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
    /// Returns the number of words represented by this <see cref="Node"/> and its children.
    /// </summary>
    public int NumWords() => (IsWord ? 1 : 0) + mChildren.Sum(kv => kv.Item2.NumWords());

    #endregion

    #region Private methods and functions

    /// <summary>
    /// Sets all numeric fields to -1, forcing recalculation.
    /// </summary>
    internal void Unset()
    {
        mRemainingDepth = -1;
    }

    #endregion
}

/// <summary>
/// A <see cref="Node"/> that has a field that represents the expected number of child <see cref="Node"/>s.
/// </summary>
[JsonConverter(typeof(NodeDeserializer))]
public sealed class DeserializedNode: Node
{
    private int? mNumChildren;

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

    #region Construction

    /// <summary>
    /// Creates a new <see cref="DeserializedNode"/>.
    /// </summary>
    public DeserializedNode() : base(true) { }

    #endregion
}