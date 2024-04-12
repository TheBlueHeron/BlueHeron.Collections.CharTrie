using System.Text;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// A <see cref="Trie{Node}"/>.
/// </summary>
public class Trie : Trie<Node> {}

/// <summary>
/// A search optimized data structure for words.
/// </summary>
/// <typeparam name="TNode">The type of the nodes</typeparam>
public class Trie<TNode> : ITrie<TNode> where TNode : INode<TNode>, new()
{
    #region Objects and variables

    private const char _rootChar = ' ';

    #endregion

    #region Properties

    /// <summary>
    /// Returns the total number of words in this <see cref="Trie{TNode}"/>.
    /// </summary>
    public int NumWords => Root.NumWords;

    /// <summary>
    /// The root <see cref="TNode"/>.
    /// </summary>
    internal TNode Root { get; } = new TNode();

    #endregion

    #region Public methods and functions

    /// <summary>
    /// Adds the given word to the <see cref="Trie{TNode}"/>.
    /// </summary>
    /// <param name="word">The <see cref="string"/> to add</param>
    public void Add(string word)
    {
        var node = AddWord(word);
        node.IsWord = true;
    }

    /// <summary>
    /// Clears all words in the <see cref="Trie{TNode}"/>.
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
    /// <returns>Boolean, <see langword="true"/> if the word exists in the <see cref="Trie{TNode}"/></returns>
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

    ///// <summary>
    ///// Tries to retrieve all words that contain the given characters.
    ///// </summary>
    ///// <param name="chars">The characters to match. The order of appearance is not important</param>
    ///// <returns>An <see cref="IEnumerable{string}"/> containing all words that contain the given characters</returns>
    //public IEnumerable<string> FindContaining(char[] chars)
    //{
    //    Dictionary<char, int> matchings = [];

    //    foreach (var c in chars)
    //    {
    //        if (matchings.TryGetValue(c, out var value))
    //        {
    //            matchings[c] = ++value;
    //        }
    //        else
    //        {
    //            matchings.Add(c, 1);
    //        }
    //    }

    //    foreach (var word in FindContaining(Root, matchings, new StringBuilder()))
    //    {
    //        yield return word;
    //    }
    //}

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
    /// Gets the <see cref="TNode"/> in this <see cref="Trie{TNode}"/> that represents the given prefix, if it exists. Else <see langword="null"/>.
    /// </summary>
    /// <param name="prefix">The <see cref="string"/> to match</param>
    /// <returns>A <see cref="TNode"/> representing the given <see cref="string"/>, else <see langword="null"/></returns>
    public TNode? GetNode(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        return Root.GetNode(prefix);
    }

    /// <summary>
    /// Removes all words matching the given prefix from the <see cref="Trie{TNode}"/>.
    /// </summary>
    public void RemovePrefix(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        RemovePrefix(AsStack(prefix, false));
    }

    /// <summary>
    /// Removes the given word from the <see cref="Trie{TNode}"/>.
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
    /// Adds the given word to the <see cref="Trie{TNode}"/> and returns the leaf node.
    /// </summary>
    /// <param name="word">The word to add</param>
    /// <returns>A <see cref="TNode"/></returns>
    protected TNode AddWord(string word)
    {
        var node = Root;

        foreach (var c in word)
        {
            if (!node.Children.TryGetValue(c, out var value))
            {
                value = new TNode();
                node.Children.Add(c, value);
            }
            node = value;
        }
        return node;
    }

    /// <summary>
    /// Gets the <see cref="TNode"/>s that form the given string as a <see cref="Stack{KeyValuePair{char, TNode}}"/>.
    /// </summary>
    /// <param name="s">The <see cref="string"/> to match</param>
    /// <param name="isWord">The <paramref name="s"/> parameter is a word (i.e. not a prefix)</param>
    /// <returns>A <see cref="Stack{KeyValuePair{char, TNode}}"/></returns>
    private Stack<KeyValuePair<char, TNode>> AsStack(string s, bool isWord = true)
    {
        var nodes = new Stack<KeyValuePair<char, TNode>>(s.Length + 1); // root node is included
        var _node = Root;

        nodes.Push(new KeyValuePair<char, TNode>(_rootChar, _node));
        foreach (var c in s)
        {
            _node = _node.GetNode(c);
            if (_node == null)
            {
                nodes.Clear();
                break;
            }
            nodes.Push(new KeyValuePair<char, TNode>(c, _node));
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
    /// Removes the given prefix and trims the <see cref="Trie">.
    /// </summary>
    private static void RemovePrefix(Stack<KeyValuePair<char, TNode>> nodes)
    {
        if (nodes.Count != 0)
        {
            nodes.Peek().Value.Clear(); // clear the last node
            Trim(nodes); // trim excess nodes
        }
    }

    /// <summary>
    /// Removes the given word and trims the <see cref="Trie{TNode}">.
    /// </summary>
    private static void RemoveWord(Stack<KeyValuePair<char, TNode>> nodes)
    {
        nodes.Peek().Value.IsWord = false; // mark the last node as not a word
        Trim(nodes); // trim excess nodes
    }

    /// <summary>
    /// Removes unneeded <see cref="Node">s going up from a <see cref="TNode"/> to the root node.
    /// </summary>
    private static void Trim(Stack<KeyValuePair<char, TNode>> nodes)
    {
        while (nodes.Count > 1)
        {
            var node = nodes.Pop();
            var parentNode = nodes.Peek().Value;
            if (node.Value.IsWord || node.Value.Children.Count != 0)
            {
                break;
            }
            parentNode.Children.Remove(node.Key);
        }
    }

    /// <summary>
    /// Tries to retrieve all words that match the given pattern of characters starting from the given node.
    /// </summary>
    /// <param name="node">The <see cref="TNode"/> to start from</param>
    /// <param name="pattern">The pattern to match</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    /// <param name="matchLength">If <see langword="true"/>, the word length must match the pattern length</param>
    /// <returns>An <see cref="IEnumerable{string}"/></returns>
    private IEnumerable<string> Walk(TNode node, char?[] pattern, StringBuilder buffer, bool matchLength, int length)
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
    /// Gets all the words recursively, starting from the given <see cref="TNode"/>.
    /// </summary>
    /// <param name="node">The <see cref="TNode"/> to start from</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    private static IEnumerable<string> Walk(TNode node, StringBuilder buffer)
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
    /// Gets all the words that contain the given string recursively, starting from the given <see cref="TNode"/>.
    /// </summary>
    /// <param name="node">The node to start from</param>
    /// <param name="fragment">The string to match</param>
    /// <param name="matchCount">Signifies how many characters have already been matched (i.e. how to proceed)</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    private static IEnumerable<string> Walk(TNode node, string fragment, StringBuilder buffer, int matchCount = 0)
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
                    TNode nextNode;
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

    ///// <summary>
    ///// Gets all the words containing the given characters recursively starting from the given <see cref="TNode"/>.
    ///// </summary>
    ///// <
    //private static IEnumerable<string> Walk(TNode node, Dictionary<char, int> matchings, StringBuilder buffer)
    //{
    //    var isMatch = false; // flag to indicate that all child nodes are a match as well (no find operation needed)

    //    if (node.IsWord)
    //    {

    //        if (isMatch)
    //        {
    //            yield return buffer.ToString();
    //        }
    //    }
    //    foreach (var child in node.Children)
    //    {
    //        buffer.Append(child.Key);
    //        foreach (var word in isMatch? Walk(child.Value, buffer) : Walk(child.Value, matchings, buffer))
    //        {
    //            yield return word;
    //        }
    //        buffer.Length--;
    //    }
    //}

    #endregion
}