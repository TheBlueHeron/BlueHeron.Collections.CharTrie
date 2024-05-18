using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlueHeron.Collections.Trie.Search;
using BlueHeron.Collections.Trie.Serialization;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// A search optimized data structure for words.
/// </summary>
/// <typeparam name="TNode">The type of the nodes</typeparam>
[JsonConverter(typeof(TrieConverter))]
[SuppressMessage("Performance", "CA1710:Rename to end with Collection or Dictionary", Justification = "A fitting name already exists.")]
public class Trie : IEnumerable, IEnumerable<KeyValuePair<char, Node>>
{
    #region Objects and variables

    private const char _rootChar = ' ';
    private const string _Export = "export";
    private const string _Import = "import";

    private static readonly CompositeFormat errImpEx = CompositeFormat.Parse("Unable to {0} '{1}'. See inner exception for details.");
    private static List<string> mRegisteredTypes = [];
    private static readonly JsonSerializerOptions mSerializerOptions = new() { WriteIndented = false };

    #endregion

    #region Properties

    /// <summary>
    /// Returns the total number of words in this <see cref="Trie"/>.
    /// </summary>
    public int NumWords => RootNode.NumWords;

    /// <summary>
    /// The root <see cref="Node"/>.
    /// </summary>
    public Node Root => RootNode;

    /// <summary>
    /// List of registered types that is used in deserialization.
    /// </summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Needed for serialization combined with static access.")]
    public IEnumerable<string> RegisteredTypes
    {
        get => mRegisteredTypes;
        internal set => mRegisteredTypes = value.ToList();
    }

    /// <summary>
    /// Internally used, mutable root node.
    /// </summary>
    internal Node RootNode { get; set; } = new Node();

    /// <summary>
    /// Provides access to the list of registered types for the <see cref="NodeConverter"/> that needs it when deserializing nodes.
    /// </summary>
    internal static List<string> Types => mRegisteredTypes;

    #endregion

    #region Public methods and functions

    /// <summary>
    /// Adds the given word to the <see cref="Trie"/>.
    /// </summary>
    /// <param name="word">The <see cref="string"/> to add</param>
    public void Add(string word)
    {
        _ = AddWord(word);
    }

    /// <summary>
    /// Adds the given word to the <see cref="Trie"/>.
    /// </summary>
    /// <param name="word">The <see cref="string"/> to add</param>
    /// <param name="value">The value represented by the <paramref name="word"/></param>
    public void Add(string word, object value)
    {
        var typeName = value.GetType().AssemblyQualifiedName;

        if (string.IsNullOrEmpty(typeName))
        {
            throw new NotSupportedException(nameof(value));
        }
        var node = AddWord(word);
        var typeIndex = mRegisteredTypes.IndexOf(typeName);

        if (typeIndex == -1)
        {
            mRegisteredTypes.Add(typeName);
            typeIndex = mRegisteredTypes.Count - 1;
        }
        node.TypeIndex = typeIndex;
        node.Value = value;
    }

    /// <summary>
    /// Walks depth-first through the tree and returns every <see cref="Node"/> that is encountered, accompanied with its key.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{KeyValuePair{char, Node}}"/></returns>
    public IEnumerable<KeyValuePair<char, Node>> AsEnumerable()
    {
        return Walk(new KeyValuePair<char, Node>(_rootChar, RootNode));
    }

    /// <summary>
    /// Tries to find the given <see cref="string"/> and returns <see langword="true"/> if there is a match.
    /// </summary>
    /// <param name="word">The word to find</param>
    /// <param name="isPrefix">If <see langword="true"/> return <see langword="true"/> if words starting with the given word exist, else only return <see langword="true"/> if an exact match is present</param>
    /// <returns>Boolean, <see langword="true"/> if the word exists in the <see cref="Trie"/></returns>
    public bool Contains(string word, bool isPrefix)
    {
        var node = Root;

        foreach (var c in word)
        {
            if (!node.Children.Get(c, out var value))
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
    public bool ContainsValue(object value)
    {
        if (value is null)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        foreach (var v in Walk(Root))
        {
            if (v != null && value.Equals(v)) // Comparer<object>.Default.Compare(value, v) == 0
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets all words that match the given fragment.
    /// </summary>
    /// <param name="fragment">The <see cref="string"/> to match; if <see langword="null"/>: all words are returned</param>
    /// <param name="isPrefix">If <see langword="true"/>, the word should start with this fragment</param>
    /// <returns>An <see cref="IEnumerable{string}"/></returns>
    public IEnumerable<string> Find(string fragment, bool isPrefix)
    {
        foreach (var word in Find(isPrefix? PatternMatch.FromPrefix(fragment): PatternMatch.FromFragment(fragment)))
        {
            yield return word;
        }
    }

    /// <summary>
    /// Tries to retrieve all words that match the given <see cref="PatternMatch"/>.
    /// </summary>
    /// <param name="pattern">The <see cref="PatternMatch"> to match</param>
    /// <returns>An <see cref="IEnumerable{string}"/> containing all words that match the pattern</returns>
    public IEnumerable<string> Find(PatternMatch pattern)
    {
        foreach (var word in Walk(Root, pattern, new StringBuilder(), pattern.Count, 0))
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
    /// Gets all values that match the given fragment.
    /// </summary>
    /// <param name="fragment">The <see cref="string"/> to match; if <see langword="null"/>: all words are returned</param>
    /// <param name="isPrefix">If <see langword="true"/>, the word should start with this fragment</param>
    /// <returns>An <see cref="IEnumerable{object?}"/></returns>
    public IEnumerable<object?> FindValues(string fragment, bool isPrefix)
    {
        foreach (var value in FindValues(isPrefix ? PatternMatch.FromPrefix(fragment) : PatternMatch.FromFragment(fragment)))
        {
            yield return value;
        }
    }

    /// <summary>
    /// Tries to retrieve all values that match the given <see cref="PatternMatch"/>.
    /// </summary>
    /// <param name="pattern">The <see cref="PatternMatch"/> to match</param>
    /// <returns>An <see cref="IEnumerable{object?}"/> containing the value of all nodes that match the <see cref="PatternMatch"/></returns>
    public IEnumerable<object?> FindValues(PatternMatch pattern)
    {
        foreach (var value in WalkValues(Root, pattern, new StringBuilder(), 0, pattern.Type == PatternMatchType.IsWord ? pattern.Count : 0, 0))            
        {
            yield return value;
        }
    }

    /// <summary>
    /// Gets the <see cref="Node"/> in this <see cref="Trie"/> that represents the given prefix, if it exists. Else <see langword="null"/> is returned.
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
    /// <returns>A <see cref="string"/> if the value could be found, else <see langword="null"></returns>
    public string? GetWord(object value)
    {
        List<char> chars = [];

        if (GetWord(chars, Root, value))
        {
            chars.Reverse(); // last character was added first
            return new string(chars.ToArray());
        }
        return null;
    }

    /// <summary>
    /// Removes all words matching the given prefix from the <see cref="Trie"/>.
    /// </summary>
    /// <param name="fragment">The fragment to match</param>
    /// <param name="isPrefix">If <see langword="true"/>, the word should start with this fragment</param>
    public void Remove(string fragment, bool isPrefix)
    {
        ArgumentNullException.ThrowIfNull(fragment);
        if (isPrefix)
        {
            RemovePrefix(AsStack(fragment, false));
        }
        else
        {
            RemoveWord(AsStack(fragment));
        }
    }

    #region IO

    /// <summary>
    /// Exports this <see cref="Trie"/> to the file with the given name asynchronously.
    /// </summary>
    /// <param name="fileName">The full path and file name, including extension</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use</param>
    /// <returns>A <see cref="bool"/>, signifying the result of the operation</returns>
    /// <exception cref="InvalidOperationException">The file could not be created or written to</exception>
    public async Task<bool> ExportAsync(string fileName, JsonSerializerOptions? options = null)
    {
        try
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                using var writer = File.CreateText(fileName);
                await writer.WriteAsync(JsonSerializer.Serialize(this, options ?? mSerializerOptions));
                await writer.FlushAsync();
                writer.Close();
                return true;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(string.Format(null, errImpEx, _Export, fileName), ex);
        }
        return false;
    }

    /// <summary>
    /// Creates a new <see cref="Trie"/> and tries to import all words in the given text file asynchronously.
    /// One word per line is expected, whitespace is trimmed and empty lines will be ignored.
    /// </summary>
    /// <param name="fi">The <see cref="FileInfo"/></param>
    /// <returns>A <see cref="Trie?"/></returns>
    /// <exception cref="InvalidOperationException">The file could not be opened and read as a text file.</exception>
    public static async Task<Trie?> ImportAsync(FileInfo fi)
    {
        Trie? trie = null;

        if (fi != null && fi.Exists)
        {
            trie = new Trie();
            try
            {
                using var reader = fi.OpenText();
                var curLine = await reader.ReadLineAsync();
                var numLinesRead = 0;
                var numLinesAdded = 0;
                while (curLine != null)
                {
                    numLinesRead++;
                    if (curLine.Length > 0)
                    {
                        trie.Add(curLine.Trim());
                        numLinesAdded++;
                    }
                    curLine = await reader.ReadLineAsync();
                }
#if DEBUG
                Debug.WriteLine("Lines read: {0} | Lines added: {1} | NumWords: {2}.", numLinesRead, numLinesAdded, trie.NumWords);
#endif
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format(null, errImpEx,_Import, fi.FullName), ex);
            }
        }
        return trie;
    }

    /// <summary>
    /// Creates a <see cref="Trie"/> from the given json file asynchronously.
    /// </summary>
    /// <param name="fi">The <see cref="FileInfo"/></param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use</param>
    /// <returns>A <see cref="Trie?"/></returns>
    /// <exception cref="InvalidOperationException">The file could not be opened and read as a text file or its contents could not be parsed into a <see cref="Trie"/>.</exception>
    public static async Task<Trie?> LoadAsync(FileInfo fi, JsonSerializerOptions? options = null)
    {
        Trie? trie = null;

        if (fi != null && fi.Exists)
        {
            try
            {
                using var stream = fi.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
                trie = await JsonSerializer.DeserializeAsync<Trie>(stream, options ?? mSerializerOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format(null, errImpEx, _Import, fi.FullName), ex);
            }
        }
        return trie;
    }

    #endregion

    #endregion

    #region Private methods and functions

    /// <summary>
    /// Adds the given word to the <see cref="Trie"/> and returns the leaf node.
    /// </summary>
    /// <param name="word">The word to add</param>
    /// <returns>A <see cref="Node"/></returns>
    private Node AddWord(string word)
    {
        var node = Root;

        foreach (var c in word)
        {
            node.Unset(); // force recalculation
            if (!node.Children.Get(c, out var value))
            {
                value = new Node();
                node.Children.Emplace(c, value);
            }
            node = value;
        }
        node.IsWord = true;
        return node;
    }

    /// <summary>
    /// Gets the <see cref="Node"/>s that form the given string as a <see cref="Stack{KeyValuePair{char, Node}}"/>.
    /// </summary>
    /// <param name="s">The <see cref="string"/> to match</param>
    /// <param name="isWord">The <paramref name="s"/> parameter is a word and not a prefix</param>
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
    /// Returns the first word that carries the given <see cref="Node.Value"/>, starting from the given node.
    /// </summary>
    /// <param name="chars">The <see cref="List{char}"/> to append characters to</param>
    /// <param name="node">The <see cref="Node"/> from which to start</param>
    /// <param name="value">The value> of which to find the word</param>
    private static bool GetWord(List<char> chars, Node node, object value)
    {
        foreach (var item in node.Children.Entries)
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
        nodes.Peek().Value.Children.Clear(); // clear the last node
        Trim(nodes); // trim excess nodes
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
    /// Removes <see cref="Node"/>s that have become unneeded after removal of one or more words, going up from a <see cref="Node"/> to the root node.
    /// </summary>
    private static void Trim(Stack<KeyValuePair<char, Node>> nodes)
    {
        while (nodes.Count > 1)
        {
            var node = nodes.Pop();
            var parentNode = nodes.Peek().Value;

            parentNode.Unset(); // force recalculation
            if (node.Value.IsWord || node.Value.Children.Count != 0)
            {
                break;
            }
            parentNode.Children.Remove(node.Key);
        }
        nodes.Peek().Value.Unset(); // root node needs to recalculate as well
    }

    /// <summary>
    /// Walks depth-first through the tree starting at the given node and returns every <see cref="Node"/> that is encountered, accompanied with its key.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{KeyValuePair{char, Node}}"/></returns>
    internal static IEnumerable<KeyValuePair<char, Node>> Walk(KeyValuePair<char, Node> node)
    {
        yield return node;
        foreach (var child in node.Value.Children.Entries)
        {
            foreach (var c in Walk(child))
            {
                yield return c;
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
        foreach (var child in node.Children.Entries)
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
    /// Tries to retrieve all words that match the given <see cref="PatternMatch"/> starting from the given <see cref="Node"/>.
    /// </summary>
    /// <param name="node">The <see cref="Node"/> to start from</param>
    /// <param name="pattern">The <see cref="PatternMatch"/> to use</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    /// <returns>An <see cref="IEnumerable{string}"/></returns>
    private static IEnumerable<string> Walk(Node node, PatternMatch pattern, StringBuilder buffer, int length, int matchCount)
    {
        if (matchCount == pattern.Count) // all words in this subtree are a match for fragment and prefix pattern types, and for word pattern type when word length matches as well
        {
            foreach (var word in Walk(node, buffer))
            {
                if (pattern.Type != PatternMatchType.IsWord || word.Length == length)
                {
                    yield return word;
                }
            }
        }
        else if (node.RemainingDepth >= pattern.Count - matchCount) // words are available that may be matched to the (remaining) pattern
        {
            var curMatch = pattern[matchCount];

            foreach (var child in node.Children.Entries)
            {
                buffer.Append(child.Key);
                if (curMatch.IsMatch(child.Key)) // keep matching
                {
                    foreach (var word in Walk(child.Value, pattern, buffer, length, matchCount + 1))
                    {
                        yield return word;
                    }
                }
                else if (pattern.Type == PatternMatchType.IsFragment) // start over
                {
                    if (matchCount == 0) // look further
                    {
                        foreach (var word in Walk(child.Value, pattern, buffer, length, 0))
                        {
                            yield return word;
                        }
                    }
                    else
                    {
                        buffer.Length--;
                        foreach (var word in Walk(node, pattern, buffer, length, 0))
                        {
                            yield return word;
                        }
                        continue;
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
        foreach (var child in node.Children.Entries)
        {
            foreach (var value in Walk(child.Value, curDepth++, length))
            {
                yield return value;
            }
        }
    }

    /// <summary>
    /// Tries to retrieve all values that match the given <see cref="PatternMatch"/> starting from the given <see cref="Node"/>.
    /// </summary>
    /// <param name="node">The <see cref="Node"/> to start from</param>
    /// <param name="pattern">The <see cref="PatternMatch"/> to match</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re-)use</param>
    /// <returns>An <see cref="IEnumerable{object?}"/></returns>
    private static IEnumerable<object?> WalkValues(Node node, PatternMatch pattern, StringBuilder buffer, int curDepth, int length, int matchCount)
    {
        if (matchCount == pattern.Count) // all words in this subtree are a match for fragment and prefix pattern types, and for word pattern type when word length matches as well
        {
            foreach (var value in Walk(node, curDepth + 1, length))
            {
                yield return value;
            }
        }
        else if (node.RemainingDepth >= pattern.Count - matchCount) // words are available that may be matched to the (remaining) pattern
        {
            var curMatch = pattern[matchCount];

            foreach (var child in node.Children.Entries)
            {
                buffer.Append(child.Key);
                if (curMatch.IsMatch(child.Key)) // keep matching
                {
                    foreach (var value in WalkValues(child.Value, pattern, buffer,curDepth + 1, length, matchCount + 1))
                    {
                        yield return value;
                    }
                }
                else if (pattern.Type == PatternMatchType.IsFragment) // start over
                {
                    if (matchCount == 0) // look further
                    {
                        foreach (var value in WalkValues(child.Value, pattern, buffer, curDepth + 1, length, 0))
                        {
                            yield return value;
                        }
                    }
                    else
                    {
                        buffer.Length--;
                        foreach (var value in WalkValues(node, pattern, buffer, curDepth, length, 0))
                        {
                            yield return value;
                        }
                        continue;
                    }
                }
                buffer.Length--;
            }
        }
    }

    public IEnumerator<KeyValuePair<char, Node>> GetEnumerator() => AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => AsEnumerable().GetEnumerator();

    #endregion
}