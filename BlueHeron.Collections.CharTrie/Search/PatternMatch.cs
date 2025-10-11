using System.Diagnostics;

namespace BlueHeron.Collections.Trie.Search;

/// <summary>
/// An extended <see cref="List{CharMatch}"/> that represents a pattern of characters, including wildcards.
/// </summary>
[DebuggerStepThrough()]
public class PatternMatch : List<CharMatch>
{
    #region Objects and variables

    internal const string _DOTSTAR = ".*";
    internal const string _INVALID = "The pattern is invalid. Check ValidationStatus.";

    private int mChecksum = -1; // flag to check if validation must be performed
    private PatternMatchType mType;
    private ValidationStatus mValidationStatus;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the <see cref="PatternMatchType"/> to use. Default: <see cref="PatternMatchType.IsPrefix"/>.
    /// </summary>
    public PatternMatchType MatchType {
        get => mType;
        set
        {
            mType = value;
            mChecksum = -1; // force validation
        }
    }

    /// <summary>
    /// Gets the regex expression for this <see cref="PatternMatch"/>.
    /// </summary>
    public string Regex => ToString();

    /// <summary>
    /// Gets the <see cref="Search.ValidationStatus"/> of this <see cref="PatternMatch"/>.
    /// </summary>
    public ValidationStatus ValidationStatus
    {
        get
        {
            if (mChecksum != Count) // something has changed -> (re-)validate
            {
                mValidationStatus = Validate();
            }
            return mValidationStatus;
        }
    }

    #endregion

    #region Construction

    /// <summary>
    /// Creates a new, empty <see cref="PatternMatch"/>.
    /// </summary>
    [DebuggerStepThrough()]
    public PatternMatch(): base() { }

    /// <summary>
    /// Creates a new <see cref="PatternMatch"/> from the given <see cref="IEnumerable{char?}"/>.
    /// A null char in the collection will yield a wildcard for that position.
    /// </summary>
    /// <param name="pattern">The <see cref="IEnumerable{char?}"/></param>
    /// <param name="type">The <see cref="PatternMatchType"/></param>
    [DebuggerStepThrough()]
    public PatternMatch(IEnumerable<char?> pattern, PatternMatchType type) : base(pattern.ToCharMatchArray()) { MatchType = type; }

    /// <summary>
    /// Creates a new <see cref="PatternMatch"/> from the given <see cref="IEnumerable{CharMatch}"/>.
    /// </summary>
    /// <param name="collection">The <see cref="IEnumerable{CharMatch}"/></param>
    /// <param name="type">The <see cref="PatternMatchType"/></param>
    [DebuggerStepThrough()]
    public PatternMatch(IEnumerable<CharMatch> collection, PatternMatchType type) : base(collection) { MatchType = type; }

    #endregion

    #region Public methods and functions

    /// <summary>
    /// Adds a <see cref="CharMatch"/> to the collection.
    /// </summary>
    /// <param name="character">The character to match</param>
    public void Add(char character) => Add(new CharMatch(character));

    /// <summary>
    /// Adds a <see cref="CharMatch"/> to the collection.
    /// </summary>
    /// <param name="character">The character to match</param>
    /// <param name="alternatives">Option array of alternative characters to match</param>
    public void Add(char character, char[]? alternatives) => Add(new CharMatch(character, alternatives));

    /// <summary>
    /// Creates a <see cref="PatternMatch"/> of type <see cref="PatternMatchType.IsFragment"/> representing the given fragment.
    /// </summary>
    /// <param name="fragment">The fragment</param>
    /// <returns>A <see cref="PatternMatch"/></returns>
    public static PatternMatch FromFragment(string fragment) => new(fragment.ToCharMatchArray(), PatternMatchType.IsFragment);

    /// <summary>
    /// Creates a <see cref="PatternMatch"/> of type <see cref="PatternMatchType.IsPrefix"/> representing the given prefix.
    /// </summary>
    /// <param name="prefix">The prefix</param>
    /// <returns>A <see cref="PatternMatch"/></returns>
    public static PatternMatch FromPrefix(string? prefix) => new(prefix.ToCharMatchArray(), PatternMatchType.IsPrefix);

    /// <summary>
    /// Creates a <see cref="PatternMatch"/> of type <see cref="PatternMatchType.IsSuffix"/> representing the given suffix.
    /// </summary>
    /// <param name="suffix">The suffix</param>
    /// <returns>A <see cref="PatternMatch"/></returns>
    public static PatternMatch FromSuffix(string? suffix) => new(suffix.ToCharMatchArray(), PatternMatchType.IsSuffix);

    /// <summary>
    /// Creates a <see cref="PatternMatch"/> of type <see cref="PatternMatchType.IsWord"/> representing the given word.
    /// </summary>
    /// <param name="word">The word</param>
    /// <returns>A <see cref="PatternMatch"/></returns>
    public static PatternMatch FromWord(string word) => new(word.ToCharMatchArray(), PatternMatchType.IsWord);

    #endregion

    #region Private methods and functions

    /// <summary>
    /// Validates the pattern of this <see cref="PatternMatch"/>.
    /// </summary>
    /// <returns>A <see cref="Search.ValidationStatus"/></returns>
    private ValidationStatus Validate()
    {
        mChecksum = Count;
        if (this.First().Primary == null)
        {
            if (mType == PatternMatchType.IsFragment)
            {
                return ValidationStatus.InvalidStartingWildCard;
            }
        }
        if (this.Last().Primary == null)
        {
            if (mType == PatternMatchType.IsFragment)
            {
                return ValidationStatus.InvalidEndingWildCard;
            }
        }
        return ValidationStatus.Valid;
    }

    #endregion

    #region Overrides

    /// <summary>
    /// Overridden to return this <see cref="PatternMatch"/> as a regex expression.
    /// </summary>
    /// <returns>A regular expression</returns>
    public override string ToString()
    {
        if (Count == 0)
        {
            return string.Empty;
        }
        var strRegex = string.Join(string.Empty, this.Select(c => c.ToString()));

        return MatchType switch
        {
            PatternMatchType.IsWord => strRegex,
            PatternMatchType.IsFragment => _DOTSTAR + strRegex + _DOTSTAR,
            PatternMatchType.IsPrefix => strRegex + _DOTSTAR,
            _ => _DOTSTAR + strRegex // suffix
        };
    }

    #endregion
}