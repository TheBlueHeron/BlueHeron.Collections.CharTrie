
namespace BlueHeron.Collections.Trie.Search;

/// <summary>
/// A <see cref="List{CharMatch}"/> that represents a pattern of characters, including wildcards.
/// </summary>
public class PatternMatch : List<CharMatch>
{
    #region Properties

    /// <summary>
    /// Gets or sets the <see cref="PatternMatchType"/> to use. Default: <see cref="PatternMatchType.IsPrefix"/>.
    /// </summary>
    public PatternMatchType Type { get; set; }

    #endregion

    #region Construction

    /// <summary>
    /// Creates a new, empty <see cref="PatternMatch"/>.
    /// </summary>
    public PatternMatch(): base(){}

    /// <summary>
    /// Creates a new <see cref="PatternMatch"/> from the given <see cref="IEnumerable{CharMatch}"/>.
    /// </summary>
    public PatternMatch(IEnumerable<CharMatch> collection, PatternMatchType type) : base(collection) { Type = type; }

    #endregion

    #region Public methods and functions

    /// <summary>
    /// Adds a <see cref="CharMatch"/> to the collection.
    /// </summary>
    /// <param name="character">The chharacter to match</param>
    /// <param name="type">The <see cref="CharMatchType"/></param>
    public void Add(char? character, CharMatchType type = CharMatchType.First)
    {
        Add(new CharMatch(character, type));
    }

    /// <summary>
    /// Adds a <see cref="CharMatch"/> to the collection.
    /// </summary>
    /// <param name="character">The chharacter to match</param>
    /// <param name="alternatives">Option array of alternative characters to match</param>
    /// <param name="type">The <see cref="CharMatchType"/></param>
    public void Add(char? character, char[]? alternatives, CharMatchType type = CharMatchType.First)
    {
        Add(new CharMatch(character, alternatives, type));
    }

    #endregion
}