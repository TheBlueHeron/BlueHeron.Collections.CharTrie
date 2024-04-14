using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// A search optimized data structure for words.
/// </summary>
/// <typeparam name="TNode">The type of the nodes</typeparam>
public class Trie
{
    #region Objects and variables

    private const char _rootChar = ' ';

    #endregion

    #region Properties

    /// <summary>
    /// Returns the total number of words in this <see cref="Trie{TNode}"/>.
    /// </summary>
    [JsonIgnore()]
    public int NumWords => RootNode.NumWords;

    /// <summary>
    /// The root <see cref="TNode"/>.
    /// </summary>
    [JsonIgnore()]
    public Node Root => RootNode;

    [JsonInclude(), JsonPropertyName("rt")]
    internal Node RootNode { get; set; } = new Node();

    #endregion

    #region Public methods and functions

    /// <summary>
    /// Adds the given word to the <see cref="Trie"/>.
    /// </summary>
    /// <param name="word">The <see cref="string"/> to add</param>
    public void Add(string word)
    {
        var node = AddWord(word);
        node.IsWord = true;
    }

    /// <summary>
    /// Adds the given word to the <see cref="Trie"/>.
    /// </summary>
    /// <param name="word">The <see cref="string"/> to add</param>
    /// <param name="value">The value represented by the <paramref name="word"/></param>
    public void Add(string word, object value)
    {
        var node = AddWord(word);
        node.IsWord = true;
        node.Value = value;
    }

    /// <summary>
    /// Clears all words in the <see cref="Trie"/>.
    /// </summary>
    public void Clear()
    {
        Root.Children.Clear();
    }

    /// <summary>
    /// Tries to find the given <see cref="string"/> and returns <see langword="true"/> if there is a match.
    /// </summary>
    /// <param name="word">The word to find</param>
    /// <param name="isPrefix">If <see langword="true"/> return <see langword="true"/> if words starting with the given word exist, else only return <see langword="true"/> if an exact match is present</param>
    /// <returns>Boolean, <see langword="true"/> if the word exists in the <see cref="Trie"/></returns>
    public bool Exists(string word, bool isPrefix)
    {
        var node = Root;

        foreach (var c in word)
        {
            if (!node.Children.TryGetValue(c, out var value))
            {
                return false;
            }
            node = value;
        }
        return isPrefix || node.IsWord;
    }

    /// <summary>
    /// Tries to find the given value and returns <see langword="true"/> if there is a match.
    /// This is a rather expensive operation.
    /// </summary>
    /// <param name="value">The value> to find</param>
    /// <returns>Boolean, <see langword="true"/> if the value exists in the <see cref="Trie"/></returns>
    public bool Exists(object value)
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
    /// Gets all words that match the given prefix.
    /// </summary>
    /// <param name="prefix">The <see cref="string"/> to match; if <see langword="null"/>: all words are returned</param>
    /// <returns>An <see cref="IEnumerable{string}"/></returns>
    public IEnumerable<string> Find(string? prefix)
    {
        var startNode = prefix == null ? Root : Root.GetNode(prefix);

        if (startNode == null)
        {
            yield break;
        }
        else
        {
            foreach (var word in Walk(startNode, new StringBuilder(prefix)))
            {
                yield return word;
            }
        }
    }

    /// <summary>
    /// Tries to retrieve all words that match the given pattern of characters.
    /// </summary>
    /// <param name="pattern">The pattern of characters to match. A null value matches all characters at that depth</param>
    /// <param name="matchLength">If <see langword="true"/>, the word length must match the pattern length. Default: <see langword="false"/></param>
    /// <returns>An <see cref="IEnumerable{string}"/> containing all words that match the pattern</returns>
    public IEnumerable<string> Find(char?[] pattern, bool matchLength = false)
    {
        foreach (var word in Walk(Root, pattern, new StringBuilder(), matchLength, pattern.Length))
        {
            yield return word;
        }
    }

    /// <summary>
    /// Retrieves all words that contain the given string.
    /// </summary>
    /// <param name="fragment">The string to match</param>
    /// <returns>An <see cref="IEnumerable{string}"/> containing all words that contain the given string</returns>
    public IEnumerable<string> FindContaining(string fragment)
    {
        foreach (var word in Walk(Root, fragment, new StringBuilder()))
        {
            yield return word;
        }
    }

    /// <summary>
    /// Gets the value carried by the given word.
    /// </summary>
    /// <param name="word">The <see cref="string"/> to match</param>
    /// <returns>A value if it exists; else <see langword="null"/></returns>
    public object? FindValue(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            throw new ArgumentOutOfRangeException(nameof(word));
        }
        var node = Root.GetNode(word);

        return node?.Value;
    }

    /// <summary>
    /// Gets all values that match the given prefix.
    /// </summary>
    /// <param name="prefix">The <see cref="string"/> to match; if <see langword="null"/>: all words are returned</param>
    /// <returns>An <see cref="IEnumerable{Object?}"/></returns>
    public IEnumerable<object?> FindValues(string? prefix)
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
    /// Tries to retrieve all values that match the given pattern of characters.
    /// </summary>
    /// <param name="pattern">The pattern of characters to match. A null value matches all characters at that depth</param>
    /// <param name="matchLength">If <see langword="true"/>, the word length must match the pattern length. Default: <see langword="false"/></param>
    /// <returns>An <see cref="IEnumerable{Object?}"/> containing the value of all nodes that match the pattern</returns>
    public IEnumerable<object?> FindValues(char?[] pattern, bool matchLength = false)
    {
        foreach (var value in Walk(Root, pattern, new StringBuilder(), 0, matchLength))
        {
            yield return value;
        }
    }

    /// <summary>
    /// Retrieves all values whose keys contain the given string.
    /// </summary>
    /// <param name="fragment">The string to match</param>
    /// <returns>An <see cref="IEnumerable{Object?}"/></returns>
    public IEnumerable<object?> FindValuesContaining(string fragment)
    {
        foreach (var value in Walk(Root, fragment))
        {
            yield return value;
        }
    }

    /// <summary>
    /// Gets the <see cref="Node"/> in this <see cref="Trie"/> that represents the given prefix, if it exists. Else <see langword="null"/>.
    /// </summary>
    /// <param name="prefix">The <see cref="string"/> to match</param>
    /// <returns>A <see cref="Node"/> representing the given <see cref="string"/>, else <see langword="null"/></returns>
    public Node? GetNode(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        return Root.GetNode(prefix);
    }

    /// <summary>
    /// Returns the first word that carries the given value.
    /// </summary>
    /// <param name="value">The value for which to find the word</param>
    /// <returns>A <see cref="string"/> if the value could be found, else an empty string</returns>
    public string GetWord(object value)
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
    /// Removes all words matching the given prefix from the <see cref="Trie"/>.
    /// </summary>
    public void RemovePrefix(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        RemovePrefix(AsStack(prefix, false));
    }

    /// <summary>
    /// Removes the given word from the <see cref="Trie"/>.
    /// </summary>
    /// <returns>An <see cref="int"/> determining the number of words removed</returns>
    public void RemoveWord(string word)
    {
        ArgumentNullException.ThrowIfNull(word);
        RemoveWord(AsStack(word));
    }

    #endregion

    #region Private methods and functions

    /// <summary>
    /// Adds the given word to the <see cref="Trie"/> and returns the leaf node.
    /// </summary>
    /// <param name="word">The word to add</param>
    /// <returns>A <see cref="Node"/></returns>
    protected Node AddWord(string word)
    {
        var node = Root;

        foreach (var c in word)
        {
            if (!node.Children.TryGetValue(c, out var value))
            {
                value = new Node();
                node.Children.Add(c, value);
            }
            node = value;
        }
        return node;
    }

    /// <summary>
    /// Gets the <see cref="Node"/>s that form the given string as a <see cref="Stack{KeyValuePair{char, Node}}"/>.
    /// </summary>
    /// <param name="s">The <see cref="string"/> to match</param>
    /// <param name="isWord">The <paramref name="s"/> parameter is a word (i.e. not a prefix)</param>
    /// <returns>A <see cref="Stack{KeyValuePair{char, Node}}"/></returns>
    private Stack<KeyValuePair<char, Node>> AsStack(string s, bool isWord = true)
    {
        var nodes = new Stack<KeyValuePair<char, Node>>(s.Length + 1); // root node is included
        var _node = Root;

        nodes.Push(new KeyValuePair<char, Node>(_rootChar, _node));
        foreach (var c in s)
        {
            _node = _node.GetNode(c);
            if (_node == null)
            {
                nodes.Clear();
                break;
            }
            nodes.Push(new KeyValuePair<char, Node>(c, _node));
        }
        if (isWord)
        {
            if (!_node?.IsWord ?? true)
            {
                throw new ArgumentOutOfRangeException(s);
            }
        }
        return nodes;
    }

    /// <summary>
    /// Returns the first word that carries the given <value, starting from the given node.
    /// </summary>
    /// <param name="sb">The <see cref="StringBuilder"/> to use</param>
    /// <param name="node">The <see cref="Node"/> from which to start</param>
    /// <param name="value">The value> of which to find the word</param>
    private static bool GetWord(List<char> chars, Node node, object value)
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
    /// Removes the given prefix and trims the <see cref="Trie">.
    /// </summary>
    private static void RemovePrefix(Stack<KeyValuePair<char, Node>> nodes)
    {
        if (nodes.Count != 0)
        {
            nodes.Peek().Value.Clear(); // clear the last node
            Trim(nodes); // trim excess nodes
        }
    }

    /// <summary>
    /// Removes the given word and trims the <see cref="Trie">.
    /// </summary>
    private static void RemoveWord(Stack<KeyValuePair<char, Node>> nodes)
    {
        nodes.Peek().Value.IsWord = false; // mark the last node as not a word
        Trim(nodes); // trim excess nodes
    }

    /// <summary>
    /// Removes unneeded <see cref="Node">s going up from a <see cref="Node"/> to the root node.
    /// </summary>
    private static void Trim(Stack<KeyValuePair<char, Node>> nodes)
    {
        while (nodes.Count > 1)
        {
            var node = nodes.Pop();
            var parentNode = nodes.Peek().Value;

            parentNode.NumWords = -1; // -1: unset
            if (node.Value.IsWord || node.Value.Children.Count != 0)
            {
                break;
            }
            parentNode.Children.Remove(node.Key);
        }
        nodes.Peek().Value.NumWords = -1; // root node
    }

    /// <summary>
    /// Tries to retrieve all words that match the given pattern of characters starting from the given node.
    /// </summary>
    /// <param name="node">The <see cref="Node"/> to start from</param>
    /// <param name="pattern">The pattern to match</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    /// <param name="matchLength">If <see langword="true"/>, the word length must match the pattern length</param>
    /// <returns>An <see cref="IEnumerable{string}"/></returns>
    private IEnumerable<string> Walk(Node node, char?[] pattern, StringBuilder buffer, bool matchLength, int length)
    {
        if (pattern.Length == 0)
        {
            foreach (var word in Find(buffer.ToString()))
            {
                if (!matchLength || word.Length == length)
                {
                    yield return word;
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
                foreach (var word in Walk(child.Value, childPattern, buffer, matchLength, length))
                {
                    yield return word;
                }
                buffer.Length--;
            }
        }
    }

    /// <summary>
    /// Gets all the words recursively, starting from the given <see cref="Node"/>.
    /// </summary>
    /// <param name="node">The <see cref="Node"/> to start from</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    private static IEnumerable<string> Walk(Node node, StringBuilder buffer)
    {
        if (node.IsWord)
        {
            yield return buffer.ToString();
        }
        foreach (var child in node.Children)
        {
            buffer.Append(child.Key);
            foreach (var word in Walk(child.Value, buffer))
            {
                yield return word;
            }
            buffer.Length--;
        }
    }

    /// <summary>
    /// Gets all the words that contain the given string recursively, starting from the given <see cref="Node"/>.
    /// </summary>
    /// <param name="node">The node to start from</param>
    /// <param name="fragment">The string to match</param>
    /// <param name="matchCount">Signifies how many characters have already been matched (i.e. how to proceed)</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    private static IEnumerable<string> Walk(Node node, string fragment, StringBuilder buffer, int matchCount = 0)
    {
        if (fragment.Length == matchCount)
        {
            if (node.IsWord) // match
            {
                yield return buffer.ToString();
            }
            foreach (var child in node.Children)  // all subsequent words are a match
            {
                buffer.Append(child.Key);
                foreach (var word in Walk(child.Value, buffer))
                {
                    yield return word;
                }
                buffer.Length--;
            }
        }
        else
        {
            var charToMatch = fragment[matchCount];
            foreach (var child in node.Children)
            {
                buffer.Append(child.Key);
                if (child.Key == charToMatch) // char is a match; try to match any remaining string
                {
                    foreach (var word in Walk(child.Value, fragment, buffer, matchCount + 1))
                    {
                        yield return word;
                    }
                }
                else // if matchCount == 0 look further, else start over from current node
                {
                    Node nextNode;
                    if (matchCount == 0)
                    {
                        nextNode = child.Value;
                    }
                    else
                    {
                        nextNode = node;
                        buffer.Length--;
                    }
                    foreach (var word in Walk(nextNode, fragment, buffer, 0))
                    {
                        yield return word;
                    }
                    if (matchCount > 0)
                    {
                        break;
                    }
                }
                buffer.Length--;
            }
        }
    }

    /// <summary>
    /// Gets all the values recursively starting from the given <see cref="Node"/>.
    /// </summary>
    /// <param name="curDepth">The depth of the current node</param>
    /// <param name="length">The length of the word</param>
    private static IEnumerable<object?> Walk(Node node, int curDepth = 0, int length = 0)
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
    /// Tries to retrieve all values that match the given pattern of characters starting from the given node.
    /// </summary>
    /// <param name="node">The <see cref="Node"/> to start from</param>
    /// <param name="pattern">The pattern to match</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re-)use</param>
    /// <param name="matchLength">If <see langword="true"/>, the word length must match the pattern length</param>
    /// <returns>An <see cref="IEnumerable{Object?}"/></returns>
    private IEnumerable<object?> Walk(Node node, char?[] pattern, StringBuilder buffer, int curDepth, bool matchLength)
    {
        if (pattern.Length == 0)
        {
            var startNode = Root.GetNode(buffer.ToString());
            if (startNode != null)
            {
                foreach (var value in matchLength ? Walk(startNode, 0, pattern.Length) : Walk(startNode))
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
    /// Gets all the values whose keys contain the given string recursively, starting from the given <see cref="Node"/>.
    /// </summary>
    /// <param name="node">The node to start from</param>
    /// <param name="fragment">The string to match</param>
    /// <param name="matchCount">Signifies how many characters have already been matched (i.e. how to proceed)</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    private static IEnumerable<object?> Walk(Node node, string fragment, int matchCount = 0)
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
                    Node nextNode;
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