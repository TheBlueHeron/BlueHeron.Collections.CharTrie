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
public class Trie
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
    /// Returns the total number of words in this <see cref="Trie{TNode}"/>.
    /// </summary>
    public int NumWords => RootNode.NumWords;

    /// <summary>
    /// The root <see cref="TNode"/>.
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
    /// Providse access to the list of registered types for the <see cref="Serialization.NodeConverter"/> that needs it when deserializing nodes.
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
    public bool Exists(object value)
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
    /// Tries to retrieve all words that match the given <see cref="PatternMatch">.
    /// </summary>
    /// <param name="pattern">The <see cref="PatternMatch"> to match/param>
    /// <returns>An <see cref="IEnumerable{string}"/> containing all words that match the pattern</returns>
    public IEnumerable<string> Find([DisallowNull] PatternMatch pattern)
    {
        foreach (var word in pattern.Type == PatternMatchType.IsFragment?
            WalkContaining(Root, pattern, new StringBuilder()):
            Walk(Root, pattern, new StringBuilder(), pattern.Count))
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
        foreach (var word in WalkContaining(Root, fragment, new StringBuilder()))
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
    /// <param name="pattern">The <see cref="PatternMatch"/> to match</param>
    /// <returns>An <see cref="IEnumerable{Object?}"/> containing the value of all nodes that match the pattern</returns>
    public IEnumerable<object?> FindValues(PatternMatch pattern)
    {
        foreach (var word in pattern.Type == PatternMatchType.IsFragment ?
            WalkValuesContaining(Root, pattern) :
            WalkValues(Root, pattern, new StringBuilder(), 0))            
        {
            yield return word;
        }
    }

    /// <summary>
    /// Retrieves all values whose keys contain the given string.
    /// </summary>
    /// <param name="fragment">The string to match</param>
    /// <returns>An <see cref="IEnumerable{Object?}"/></returns>
    public IEnumerable<object?> FindValuesContaining(string fragment)
    {
        foreach (var value in WalkValuesContaining(Root, fragment))
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
    /// Exports this <see cref="Trie"/> to the file with the given name.
    /// </summary>
    /// <param name="fileName">The full path and file name, including extension</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use</param>
    /// <returns>A <see cref="bool"/>, signifying the result of the operation</returns>
    /// <exception cref="InvalidOperationException">The file could not be created or written to</exception>
    public bool Export(string fileName, JsonSerializerOptions? options = null)
    {
        try
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                using var writer = File.CreateText(fileName);
                writer.Write(JsonSerializer.Serialize(this, options ?? mSerializerOptions));
                writer.Flush();
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
    /// Exports this <see cref="Trie"/> to the file with the given name asynchronously.
    /// </summary>
    /// <param name="fileName">The full path and file name, including extension</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use</param>
    /// <returns>A <see cref="Task{Boolean}"/>, signifying the result of the operation</returns>
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
    /// Creates a new <see cref="Trie"/> and tries to import all words in the given text file. One word per line is expected.
    /// Whitespace is trimmed. Empty lines are ignored.
    /// </summary>
    /// <param name="fi">The <see cref="FileInfo"/></param>
    /// <returns>A <see cref="Trie?"/></returns>
    /// <exception cref="InvalidOperationException">The file could not be opened and read as a text file.</exception>
    public static Trie? Import(FileInfo fi)
    {
        Trie? trie = null;

        if (fi != null && fi.Exists)
        {
            trie = new Trie();
            try
            {
                using var reader = fi.OpenText();
                var curLine = reader.ReadLine();
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
                    curLine = reader.ReadLine();
                }
#if DEBUG
                Debug.WriteLine("Lines read: {0} | Lines added: {1} | NumWords: {2}.", numLinesRead, numLinesAdded, trie.NumWords);
#endif
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format(null, errImpEx, _Import, fi.FullName), ex);
            }
        }
        return trie;
    }

    /// <summary>
    /// Creates a new <see cref="Trie"/> and tries to import all words in the given text file asynchronously. One word per line is expected.
    /// Whitespace is trimmed. Empty lines are ignored.
    /// </summary>
    /// <param name="fi">The <see cref="FileInfo"/></param>
    /// <returns>A <see cref="Task{Trie?}"/></returns>
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
    /// Creates a <see cref="Trie"/> from the given json file.
    /// </summary>
    /// <param name="fi">The <see cref="FileInfo"/></param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use</param>
    /// <returns>A <see cref="Trie?"/></returns>
    /// <exception cref="InvalidOperationException">The file could not be opened and read as a text file or its contents could not be parsed into a <see cref="Trie"/>.</exception>
    public static Trie? Load(FileInfo fi, JsonSerializerOptions? options = null)
    {
        Trie? trie = null;

        if (fi != null && fi.Exists)
        {
            try
            {
                using var stream = fi.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
                trie = JsonSerializer.Deserialize<Trie>(stream, options ?? mSerializerOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format(null, errImpEx, _Import, fi.FullName), ex);
            }
        }
        return trie;
    }

    /// <summary>
    /// Creates a <see cref="Trie"/> from the given json file asynchronously.
    /// </summary>
    /// <param name="fi">The <see cref="FileInfo"/></param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use</param>
    /// <returns>A <see cref="Task{Trie}"/></returns>
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

    /// <summary>
    /// Walks depth-first through the tree and returns every <see cref="Node"/> that is encountered, accompanied with its key.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{KeyValuePair{char, Node}}"/></returns>
    public IEnumerable<KeyValuePair<char, Node>> Walk()
    {
        return Walk(new KeyValuePair<char, Node>(_rootChar, RootNode));
    }

    /// <summary>
    /// Walks depth-first through the tree starting at the node represented by the given prefix and returns every <see cref="Node"/> that is encountered, accompanied with its key.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{KeyValuePair{char, Node}}"/></returns>
    public IEnumerable<KeyValuePair<char, Node>>? Walk(string prefix)
    {
        var node = GetNode(prefix);
        if (node != null)
        {
            return Walk(new KeyValuePair<char, Node>(prefix.Last(), node));
        }
        return default;
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
            node.NumChildren = -1; // force recalculation
            node.NumWords = -1;
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
    /// Removes unneeded <see cref="Node"/>s going up from a <see cref="Node"/> to the root node.
    /// </summary>
    private static void Trim(Stack<KeyValuePair<char, Node>> nodes)
    {
        while (nodes.Count > 1)
        {
            var node = nodes.Pop();
            var parentNode = nodes.Peek().Value;

            parentNode.NumChildren = -1; // -1: unset
            parentNode.NumWords = -1;
            if (node.Value.IsWord || node.Value.Children.Count != 0)
            {
                break;
            }
            parentNode.Children.Remove(node.Key);
        }
        var root = nodes.Peek().Value;

        root.NumChildren = -1; // root node to unset as well
        root.NumWords = -1;
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
    /// Tries to retrieve all words that match the given <see cref="PatternMatch"/> starting from the given node.
    /// </summary>
    /// <param name="node">The <see cref="Node"/> to start from</param>
    /// <param name="pattern">The <see cref="PatternMatch"> to use</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    /// <returns>An <see cref="IEnumerable{string}"/></returns>
    private IEnumerable<string> Walk(Node node, PatternMatch pattern, StringBuilder buffer, int length)
    {
        if (pattern.Count == 0)
        {
            foreach (var word in Find(buffer.ToString()))
            {
                if (pattern.Type != PatternMatchType.IsWord || word.Length == length)
                {
                    yield return word;
                }
            }
        }
        else
        {
            var curMatch = pattern[0];
            var childPattern = new PatternMatch(pattern.Skip(1), pattern.Type);
            var childNodes = curMatch == null || curMatch.Type == CharMatchType.All ? node.Children.Entries : node.Children.Entries.Where(kv => curMatch.IsMatch(kv.Key));
            foreach (var child in childNodes)
            {
                buffer.Append(child.Key);
                foreach (var word in Walk(child.Value, childPattern, buffer, length))
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
    /// Gets all the words that contain the given string recursively, starting from the given <see cref="Node"/>.
    /// </summary>
    /// <param name="node">The node to start from</param>
    /// <param name="fragment">The string to match</param>
    /// <param name="matchCount">Signifies how many characters have already been matched (i.e. how to proceed)</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    private static IEnumerable<string> WalkContaining(Node node, string fragment, StringBuilder buffer, int matchCount = 0)
    {
        if (fragment.Length == matchCount)
        {
            if (node.IsWord) // match
            {
                yield return buffer.ToString();
            }
            foreach (var child in node.Children.Entries)  // all subsequent words are a match
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
            foreach (var child in node.Children.Entries)
            {
                buffer.Append(child.Key);
                if (child.Key == charToMatch) // char is a match; try to match any remaining string
                {
                    foreach (var word in WalkContaining(child.Value, fragment, buffer, matchCount + 1))
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
                    foreach (var word in WalkContaining(nextNode, fragment, buffer, 0))
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
    /// Gets all the words that contain the given string recursively, starting from the given <see cref="Node"/>.
    /// </summary>
    /// <param name="node">The node to start from</param>
    /// <param name="fragment">The <see cref="PatternMatch"> to match</param>
    /// <param name="matchCount">Signifies how many characters have already been matched (i.e. how to proceed)</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    private static IEnumerable<string> WalkContaining(Node node, PatternMatch fragment, StringBuilder buffer, int matchCount = 0)
    {
        if (fragment.Count == matchCount)
        {
            if (node.IsWord) // match
            {
                yield return buffer.ToString();
            }
            foreach (var child in node.Children.Entries)  // all subsequent words are a match
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
            foreach (var child in node.Children.Entries)
            {
                buffer.Append(child.Key);
                if (charToMatch.IsMatch(child.Key)) // char is a match; try to match any remaining string
                {
                    foreach (var word in WalkContaining(child.Value, fragment, buffer, matchCount + 1))
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
                    foreach (var word in WalkContaining(nextNode, fragment, buffer, 0))
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
    /// Tries to retrieve all values that match the given pattern of characters starting from the given node.
    /// </summary>
    /// <param name="node">The <see cref="Node"/> to start from</param>
    /// <param name="pattern">The <see cref="PatternMatch"/> to match</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re-)use</param>
    /// <returns>An <see cref="IEnumerable{Object?}"/></returns>
    private IEnumerable<object?> WalkValues(Node node, PatternMatch pattern, StringBuilder buffer, int curDepth)
    {
        if (pattern.Count == 0)
        {
            var startNode = Root.GetNode(buffer.ToString());
            if (startNode != null)
            {
                if (pattern.Type == PatternMatchType.IsWord )
                {
                    if (startNode.IsWord)
                    {
                        yield return startNode.Value;
                    }
                }
                else
                {
                    {
                        foreach (var value in Walk(startNode))
                        {
                            yield return value;
                        }
                    }
                }
            }
        }
        else
        {
            var curMatch = pattern[0];
            var childPattern = new PatternMatch(pattern.Skip(1), pattern.Type);
            var childNodes = curMatch.Type == CharMatchType.All ? node.Children.Entries : node.Children.Entries.Where(kv => curMatch.IsMatch(kv.Key));
            foreach (var child in childNodes)
            {
                buffer.Append(child.Key);
                foreach (var value in WalkValues(child.Value, childPattern, buffer, curDepth + 1))
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
    /// <param name="fragment">The <see cref="PatternMatch"/> to match</param>
    /// <param name="matchCount">Signifies how many characters have already been matched (i.e. how to proceed)</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    private static IEnumerable<object?> WalkValuesContaining(Node node, PatternMatch fragment, int matchCount = 0)
    {
        if (fragment.Count == matchCount)
        {
            if (node.IsWord) // match
            {
                yield return node.Value;
            }
            foreach (var child in node.Children.Entries)  // all subsequent words are a match
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
            foreach (var child in node.Children.Entries)
            {
                if (charToMatch.IsMatch(child.Key)) // char is a match; try to match any remaining string
                {
                    foreach (var value in WalkValuesContaining(child.Value, fragment, matchCount + 1))
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
                    foreach (var value in WalkValuesContaining(nextNode, fragment, 0))
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

    /// <summary>
    /// Gets all the values whose keys contain the given string recursively, starting from the given <see cref="Node"/>.
    /// </summary>
    /// <param name="node">The node to start from</param>
    /// <param name="fragment">The string to match</param>
    /// <param name="matchCount">Signifies how many characters have already been matched (i.e. how to proceed)</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    private static IEnumerable<object?> WalkValuesContaining(Node node, string fragment, int matchCount = 0)
    {
        if (fragment.Length == matchCount)
        {
            if (node.IsWord) // match
            {
                yield return node.Value;
            }
            foreach (var child in node.Children.Entries)  // all subsequent words are a match
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
            foreach (var child in node.Children.Entries)
            {
                if (child.Key == charToMatch) // char is a match; try to match any remaining string
                {
                    foreach (var value in WalkValuesContaining(child.Value, fragment, matchCount + 1))
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
                    foreach (var value in WalkValuesContaining(nextNode, fragment, 0))
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