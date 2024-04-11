
namespace BlueHeron.Collections.Trie;

/// <summary>
/// Interface definition for a trie, a search optimized data structure.
/// Leaf nodes carry a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="TNode">The type of the nodes</typeparam>
/// <typeparam name="TValue">The type of the value carried by the <typeparamref name="TNode"/>s</typeparam>
public interface ITrie<TNode, TValue> : ITrie<TNode> where TNode: INode<TNode, TValue>, new()
{
    /// <summary>
    /// Adds the given word to the <see cref="ITrie{TNode, TValue}"/>.
    /// </summary>
    /// <param name="word">The <see cref="string"/> to add</param>
    /// <param name="value">The value of type <typeparamref name="TValue"/> represented by the <paramref name="word"/></param>
    void Add(string word, TValue value);

    /// <summary>
    /// Tries to find the given <see cref="TValue"/> and returns <see langword="true"/> if there is a match.
    /// </summary>
    /// <param name="value">The <see cref="TValue"> to find</param>
    /// <returns>Boolean, <see langword="true"/> if the value exists in the <see cref="ITrie{TNode, TValue}"/></returns>
    bool Exists(TValue value);

    /// <summary>
    /// Tries to retrieve all <see cref="TValue">s that match the given pattern of characters.
    /// </summary>
    /// <param name="pattern">The pattern of characters to match. A null value matches all characters at that depth</param>
    /// <returns>An <see cref="IEnumerable{TValue}"/> containing the value of all nodes that match the pattern</returns>
    IEnumerable<TValue?> FindValue(char?[] pattern);

    /// <summary>
    /// Gets the <see cref="TValue"/> carried by the given word.
    /// </summary>
    /// <param name="word">The <see cref="string"/> to match</param>
    /// <returns>A <see cref="TValue"/> if it exists; else <see langword="null"/></returns>
    TValue? GetValue(string word);

    /// <summary>
    /// Gets all values that match the given prefix.
    /// </summary>
    /// <param name="prefix">The <see cref="string"/> to match; if <see langword="null"/>: all words are returned</param>
    /// <returns>An <see cref="IEnumerable{TValue}"/></returns>
    IEnumerable<TValue?> GetValues(string? prefix);
}