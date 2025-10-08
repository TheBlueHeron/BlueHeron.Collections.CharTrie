using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using BlueHeron.Collections.Trie.Serialization;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// Represents a character node in the <see cref="CharTrie"/>.
/// FirstChildIndex (int) + Meta (uint) -> 8 bytes total on most runtimes.
/// Meta layout: bits 0-7: CharIndex; bits 8-15: ChildCount; bit 16: IsWordEnd; bit 17-31: RemainingDepth
/// </summary>
[JsonConverter(typeof(CharNodeConverter))]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct CharNode : IEquatable<CharNode>
{
    private uint _meta;

    /// <summary>
    /// The index of the first child <see cref="CharNode"/> in the child indices list.
    /// </summary>
    public int FirstChildIndex;

    /// <summary>
    /// The index of the character in the characters set, represented by this <see cref="CharNode"/>.
    /// </summary>
    public byte CharIndex
    {
        readonly get => (byte)(_meta & 0xFFu);
        set => _meta = (_meta & ~0xFFu) | value;
    }

    /// <summary>
    /// The number of child <see cref="CharNode"/>s under this <see cref="CharNode"/>.
    /// </summary>
    public byte ChildCount
    {
        readonly get => (byte)((_meta >> 8) & 0xFFu);
        set => _meta = (_meta & ~0xFF00u) | ((uint)value << 8);
    }

    /// <summary>
    /// If <see langword="true"/>, this <see cref="CharNode"/> terminates a word; else <see langword="false"/>.
    /// </summary>
    public bool IsWordEnd
    {
        readonly get => ((_meta >> 16) & 0x1u) == 1u;
        set
        {
            if (value)
            {
                _meta |= (1u << 16);
            }
            else
            {
                _meta &= ~(1u << 16);
            }
        }
    }

    /// <summary>
    /// The maximum depth of the subtree under this <see cref="CharNode"/>.
    /// </summary>
    public ushort RemainingDepth
    {
        readonly get => (ushort)((_meta >> 17) & 0x7FFFu);
        set => _meta = (_meta & ~(0xFFFFu << 17)) | ((uint)(value & 0x7FFF) << 17);
    }

    #region Operators and overrides

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj) => obj is CharNode n && Equals(n);

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