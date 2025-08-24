using System.Diagnostics;
using System.Runtime.CompilerServices;
using BlueHeron.Collections.Trie.Search;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// Object that constitutes a nullable reference to a <see cref="TrieNode"/> with a <see cref="Visited"/> property that is used in <see cref="PatternMatchType.IsFragment"/> type searches.
/// </summary>
[DebuggerDisplay("{Node} (v: {Visited})")]
public ref struct NodeReference
{
    #region Objects and variables

    private readonly ref TrieNode mNode;

    #endregion

    #region Properties

    /// <summary>
    /// Gets a value, determining whether this <see cref="NodeReference"/> wraps a <see cref="TrieNode"/> or is <see langword="null"/>.
    /// </summary>
    public readonly bool HasNode => !Unsafe.IsNullRef(ref mNode);

    /// <summary>
    /// Determines whether this node was matched to a wildcard and shouldn't be marked as visited.
    /// </summary>
    internal bool IsWildCard { get; set; }

    /// <summary>
    /// Gets a reference to the wrapped <see cref="TrieNode"/>.
    /// If <see cref="HasNode"/> is <see langword="false"/>, an <see cref="InvalidOperationException"/> is thrown.
    /// </summary>
    /// <exception cref="InvalidOperationException">No <see cref="TrieNode"/> is present. Call <see cref="HasNode"/> first.</exception>
    public readonly ref TrieNode Node
    {
        get
        {
            if (!HasNode) { throw new InvalidOperationException(); }
            return ref mNode;
        }
    }

    /// <summary>
    /// Determines whether this node already served as the start node of a walk.
    /// This prevents identical retries from occurring multiple times.
    /// </summary>
    internal bool Visited { get; set; }

    #endregion

    #region Construction

    /// <summary>
    /// Creates a new <see cref="NodeReference"/>, representing a reference to the given <see cref="TrieNode"/>.
    /// </summary>
    /// <param name="node">A reference to a <see cref="TrieNode"/></param>
    /// <param name="wildCard">Determines whether this node was matched to a wildcard and shouldn't be marked as visited</param>
    [DebuggerStepThrough()]
    internal NodeReference(ref TrieNode node, bool wildCard = false)
    {
        mNode = ref node;
        IsWildCard = wildCard;
    }

    #endregion

    #region Public methods and functions

    /// <summary>
    /// Overridden to return the <see cref="TrieNode.ToString()"/> value and whether it has been visited already.
    /// </summary>
    public readonly override string ToString() => $"{Node} (v: {Visited})";

    #endregion
}