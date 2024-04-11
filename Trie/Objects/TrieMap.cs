using System.Text;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// A <see cref="TrieMap{MapNode, TValue}"/>.
/// </summary>
/// <typeparam name="TValue">The type of the values carried by the leaf nodes</typeparam>
public class TrieMap<TValue> : TrieMap<MapNode<TValue>, TValue> {}

/// <summary>
/// A search optimized data structure for words that represent a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the value carried by leaf nodes</typeparam>
public class TrieMap<TNode, TValue> : Trie<TNode>, ITrie<TNode, TValue> where TNode : INode<TNode, TValue>, new()
{
    #region Public methods and functions

    /// <summary>
    /// Adds the given word to the <see cref="TrieMap{TNode, TValue}"/>.
    /// </summary>
    /// <param name="word">The <see cref="string"/> to add</param>
    /// <param name="value">The value represented by the <paramref name="word"/></param>
    public void Add(string word, TValue value)
    {
        var node = AddWord(word);
        node.IsWord = true;
        node.Value = value;
    }

    /// <summary>
    /// Tries to find the given <see cref="TValue"/> and returns <see langword="true"/> if there is a match.
    /// </summary>
    /// <param name="value">The <see cref="TValue"> to find</param>
    /// <returns>Boolean, <see langword="true"/> if the value exists in the <see cref="ITrie{TNode, TValue}"/></returns>
    public bool Exists(TValue value)
    {
        if (value is null)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        foreach (var v in Traverse(Root))
        {
            if (v != null && value.Equals(v))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the <see cref="TValue"/> carried by the given word.
    /// </summary>
    /// <param name="word">The <see cref="string"/> to match</param>
    /// <returns>A <see cref="TValue"/> if it exists; else <see langword="null"/></returns>
    public TValue? GetValue(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            throw new ArgumentOutOfRangeException(nameof(word));
        }
        var node = Root.GetNode(word);

        return node == null ? default : node.Value;
    }

    /// <summary>
    /// Returns the first word that carries the given <see cref="TValue"/>.
    /// </summary>
    /// <param name="value">The <see cref="TValue"/> for which to find the word</param>
    /// <returns>A <see cref="string"/> if the value could be found, else an empty string</returns>
    public string GetWord(TValue value)
    {
        List<char> chars = [];

        if (GetWord(chars, Root, value))
        {
            chars.Reverse(); // last character was added first
            return new string(chars.ToArray());
        }
        return string.Empty;
    }

    /// <summary>
    /// Gets all values that match the given prefix.
    /// </summary>
    /// <param name="prefix">The <see cref="string"/> to match; if <see langword="null"/>: all words are returned</param>
    /// <returns>An <see cref="IEnumerable{TValue}"/></returns>
    public IEnumerable<TValue?> GetValues(string? prefix)
    {
        var startNode = prefix == null ? Root : Root.GetNode(prefix);

        if (startNode == null)
        {
            yield break;
        }
        else
        {
            foreach (var value in Traverse(startNode))
            {
                yield return value;
            }
        }
    }

    #endregion

    #region Private methods and functions

    /// <summary>
    /// Returns the first word that carries the given <see cref="TValue"/>, starting from the given node.
    /// </summary>
    /// <param name="sb">The <see cref="StringBuilder"/> to use</param>
    /// <param name="node">The <see cref="TNode"/> from which to start</param>
    /// <param name="value">The <see cref="TValue"/> of which to find the word</param>
    private static bool GetWord(List<char> chars, TNode node, TValue value)
    {
        foreach (var item in node.Children)
        {
            var childNode = item.Value;

            if (childNode.Value != null && childNode.Value.Equals(value))
            {
                chars.Add(item.Key);
                return true;
            }
            else
            {
                if (GetWord(chars, childNode, value))
                {
                    chars.Add(item.Key);
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Gets all the values recursively starting from the given <typeparamref name="TNode"/>.
    /// </summary>
    private static IEnumerable<TValue?> Traverse(TNode node)
    {
        if (node.IsWord)
        {
            yield return node.Value;
        }
        foreach (var child in node.Children)
        {
            foreach (var value in Traverse(child.Value))
            {
                yield return value;
            }
        }
    }

    #endregion
}