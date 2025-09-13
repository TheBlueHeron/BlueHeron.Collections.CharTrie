using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using BlueHeron.Collections.Trie.Serialization;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// A node in the <see cref="Trie"/>, which represents a character.
/// </summary>
[JsonConverter(typeof(NodeSerializer))]
[StructLayout(LayoutKind.Sequential)]
public struct TrieNode() : IComparable<TrieNode>, IEquatable<TrieNode>
{
    #region Variables

    private const byte IsWordMask = 1 << 0;     // 0000_0001
    private const byte IsVisitedMask = 1 << 1;  // 0000_0010

    private byte mFlags;
    private short mRemainingDepth = -1;

    #endregion

    #region Properties

    /// <summary>
    /// The character represented by this <see cref="TrieNode"/>. Is <see cref="char.MaxValue"/> for the root node of a <see cref="Trie"/>.
    /// </summary>
    public char Character;

    /// <summary>
    /// Gets or sets the <see cref="TrieNode"/>'s array of child <see cref="TrieNode"/>s.
    /// </summary>
    public TrieNode[] Children = [];

    /// <summary>
    /// Determines whether this node has been visited.
    /// </summary>
    internal bool IsVisited
    {
        readonly get => (mFlags & IsVisitedMask) != 0;
        set
        {
            if (value)
            {
                mFlags |= IsVisitedMask;
            }
            else
            {
                mFlags &= unchecked((byte)~IsVisitedMask);
            }
        }
    }

    /// <summary>
    /// Determines whether this <see cref="TrieNode"/> finishes a word.
    /// </summary>
    public bool IsWord
    {

        readonly get => (mFlags & IsWordMask) != 0;
        internal set
        {
            if (value)
            {
                mFlags |= IsWordMask;
            }
            else
            {
                mFlags &= unchecked((byte)~IsWordMask);
            }
        }

    }

    /// <summary>
    /// Gets the maximum depth of this <see cref="TrieNode"/>'s tree of children.
    /// </summary>
    public short RemainingDepth
    {
        get
        {
            if (mRemainingDepth < 0)
            {
                mRemainingDepth = (short)(Children.Length == 0 ? 0 : 1 + Children.Max(n => n.RemainingDepth));
            }
            return mRemainingDepth;
        }
        internal set => mRemainingDepth = value;
    }

    #endregion

    #region Public methods and functions

    /// <summary>
    /// Returns the child <see cref="TrieNode"/> representing the given prefix, or <see langword="null"/>.
    /// </summary>
    /// <param name="prefix">The <see cref="string"/> prefix to match</param>
    /// <returns>A <see cref="TrieNode"/> if a match was made; else <see langword="null"/></returns>
    public readonly TrieNode? GetNode(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            throw new ArgumentNullException(nameof(prefix));
        }
        var node = this;

        foreach (var prefixChar in prefix)
        {
            var childNode = node.GetNode(prefixChar);
            if (childNode == null)
            {
                return null;
            }
            node = childNode.Value;
        }
        return node;
    }

    /// <summary>
    /// Returns the total number of<see cref="TrieNode"/>s under this <see cref="TrieNode"/>, including this <see cref="TrieNode"/> itself.
    /// </summary>
    public readonly int NumNodes() => 1 + Children.Sum(n => n.NumNodes());

    /// <summary>
    /// Returns the number of words represented by this <see cref="TrieNode"/> and its children.
    /// </summary>
    public readonly int NumWords() => (IsWord ? 1 : 0) + Children.Sum(n => n.NumWords());

    /// <summary>
    /// Overridden to return the <see cref="Character"/> value, whether it has been visited and whether it finishes a word.
    /// </summary>
    public readonly override string ToString() => $"{Character} (IsVisited: {IsVisited} | IsWord: {IsWord})";

    #endregion

    #region Private methods and functions

    /// <summary>
    /// Returns a new <see cref="TrieNode"/>.
    /// </summary>
    /// <param name="character">The character</param>
    /// <param name="isWord">Determines whether this <see cref="TrieNode"/> finishes a word</param>
    internal static TrieNode Create(char character, bool isWord)
    {
        return new TrieNode { Character = character, IsWord = isWord };
    }

    /// <summary>
    /// Returns the direct child <see cref="TrieNode"/> that represents the given <see cref="char"/> if it exists, else <see langword="null"/>.
    /// </summary>
    /// <param name="character">The <see cref="char"/> to find</param>
    /// <returns>A <see cref="TrieNode"/> if it exists; else <see langword="null"/></returns>
    private TrieNode? GetNode(char character)
    {
        var idx = Trie.Search(ref Children, 0, Children.Length - 1, character);
        return idx < 0 ? null : Children[idx];
    }

    #endregion

    #region IComparable

    /// <summary>
    /// Executes <see cref="char.CompareTo(char)"/> using both <see cref="TrieNode"/>'s <see cref="Character"/> values.
    /// </summary>
    /// <param name="other">The <see cref="TrieNode"/> to compare with</param>
    public readonly int CompareTo(TrieNode other) => Character.CompareTo(other.Character);

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj) => obj is TrieNode node && Equals(node);

    /// <inheritdoc/>
    public readonly override int GetHashCode() => Character.GetHashCode();

    /// <summary>
    /// Returns a value that indicates whether this instance is equal to a specified object.
    /// </summary>
    /// <param name="other">An object to compare with this instance or null</param>
    /// <returns><see cref="bool"/>, <see langword="true"/> if the specified object is equal to this instance</returns>
    public readonly bool Equals(TrieNode other) => Character.Equals(other.Character);

    /// <summary>
    /// Returns a value that indicates whether the <see cref="TrieNode"/>s are equal.
    /// </summary>
    /// <param name="left">The <see cref="TrieNode"/> on the left of the operator</param>
    /// <param name="right">The <see cref="TrieNode"/> on the right of the operator</param>
    /// <returns><see cref="bool"/>, <see langword="true"/> if the <see cref="TrieNode"/>s are equal</returns>
    public static bool operator ==(TrieNode left, TrieNode right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Returns a value that indicates whether the <see cref="TrieNode"/>s are not equal.
    /// </summary>
    /// <param name="left">The <see cref="TrieNode"/> on the left of the operator</param>
    /// <param name="right">The <see cref="TrieNode"/> on the right of the operator</param>
    /// <returns><see cref="bool"/>, <see langword="true"/> if the <see cref="TrieNode"/>s are not equal</returns>
    public static bool operator !=(TrieNode left, TrieNode right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Returns a value that indicates whether the character of the left <see cref="TrieNode"/> is smaller than the character of the right <see cref="TrieNode"/>.
    /// </summary>
    /// <param name="left">The <see cref="TrieNode"/> on the left of the operator</param>
    /// <param name="right">The <see cref="TrieNode"/> on the right of the operator</param>
    /// <returns><see cref="bool"/>, <see langword="true"/> if the character of the <see cref="TrieNode"/> is smaller than the character of the right <see cref="TrieNode"/></returns>
    public static bool operator <(TrieNode left, TrieNode right)
    {
        return left.CompareTo(right) < 0;
    }

    /// <summary>
    /// Returns a value that indicates whether the character of the left <see cref="TrieNode"/> is smaller than or equal to the character of the right <see cref="TrieNode"/>.
    /// </summary>
    /// <param name="left">The <see cref="TrieNode"/> on the left of the operator</param>
    /// <param name="right">The <see cref="TrieNode"/> on the right of the operator</param>
    /// <returns><see cref="bool"/>, <see langword="true"/> if the character of the <see cref="TrieNode"/> is smaller than or equal to the character of the right <see cref="TrieNode"/></returns>
    public static bool operator <=(TrieNode left, TrieNode right)
    {
        return left.CompareTo(right) <= 0;
    }

    /// <summary>
    /// Returns a value that indicates whether the character of the left <see cref="TrieNode"/> is bigger than the character of the right <see cref="TrieNode"/>.
    /// </summary>
    /// <param name="left">The <see cref="TrieNode"/> on the left of the operator</param>
    /// <param name="right">The <see cref="TrieNode"/> on the right of the operator</param>
    /// <returns><see cref="bool"/>, <see langword="true"/> if the character of the <see cref="TrieNode"/> is bigger than the character of the right <see cref="TrieNode"/></returns>
    public static bool operator >(TrieNode left, TrieNode right)
    {
        return left.CompareTo(right) > 0;
    }

    /// <summary>
    /// Returns a value that indicates whether the character of the left <see cref="TrieNode"/> is bigger than or equal to the character of the right <see cref="TrieNode"/>.
    /// </summary>
    /// <param name="left">The <see cref="TrieNode"/> on the left of the operator</param>
    /// <param name="right">The <see cref="TrieNode"/> on the right of the operator</param>
    /// <returns><see cref="bool"/>, <see langword="true"/> if the character of the <see cref="TrieNode"/> is bigger than or equal to the character of the right <see cref="TrieNode"/></returns>
    public static bool operator >=(TrieNode left, TrieNode right)
    {
        return left.CompareTo(right) >= 0;
    }

    #endregion
}