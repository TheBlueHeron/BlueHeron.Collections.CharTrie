using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// A node in the <see cref="CharTrie"/>.
/// </summary>
/// <param name="character">The unicode character to be represented by the node</param>
[StructLayout(LayoutKind.Sequential)]
public readonly struct CharNode(char character) : IComparable<CharNode>, IEquatable<CharNode>
{
    #region Properties

    public readonly char Character = character;

    #endregion

    #region Public methods and functions

    /// <summary>
    /// Returns the <see cref="Character"/>.
    /// </summary>
    public readonly override string ToString() => $"{Character}";

    #endregion 

    #region IComparable

    /// <summary>
    /// Executes <see cref="char.CompareTo(char)"/> using both <see cref="CharNode"/>'s <see cref="Character"/> values.
    /// </summary>
    /// <param name="other">The <see cref="CharNode"/> to compare with</param>
    public readonly int CompareTo(CharNode other) => Character.CompareTo(other.Character);

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj) => obj is CharNode node && Equals(node);

    /// <inheritdoc/>
    public readonly override int GetHashCode() => Character.GetHashCode();

    /// <summary>
    /// Returns a value that indicates whether this instance is equal to a specified object.
    /// </summary>
    /// <param name="other">An object to compare with this instance or null</param>
    /// <returns><see cref="bool"/>, <see langword="true"/> if the specified object is equal to this instance</returns>
    public readonly bool Equals(CharNode other) => Character.Equals(other.Character);

    /// <summary>
    /// Returns a value that indicates whether the <see cref="CharNode"/>s are equal.
    /// </summary>
    /// <param name="left">The <see cref="CharNode"/> on the left of the operator</param>
    /// <param name="right">The <see cref="CharNode"/> on the right of the operator</param>
    /// <returns><see cref="bool"/>, <see langword="true"/> if the <see cref="CharNode"/>s are equal</returns>
    public static bool operator ==(CharNode left, CharNode right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Returns a value that indicates whether the <see cref="CharNode"/>s are not equal.
    /// </summary>
    /// <param name="left">The <see cref="CharNode"/> on the left of the operator</param>
    /// <param name="right">The <see cref="CharNode"/> on the right of the operator</param>
    /// <returns><see cref="bool"/>, <see langword="true"/> if the <see cref="CharNode"/>s are not equal</returns>
    public static bool operator !=(CharNode left, CharNode right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Returns a value that indicates whether the character of the left <see cref="CharNode"/> is smaller than the character of the right <see cref="CharNode"/>.
    /// </summary>
    /// <param name="left">The <see cref="CharNode"/> on the left of the operator</param>
    /// <param name="right">The <see cref="CharNode"/> on the right of the operator</param>
    /// <returns><see cref="bool"/>, <see langword="true"/> if the character of the <see cref="CharNode"/> is smaller than the character of the right <see cref="CharNode"/></returns>
    public static bool operator <(CharNode left, CharNode right)
    {
        return left.CompareTo(right) < 0;
    }

    /// <summary>
    /// Returns a value that indicates whether the character of the left <see cref="CharNode"/> is smaller than or equal to the character of the right <see cref="CharNode"/>.
    /// </summary>
    /// <param name="left">The <see cref="CharNode"/> on the left of the operator</param>
    /// <param name="right">The <see cref="CharNode"/> on the right of the operator</param>
    /// <returns><see cref="bool"/>, <see langword="true"/> if the character of the <see cref="CharNode"/> is smaller than or equal to the character of the right <see cref="CharNode"/></returns>
    public static bool operator <=(CharNode left, CharNode right)
    {
        return left.CompareTo(right) <= 0;
    }

    /// <summary>
    /// Returns a value that indicates whether the character of the left <see cref="CharNode"/> is bigger than the character of the right <see cref="CharNode"/>.
    /// </summary>
    /// <param name="left">The <see cref="CharNode"/> on the left of the operator</param>
    /// <param name="right">The <see cref="CharNode"/> on the right of the operator</param>
    /// <returns><see cref="bool"/>, <see langword="true"/> if the character of the <see cref="CharNode"/> is bigger than the character of the right <see cref="CharNode"/></returns>
    public static bool operator >(CharNode left, CharNode right)
    {
        return left.CompareTo(right) > 0;
    }

    /// <summary>
    /// Returns a value that indicates whether the character of the left <see cref="CharNode"/> is bigger than or equal to the character of the right <see cref="CharNode"/>.
    /// </summary>
    /// <param name="left">The <see cref="CharNode"/> on the left of the operator</param>
    /// <param name="right">The <see cref="CharNode"/> on the right of the operator</param>
    /// <returns><see cref="bool"/>, <see langword="true"/> if the character of the <see cref="CharNode"/> is bigger than or equal to the character of the right <see cref="CharNode"/></returns>
    public static bool operator >=(CharNode left, CharNode right)
    {
        return left.CompareTo(right) >= 0;
    }

    #endregion
}

/// <summary>
/// A Trie (or prefix tree) for storing strings made up of unicode characters.
/// </summary>
public class CharTrie
{
    #region Fields

    private CharNode[] mNodes;
    private int[][] mChildren;
    private bool[] mIsWordEnd;
    private int mNodeCount;

    #endregion

    #region Construction

    public CharTrie()
    {
        mNodes = new CharNode[1]; // start with 1 node (root)
        mChildren = new int[1][]; // children of each node
        mIsWordEnd = new bool[1]; // word end markers

        mNodeCount = 1; // root is at index 0
        mNodes[0] = new CharNode('\0');
        mChildren[0] = [];
    }

    #endregion

    #region Properties



    #endregion

    #region Public methods and functions

    public void Add(string word)
    {
        ArgumentException.ThrowIfNullOrEmpty(word);

        var current = 0; // index of root node

        foreach (var c in word)
        {
            var next = -1;

            for (var i = 0; i < mChildren[current].Length; i++) // search for existing child
            {
                var childIndex = mChildren[current][i];
                if (mNodes[childIndex].Character == c)
                {
                    next = childIndex;
                    break;
                }
            }

            if (next == -1) // if not found, create new node
            {
                if (mNodeCount >= mNodes.Length) // resize arrays if needed
                {
                    Array.Resize(ref mNodes, mNodes.Length + 1);
                    Array.Resize(ref mChildren, mChildren.Length + 1);
                    Array.Resize(ref mIsWordEnd, mIsWordEnd.Length + 1);
                }

                next = mNodeCount++;
                mNodes[next] = new CharNode(c);
                mChildren[next] = [];

                var updated = new int[mChildren[current].Length + 1]; // add new child to current node
                Array.Copy(mChildren[current], updated, mChildren[current].Length);
                updated[^1] = next; // add index of new child at the end
                mChildren[current] = updated;
            }
            current = next;
        }
        mIsWordEnd[current] = true;
    }

    public IEnumerable<string> All()
    {
        foreach (var word in Walk(0, string.Empty))
        {
            yield return word;
        }
    }

    public bool StartsWith(string prefix)
    {
        ArgumentException.ThrowIfNullOrEmpty(prefix);

        var current = 0;
        foreach (var c in prefix)
        {
            var found = false;
            for (var i = 0; i < mChildren[current].Length; i++)
            {
                var childIndex = mChildren[current][i];
                if (mNodes[childIndex].Character == c)
                {
                    current = childIndex;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                return false;
            }
        }
        return true;
    }

    #endregion

    #region Private methods and functions

    private IEnumerable<string> Walk(int index, string prefix)
    {
        if (index != 0)
        {
            prefix += mNodes[index].Character;
        }

        if (mIsWordEnd[index])
        {
            yield return prefix;
        }

        for (var i = 0; i < mChildren[index].Length; i++)
        {
            foreach (var word in Walk(mChildren[index][i], prefix))
            {
                yield return word;
            }
        }
    }

    #endregion
}