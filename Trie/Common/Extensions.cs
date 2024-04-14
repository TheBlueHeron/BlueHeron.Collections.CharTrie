using System.Text;

namespace BlueHeron.Collections;

/// <summary>
/// Extension functions for use with <see cref="Trie.Trie"/>s.
/// </summary>
public static class GuidExtensions
{
    #region Fields

    private const string _MINUS = "-";
    private static readonly CompositeFormat fmtGuid = CompositeFormat.Parse("{0}-{1}-{2}-{3}-{4}");

    #endregion

    /// <summary>
    /// Converts this <see cref="Guid"/> to a string for use in a <see cref="Trie.Trie"/>.
    /// </summary>
    /// <param name="id">This <see cref="Guid"/></param>
    /// <returns>A <see cref="string"/> containing the numbers only</returns>
    public static string ToWord(this Guid id) => id.ToString().Replace(_MINUS, string.Empty);

    /// <summary>
    /// Tries to create a <see cref="Guid"/> from the given string.
    /// </summary>
    /// <param name="id">The condensed string representation of the <see cref="Guid"/></param>
    /// <returns>A <see cref="Guid"/></returns>
    public static Guid ToGuid(this string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentNullException(nameof(id));
        }
        if (id.Length != 32)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }
        return Guid.Parse(string.Format(null, fmtGuid,
            id[..8],
            id.Substring(8, 4),
            id.Substring(12, 4),
            id.Substring(16, 4),
            id.Substring(20, 12)));
    }
}