
namespace BlueHeron.Collections.Trie;

/// <summary>
/// Interface definition for a trie, a search optimized data structure.
/// </summary>
/// <typeparam name="TNode">The type of the nodes</typeparam>
public interface ITrie<TNode> where TNode : INode<TNode>, new()
{
    /// <summary>
    /// Returns the total number of words in this <see cref="ITrie{TNode}"/>.
    /// </summary>
    int NumWords { get; }

    /// <summary>
    /// Adds the given word to the <see cref="ITrie{TNode}"/>.
    /// </summary>
    /// <param name="word">The <see cref="string"/> to add</param>
    void Add(string word);

    /// <summary>
    /// Clears all words in the <see cref="ITrie{TNode}"/>.
    /// </summary>
    void Clear();

    /// <summary>
    /// Tries to find the given <see cref="string"/> and returns <see langword="true"/> if there is a match.
    /// </summary>
    /// <param name="word">The word to find</param>
    /// <param name="isPrefix">If <see langword="true"/> return <see langword="true"/> if words starting with the given word exist, else only return <see langword="true"/> if an exact match is present</param>
    /// <returns>Boolean, <see langword="true"/> if the word exists in the <see cref="ITrie{TNode}"/></returns>
    bool Exists(string word, bool isPrefix);

    /// <summary>
    /// Gets the <typeparamref name="TNode"/> in this <see cref="ITrie{TNode}"/> that represents the given prefix, if it exists. Else <see langword="null"/>.
    /// </summary>
    /// <param name="prefix">The <see cref="string"/> to match</param>
    /// <returns>A <typeparamref name="TNode"/> representing the given <see cref="string"/>, else <see langword="null"/></returns>
    TNode? GetNode(string prefix);

    /// <summary>
    /// Gets all words that match the given prefix.
    /// </summary>
    /// <param name="prefix">The <see cref="string"/> to match; if <see langword="null"/>: all words are returned</param>
    /// <returns>An <see cref="IEnumerable{string}"/></returns>
    IEnumerable<string> GetWords(string? prefix);

    /// <summary>
    /// Removes all words matching the given prefix from the <see cref="ITrie{TNode}"/>.
    /// </summary>
    void RemovePrefix(string prefix);

    /// <summary>
    /// Removes the given word from the <see cref="ITrie{TNode}"/>.
    /// </summary>
    void RemoveWord(string word);
}