using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using BlueHeron.Collections.Trie.Serialization;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// A node in the <see cref="Trie"/>, which represents a character.
/// </summary>
[JsonConverter(typeof(NodeSerializer))]
[StructLayout(LayoutKind.Auto)]
public struct TrieNode : IComparable<TrieNode>, IEquatable<TrieNode>
{
    #region Fields

    private int mRemainingDepth;

    /// <summary>
    /// The character. Is only <see langword="null"/> on the root node.
    /// </summary>
    public char Character;

    /// <summary>
    /// The <see cref="TrieNode"/>'s collection of child <see cref="TrieNode"/>s.
    /// </summary>
    public TrieNode[] Children;

    /// <summary>
    /// Determines whether this <see cref="TrieNode"/> finishes a word.
    /// </summary>
    public bool IsWord;

    /// <summary>
    /// The maximum depth of this <see cref="TrieNode"/>'s tree of children.
    /// </summary>
    public int RemainingDepth
    {
        get
        {
            if (mRemainingDepth < 0)
            {
                mRemainingDepth = Children.Length == 0 ? 0 : 1 + Children.Max(n => n.RemainingDepth);
            }
            return mRemainingDepth;
        }
        internal set => mRemainingDepth = value;
    }

    /// <summary>
    /// The value that is represented by this <see cref="TrieNode"/>.
    /// </summary>
    public string? Value;

    #endregion

    #region Construction

    /// <summary>
    /// Creates a new <see cref="TrieNode"/> without a <see cref="Character"/> and an empty <see cref="Children"/> array.
    /// </summary>
    public TrieNode()
    {
        Children = []; RemainingDepth = -1;
    }

    /// <summary>
    /// Creates a new <see cref="TrieNode"/>.
    /// </summary>
    /// <param name="character">The character</param>
    /// <param name="isWord">Determines whether this <see cref="TrieNode"/> finishes a word</param>
    /// <param name="value">The value, represented by this <see cref="TrieNode"/></param>
    internal TrieNode(char character, bool isWord, string? value = null) : this()
    {
        Character = character;
        IsWord = isWord;
        Value = value;
    }

    #endregion

    #region Public methods and functions

    /// <summary>
    /// Returns a <see cref="NodeReference"/> to the child <see cref="TrieNode"/> that represent the given <see cref="char"/> if it exists, else <see langword="null"/>.
    /// </summary>
    /// <param name="character">The <see cref="char"/> to find</param>
    /// <returns>A <see cref="NodeReference"/></returns>
    public NodeReference GetNode(char character)
    {
        var idx = Trie.Search(ref Children, 0, Children.Length - 1, character);
        return idx < 0 ? new NodeReference() : new NodeReference(ref Children[idx]);
    }

    /// <summary>
    /// Sets the given <see cref="NodeReference"/> to wrap the <see cref="TrieNode"/> representing the given prefix, or <see langword="null"/>.
    /// Returns a value signifying whether the given prefix could be located.
    /// </summary>
    /// <param name="prefix">The <see cref="string"/> prefix to match</param>
    /// <param name="nodeRef">Will hold a <see cref="NodeReference"/> to the <see cref="TrieNode"/> representing the given prefix, or <see langword="null"/></param>
    /// <returns>A value signifying whether the prefix could be located</returns>
    public static bool GetNode(string prefix, ref NodeReference nodeRef)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            throw new ArgumentNullException(nameof(prefix));
        }
        //var nodeRef = new NodeReference(ref this);

        foreach (var prefixChar in prefix)
        {
            nodeRef = nodeRef.Node.GetNode(prefixChar);
            if (!nodeRef.HasNode)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Returns the total number of<see cref="TrieNode"/>s under this <see cref="TrieNode"/>, including this <see cref="TrieNode"/> itself.
    /// </summary>
    public readonly int NumNodes() => 1 + Children.Sum(n => n.NumNodes());

    /// <summary>
    /// Returns the number of words represented by this <see cref="TrieNode"/> and its children.
    /// </summary>
    public readonly int NumWords() => (IsWord ? 1 : 0) + Children.Sum(n => n.NumWords());

    #endregion

    #region IComparable

    /// <summary>
    /// Executes <see cref="char.CompareTo(char)"/>.
    /// </summary>
    /// <param name="other">The <see cref="TrieNode"/> to compare</param>
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

    public static bool operator ==(TrieNode left, TrieNode right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TrieNode left, TrieNode right)
    {
        return !(left == right);
    }

    public static bool operator <(TrieNode left, TrieNode right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(TrieNode left, TrieNode right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(TrieNode left, TrieNode right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(TrieNode left, TrieNode right)
    {
        return left.CompareTo(right) >= 0;
    }

    #endregion
}