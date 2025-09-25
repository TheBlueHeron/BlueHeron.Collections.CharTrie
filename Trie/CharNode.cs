using System.Runtime.InteropServices;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// A node in the <see cref="CharTrie"/>.
/// </summary>
/// <param name="character">The unicode character to be represented by the node</param>
[StructLayout(LayoutKind.Sequential)]
public struct CharNode(char character) : IComparable<CharNode>, IEquatable<CharNode>
{
    #region Properties

    /// <summary>
    /// Gets the unicode character represented by this <see cref="CharNode"/>.
    /// </summary>
    public readonly char Character = character;

    /// <summary>
    /// Gets the maximum depth of this <see cref="CharNode"/>'s tree of children.
    /// </summary>
    public byte RemainingDepth { readonly get; internal set; }

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