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
    /// This is a rather expensive operation.
    /// </summary>
    /// <param name="value">The <see cref="TValue"> to find</param>
    /// <returns>Boolean, <see langword="true"/> if the value exists in the <see cref="ITrie{TNode, TValue}"/></returns>
    public bool Exists(TValue value)
    {
        if (value is null)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        foreach (var v in Walk(Root))
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
    public TValue? FindValue(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            throw new ArgumentOutOfRangeException(nameof(word));
        }
        var node = Root.GetNode(word);

        return node == null ? default : node.Value;
    }

    /// <summary>
    /// Gets all values that match the given prefix.
    /// </summary>
    /// <param name="prefix">The <see cref="string"/> to match; if <see langword="null"/>: all words are returned</param>
    /// <returns>An <see cref="IEnumerable{TValue}"/></returns>
    public IEnumerable<TValue?> FindValues(string? prefix)
    {
        var startNode = prefix == null ? Root : Root.GetNode(prefix);

        if (startNode == null)
        {
            yield break;
        }
        else
        {
            foreach (var value in Walk(startNode))
            {
                yield return value;
            }
        }
    }

    /// <summary>
    /// Tries to retrieve all <see cref="TValue"/>s that match the given pattern of characters.
    /// </summary>
    /// <param name="pattern">The pattern of characters to match. A null value matches all characters at that depth</param>
    /// <param name="matchLength">If <see langword="true"/>, the word length must match the pattern length. Default: <see langword="false"/></param>
    /// <returns>An <see cref="IEnumerable{TValue?}"/> containing the value of all nodes that match the pattern</returns>
    public IEnumerable<TValue?> FindValues(char?[] pattern, bool matchLength = false)
    {
        foreach (var value in Walk(Root, pattern, new StringBuilder(), 0, matchLength))
        {
            yield return value;
        }
    }

    /// <summary>
    /// Retrieves all <see cref="TValue">s whose keys contain the given string.
    /// </summary>
    /// <param name="fragment">The string to match</param>
    /// <returns>An <see cref="IEnumerable{TValue?}"/></returns>
    public IEnumerable<TValue?> FindValuesContaining(string fragment)
    {
        foreach (var value in Walk(Root, fragment))
        {
            yield return value;
        }
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
    /// Tries to retrieve all values that match the given pattern of characters starting from the given node.
    /// </summary>
    /// <param name="node">The <see cref="TNode"/> to start from</param>
    /// <param name="pattern">The pattern to match</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re-)use</param>
    /// <param name="matchLength">If <see langword="true"/>, the word length must match the pattern length</param>
    /// <returns>An <see cref="IEnumerable{TValue?}"/></returns>
    private IEnumerable<TValue?> Walk(TNode node, char?[] pattern, StringBuilder buffer, int curDepth, bool matchLength)
    {
        if (pattern.Length == 0)
        {
            var startNode = Root.GetNode(buffer.ToString());
            if (startNode != null)
            {
                foreach (var value in matchLength? Walk(startNode, 0, pattern.Length) : Walk(startNode))
                {
                    yield return value;
                }
            }
        }
        else
        {
            var curChar = pattern[0];
            var childPattern = pattern.Skip(1).ToArray();
            var childNodes = curChar == null ? node.Children : node.Children.Where(kv => kv.Key == curChar);
            foreach (var child in childNodes)
            {
                buffer.Append(child.Key);
                foreach (var value in Walk(child.Value, childPattern, buffer, curDepth + 1, matchLength))
                {
                    yield return value;
                }
                buffer.Length--;
            }
        }
    }

    /// <summary>
    /// Gets all the values recursively starting from the given <typeparamref name="TNode"/>.
    /// </summary>
    /// <param name="curDepth">The depth of the current node</param>
    /// <param name="length">The length of the word</param>
    private static IEnumerable<TValue?> Walk(TNode node, int curDepth = 0, int length = 0)
    {
        if (node.IsWord && (length == 0 || length == curDepth))
        {
            yield return node.Value;
        }
        foreach (var child in node.Children)
        {
            foreach (var value in Walk(child.Value))
            {
                yield return value;
            }
        }
    }

    /// <summary>
    /// Gets all the <see cref="TValue">s whose keys contain the given string recursively, starting from the given <see cref="TNode"/>.
    /// </summary>
    /// <param name="node">The node to start from</param>
    /// <param name="fragment">The string to match</param>
    /// <param name="matchCount">Signifies how many characters have already been matched (i.e. how to proceed)</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    private static IEnumerable<TValue?> Walk(TNode node, string fragment, int matchCount = 0)
    {
        if (fragment.Length == matchCount)
        {
            if (node.IsWord) // match
            {
                yield return node.Value;
            }
            foreach (var child in node.Children)  // all subsequent words are a match
            {
                foreach (var value in Walk(child.Value))
                {
                    yield return value;
                }
            }
        }
        else
        {
            var charToMatch = fragment[matchCount];
            foreach (var child in node.Children)
            {
                if (child.Key == charToMatch) // char is a match; try to match any remaining string
                {
                    foreach (var value in Walk(child.Value, fragment, matchCount + 1))
                    {
                        yield return value;
                    }
                }
                else // if matchCount == 0 look further, else start over from current node
                {
                    TNode nextNode;
                    if (matchCount == 0)
                    {
                        nextNode = child.Value;
                    }
                    else
                    {
                        nextNode = node;
                    }
                    foreach (var value in Walk(nextNode, fragment, 0))
                    {
                        yield return value;
                    }
                    if (matchCount > 0)
                    {
                        break;
                    }
                }
            }
        }
    }

    #endregion
}