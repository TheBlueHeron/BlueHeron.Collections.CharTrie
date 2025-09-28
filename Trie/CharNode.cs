using System.Runtime.InteropServices;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// Represents a character node in the <see cref="CharTrie"/>.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct CharNode : IEquatable<CharNode>
{
    [FieldOffset(0)]
    public int FirstChildIndex; // 4
    [FieldOffset(4)]
    public byte CharIndex; // 1
    [FieldOffset(5)]
    public byte ChildCount; // 1
    [FieldOffset(6)]
    public bool IsWordEnd; // 1

    #region Operators and overrides

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj) => object.Equals(CharIndex, obj);

    /// <inheritdoc/>
    public readonly override int GetHashCode() => CharIndex.GetHashCode();

    /// <summary>
    /// Compares by comparing <see cref="CharIndex"/> values.
    /// </summary>
    public static bool operator ==(CharNode left, CharNode right) => left.Equals(right);

    /// <summary>
    /// Compares by comparing <see cref="CharIndex"/> values.
    /// </summary>
    public static bool operator !=(CharNode left, CharNode right) => !(left == right);

    /// <summary>
    /// Compares by comparing <see cref="CharIndex"/> values.
    /// </summary>
    /// <param name="other">The <see cref="CharNode"/> to compare</param>
    public readonly bool Equals(CharNode other) => CharIndex == other.CharIndex;

    #endregion
}