using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlueHeron.Collections.Trie.Search;
using BlueHeron.Collections.Trie.Serialization;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// A search optimized data structure for words.
/// </summary>
[JsonConverter(typeof(TrieConverter))]
[SuppressMessage("Performance", "CA1710:Rename to end with Collection or Dictionary", Justification = "Nomen est omen.")]
public sealed class Trie : IEnumerable, IEnumerable<Trie.Node>
{
    #region Fields

    private Node mRoot;
    private const char _rootChar = char.MaxValue;
    private const string _Export = "export";
    private const string _Import = "import";

    private static readonly CompositeFormat errImpEx = CompositeFormat.Parse("Unable to {0} '{1}'. See inner exception for details.");
    private static readonly JsonSerializerOptions mSerializerOptions = new() { WriteIndented = false };

    #endregion

    #region Nodes

    /// <summary>
    /// A node in the <see cref="Trie"/>, which represents a character.
    /// </summary>
    [JsonConverter(typeof(NodeSerializer))]
    [StructLayout(LayoutKind.Auto)]
    public struct Node: IComparable<Node>, IEquatable<Node>
    {
        #region Fields

        private int mRemainingDepth;

        /// <summary>
        /// The character. Is only <see langword="null"/> on the root node.
        /// </summary>
        public char Character;
        /// <summary>
        /// The <see cref="Node"/>'s collection of child <see cref="Node"/>s.
        /// </summary>
        public Node[] Children;
        /// <summary>
        /// Determines whether this <see cref="Node"/> finishes a word.
        /// </summary>
        public bool IsWord;
        /// <summary>
        /// The maximum depth of this <see cref="Node"/>'s tree of children.
        /// </summary>
        public int RemainingDepth
        {
            get
            {
                if (mRemainingDepth < 0)
                {
                    mRemainingDepth = Children.Length == 0 ? 0 : 1 + Children.Max(n => n.RemainingDepth);
                }
                return mRemainingDepth;
            }
            internal set => mRemainingDepth = value;
        }
        /// <summary>
        /// The value that is represented by this <see cref="Node"/>.
        /// </summary>
        public string? Value;

        #endregion

        #region Construction

        /// <summary>
        /// Creates a new node without a <see cref="Character"/> and an empty <see cref="Children"/> array.
        /// </summary>
        public Node() { Children = []; RemainingDepth = -1; }

        /// <summary>
        /// Creates a new <see cref="Node"/>.
        /// </summary>
        /// <param name="character">The character</param>
        /// <param name="isWord">Determines whether this <see cref="Node"/> finishes a word</param>
        /// <param name="value">The value, represented by this <see cref="Node"/></param>
        internal Node(char character, bool isWord, string? value = null) : this()
        {
            Character = character;
            IsWord = isWord;
            Value = value;
        }

        #endregion

        #region Public methods and functions

        /// <summary>
        /// Returns a <see cref="NodeReference"/> to the child <see cref="Node"/> that represent the given <see cref="char"/> if it exists, else <see langword="null"/>.
        /// </summary>
        /// <param name="character">The <see cref="char"/> to find</param>
        /// <returns>A <see cref="NodeReference"/></returns>
        public NodeReference GetNode(char character)
        {
            var idx = Search(ref Children, 0, Children.Length - 1, character);
            return idx < 0 ? new NodeReference() : new NodeReference(ref Children[idx]);
        }

        /// <summary>
        /// Sets the given <see cref="NodeReference"/> to wrap the <see cref="Node"/> representing the given prefix, or <see langword="null"/>.
        /// Returns a value signifying whether the given prefix could be located.
        /// </summary>
        /// <param name="prefix">The <see cref="string"/> prefix to match</param>
        /// <param name="nodeRef">Will hold a <see cref="NodeReference"/> to the <see cref="Node"/> representing the given prefix, or <see langword="null"/></param>
        /// <returns>A value signifying whether the prefix could be located</returns>
        public static bool GetNode(string prefix, ref NodeReference nodeRef)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                throw new ArgumentNullException(nameof(prefix));
            }
            //var nodeRef = new NodeReference(ref this);

            foreach (var prefixChar in prefix)
            {
                nodeRef = nodeRef.Node.GetNode(prefixChar);
                if (!nodeRef.HasNode)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns the total number of<see cref="Node"/>s under this <see cref="Node"/>, including this <see cref="Node"/> itself.
        /// </summary>
        public readonly int NumNodes() => 1 + Children.Sum(n => n.NumNodes());

        /// <summary>
        /// Returns the number of words represented by this <see cref="Node"/> and its children.
        /// </summary>
        public readonly int NumWords() => (IsWord ? 1 : 0) + Children.Sum(n => n.NumWords());

        #endregion

        #region IComparable

        /// <summary>
        /// Executes <see cref="char.CompareTo(char)"/>.
        /// </summary>
        /// <param name="other">The <see cref="Node"/> to compare</param>
        public readonly int CompareTo(Node other) => Character.CompareTo(other.Character);

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj) => obj is Node node && Equals(node);

        /// <inheritdoc/>
        public readonly override int GetHashCode() => Character.GetHashCode();

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="other">An object to compare with this instance or null</param>
        /// <returns><see cref="bool"/>, <see langword="true"/> if the specified object is equal to this instance</returns>
        public readonly bool Equals(Node other) => Character.Equals(other.Character);

        public static bool operator ==(Node left, Node right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Node left, Node right)
        {
            return !(left == right);
        }

        public static bool operator <(Node left, Node right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(Node left, Node right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(Node left, Node right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(Node left, Node right)
        {
            return left.CompareTo(right) >= 0;
        }

        #endregion
    }

    /// <summary>
    /// Object that constitutes a nullable reference to a <see cref="Trie.Node"/> with a <see cref="Visited"/> property that is used in <see cref="PatternMatchType.IsFragment"/> type searches.
    /// </summary>
    public ref struct NodeReference
    {
        #region Objects and variables

        private readonly ref Node mNode;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value, determining whether this <see cref="NodeReference"/> wraps a <see cref="Trie.Node"/> or is <see langword="null"/>.
        /// </summary>
        public readonly bool HasNode => !Unsafe.IsNullRef(ref mNode);

        /// <summary>
        /// Determines whether this node was matched to a wildcard and shouldn't be marked as visited.
        /// </summary>
        internal bool IsWildCard { get; set; }

        /// <summary>
        /// Gets a reference to the wrapped <see cref="Trie.Node"/>.
        /// If <see cref="HasNode"/> is <see langword="false"/>, an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        /// <exception cref="InvalidOperationException">No <see cref="Trie.Node"/> is present. Call <see cref="HasNode"/> first.</exception>
        public readonly ref Node Node
        {
            get
            {
                if (!HasNode) { throw new InvalidOperationException(); }
                return ref mNode;
            }
        }

        /// <summary>
        /// Determines whether this node already served as the start node of a walk.
        /// This prevents identical retries from occurring multiple times.
        /// </summary>
        internal bool Visited { get; set; }

        #endregion

        #region Construction

        /// <summary>
        /// Creates a new <see cref="NodeReference"/>, representing a reference to the given <see cref="Trie.Node"/>.
        /// </summary>
        /// <param name="node">A reference to a <see cref="Trie.Node"/></param>
        /// <param name="wildCard">Determines whether this node was matched to a wildcard and shouldn't be marked as visited</param>
        internal NodeReference(ref Node node, bool wildCard = false)
        {
            mNode = ref node;
            IsWildCard = wildCard;
        }

        #endregion
    }

    /// <summary>
    /// Wrapper for a <see cref="Trie.Node"/> that is used in deserialization and <see cref="Search.PatternMatchType.IsFragment"/> searches.
    /// </summary>
    /// <param name="node">The <see cref="Node"/></param>
    [JsonConverter(typeof(NodeDeserializer))]
    internal sealed class DeserializedNode(Node node)
    {
        #region Objects and variables

        private int? mNumChildren;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the wrapped <see cref="Trie.Node"/>.
        /// </summary>
        public Node Node = node;

        /// <summary>
        /// Gets the expected number of <see cref="Trie.Node"/>s in the <see cref="Trie.Node"/>'s <see cref="Node.Children"/> array.
        /// </summary>
        public int NumChildren
        {
            get
            {
                if (!mNumChildren.HasValue)
                {
                    mNumChildren = Node.Children.Length;
                }
                return mNumChildren.Value;
            }
            internal set => mNumChildren = value;
        }

        #endregion
    }

    #endregion

    #region Construction

    /// <summary>
    /// Creates a new <see cref="Trie"/>.
    /// </summary>
    public Trie()
    {
        mRoot = new Node(_rootChar, false);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the root <see cref="Node"/>.
    /// </summary>
    public Node RootNode
    {
        get => mRoot;
        internal set => mRoot = value;
    }

    #endregion

    #region Public methods and functions

    /// <summary>
    /// Adds the given word to the collection.
    /// </summary>
    /// <param name="word">The word to add</param>
    /// <param name="value">Optional value to assign</param>
    public void Add(string word, string? value = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(word);
        var nodeIndexes = new int[word.Length];
        Array.Fill(nodeIndexes, -1);

        for (var i = 0; i < word.Length; i++)
        {
            var curChar = word[i];
            var isWord = i == word.Length - 1;

            nodeIndexes[i] = AddNode(curChar, ref GetNode(ref nodeIndexes),  isWord, value);
        }
    }

    /// <summary>
    /// Walks depth-first through the tree and returns every <see cref="Node"/> that is encountered, accompanied with its key.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{Node}"/></returns>
    public IEnumerable<Node> AsEnumerable()
    {
        return Walk(new NodeReference(ref mRoot));
    }

    /// <summary>
    /// Tries to find the given <see cref="string"/> and returns <see langword="true"/> if there is a match.
    /// </summary>
    /// <param name="word">The word to find</param>
    /// <param name="isPrefix">If <see langword="true"/> return <see langword="true"/> if words starting with the given word exist, else only return <see langword="true"/> if an exact match is present</param>
    /// <returns>Boolean, <see langword="true"/> if the word exists in the <see cref="Collections.Trie"/></returns>
    public bool Contains(string word, bool isPrefix)
    {
        ArgumentException.ThrowIfNullOrEmpty(word, nameof(word));
        var nodeRef = new NodeReference(ref mRoot);

        foreach (var c in word)
        {
            var childIndex = Search(ref nodeRef.Node.Children,0, nodeRef.Node.Children.Length - 1, c);
            if (childIndex < 0)
            {
                return false;
            }
            nodeRef = new NodeReference(ref nodeRef.Node.Children[childIndex]);
        }
        return isPrefix || nodeRef.Node.IsWord;
    }

    /// <summary>
    /// Tries to find the given value and returns <see langword="true"/> if there is a match.
    /// This is a rather expensive operation.
    /// </summary>
    /// <param name="value">The value to find</param>
    /// <returns>Boolean, <see langword="true"/> if the value exists in the <see cref="Collections.Trie"/></returns>
    public bool ContainsValue(string value)
    {
        foreach (var v in Walk(new NodeReference(ref mRoot), 0, 0))
        {
            if (value == v)
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
        foreach (var word in Find(isPrefix ? PatternMatch.FromPrefix(fragment) : PatternMatch.FromFragment(fragment)))
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
        ArgumentNullException.ThrowIfNull(pattern, nameof(pattern));
        var nodeRef = new NodeReference(ref mRoot);

        if (pattern.Type == PatternMatchType.IsFragment)
        {
            foreach (var word in WalkContaining(ref nodeRef, pattern, new StringBuilder(), 0))
            {
                yield return word;
            }
        }
        else
        {
            foreach (var word in Walk(nodeRef, pattern, new StringBuilder(), pattern.Type == PatternMatchType.IsWord ? pattern.Count : 0, 0))
            {
                yield return word;
            }
        }
    }

    /// <summary>
    /// Gets the value carried by the given word.
    /// </summary>
    /// <param name="word">The <see cref="string"/> to match</param>
    /// <returns>A value if it exists; else <see langword="null"/></returns>
    public string? FindValue(string word)
    {
        ArgumentException.ThrowIfNullOrEmpty(word, nameof(word));
        var nodeRef = new NodeReference(ref mRoot);

        if (Node.GetNode(word, ref nodeRef))
        {
            return nodeRef.Node.Value;
        }
        return null;
    }

    /// <summary>
    /// Gets all values that match the given fragment.
    /// </summary>
    /// <param name="fragment">The <see cref="string"/> to match; if <see langword="null"/>: all words are returned</param>
    /// <param name="isPrefix">If <see langword="true"/>, the word should start with this fragment</param>
    /// <returns>An <see cref="IEnumerable{string?}"/></returns>
    public IEnumerable<string?> FindValues(string fragment, bool isPrefix)
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
    /// <returns>An <see cref="IEnumerable{string?}"/> containing the value of all words that match the <see cref="PatternMatch"/></returns>
    public IEnumerable<string?> FindValues(PatternMatch pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern, nameof(pattern));
        var nodeRef = new NodeReference(ref mRoot);

        if (pattern.Type == PatternMatchType.IsFragment)
        {
            foreach (var value in WalkValuesContaining(ref nodeRef, pattern, new StringBuilder(), 0, 0, 0))
            {
                yield return value;
            }
        }
        else
        {
            foreach (var value in WalkValues(nodeRef, pattern, new StringBuilder(), 0, pattern.Type == PatternMatchType.IsWord ? pattern.Count : 0, 0))
            {
                yield return value;
            }
        }
    }

    /// <summary>
    /// Returns an <see cref="IEnumerator{Node}"/> that allows for enumerating all <see cref="Node"/>s in this <see cref="Trie"/>.
    /// </summary>
    /// <returns>An <see cref="IEnumerator{Node}"/></returns>
    public IEnumerator GetEnumerator() => AsEnumerable().GetEnumerator();

    /// <summary>
    /// Gets a <see cref="NodeReference"/> to the <see cref="Node"/> in this <see cref="Trie"/> that represents the given prefix, if it exists, or <see langword="null"/>.
    /// </summary>
    /// <param name="prefix">The <see cref="string"/> to match</param>
    /// <returns>A <see cref="NodeReference"/></returns>
    public NodeReference GetNode(string prefix)
    {
        ArgumentException.ThrowIfNullOrEmpty(prefix);
        var nodeRef = new NodeReference(ref mRoot);

        return Node.GetNode(prefix, ref nodeRef) ? nodeRef : new NodeReference();
    }

    /// <summary>
    /// Returns the first word that carries the given value.
    /// </summary>
    /// <param name="value">The value for which to find the word</param>
    /// <returns>A <see cref="string"/> if the value could be found, else <see langword="null"></returns>
    public string? GetWord(string value)
    {
        List<char> chars = [];

        if (GetWord(chars, mRoot, value))
        {
            chars.Reverse(); // last character was added first
            return new string(chars.ToArray());
        }
        return null;
    }

    /// <summary>
    /// Returns the number of <see cref="Node"/>s in this <see cref="Collections.Trie"/>.
    /// </summary>
    public int NumNodes() => mRoot.NumNodes();

    /// <summary>
    /// Returns the number of words represented by this <see cref="Collections.Trie"/>.
    /// </summary>
    public int NumWords() => mRoot.NumWords();

    /// <summary>
    /// Removes all words matching the given prefix from the <see cref="Collections.Trie"/>.
    /// </summary>
    /// <param name="fragment">The fragment to match</param>
    /// <param name="isPrefix">If <see langword="true"/>, the word should start with this fragment</param>
    public bool Remove(string fragment, bool isPrefix)
    {
        ArgumentException.ThrowIfNullOrEmpty(fragment);
        var nodeIndexes = new int[fragment.Length];
        var node = mRoot;

        bool deleteAndTrim()
        {
            if (nodeIndexes.Length == 0)
            {
                return true;
            }
            nodeIndexes[^1] = -1; // need reference to last node's parent
            ref var lastParent = ref GetNode(ref nodeIndexes);
            var n = nodeIndexes.Length - 1;
            lastParent.RemainingDepth = -1; // force recalculation
            if (Delete(ref lastParent.Children, fragment[n]))
            {
                if (lastParent.Children.Length == 0) // delete and trim parent
                {
                    Array.Resize(ref nodeIndexes, n);
                    return deleteAndTrim();
                }
            }
            return false;
        }

        Array.Fill(nodeIndexes, -1);
        for (var i = 0; i < fragment.Length; i++)
        {
            var curIdx = Search(ref node.Children, 0, node.Children.Length - 1, fragment[i]);
            if (curIdx < 0)
            {
                return false;
            }
            nodeIndexes[i] = curIdx;
            node = node.Children[curIdx];
        }
        if (isPrefix)
        {
            return deleteAndTrim();
        }
        else
        {
            ref var lastNode = ref GetNode(ref nodeIndexes);
            if (lastNode.Children.Length == 0)
            {
                return deleteAndTrim();
            }
            else
            {
                lastNode.IsWord = false;
                return true;
            }
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
                await writer.WriteAsync(JsonSerializer.Serialize(this, options ?? mSerializerOptions)).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
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
            trie = [];
            try
            {
                using var reader = fi.OpenText();
                var curLine = await reader.ReadLineAsync().ConfigureAwait(false);
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
                    curLine = await reader.ReadLineAsync().ConfigureAwait(false);
                }
#if DEBUG
                Debug.WriteLine("Lines read: {0} | Lines added: {1}.", numLinesRead, numLinesAdded);
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
    /// Creates a <see cref="Trie"/> from the given json file asynchronously.
    /// </summary>
    /// <param name="fi">The <see cref="FileInfo"/></param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use</param>
    /// <returns>A <see cref="Collections.Trie?"/></returns>
    /// <exception cref="InvalidOperationException">The file could not be opened and read as a text file or its contents could not be parsed into a <see cref="Collections.Trie"/>.</exception>
    public static async Task<Trie?> LoadAsync(FileInfo fi, JsonSerializerOptions? options = null)
    {
        Trie? trie = null;

        if (fi != null && fi.Exists)
        {
            try
            {
                using var stream = fi.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
                trie = await JsonSerializer.DeserializeAsync<Trie>(stream, options ?? mSerializerOptions).ConfigureAwait(false);
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
    /// Adds the given character as a <see cref="Node"/> to the given parent <see cref="Node"/>'s <see cref="Node.Children"/> collection and returns its index in the collection.
    /// If a <see cref="Node"/> representing this character already exists, just its index is returned.
    /// </summary>
    /// <param name="character">The character</param>
    /// <param name="parentNode">The parent <see cref="Node"/></param>
    /// <param name="isWord">Determines whether the character finishes a word</param>
    /// <param name="value">The value, represented by this character</param>
    /// <returns>The index of the <see cref="Node"/> in the parent's <see cref="Node.Children"/> collection</returns>
    private static int AddNode(char character, ref Node parentNode, bool isWord, string? value)
    {
        int idx;
        if (parentNode.Children.Length == 0)
        {
            parentNode.Children = [new Node(character, isWord, isWord? value : null)];
            idx = parentNode.Children.Length - 1;
        }
        else
        {
            idx = Search(ref parentNode.Children, 0, parentNode.Children.Length - 1, character);
            if (idx < 0)
            {
                parentNode.Children = [.. parentNode.Children, new Node()];
                idx = Insert(ref parentNode.Children, new Node(character, isWord, isWord ? value : null));
            }
        }
        return idx;
    }

    /// <summary>
    /// Returns an <see cref="IEnumerator"/> that allows for enumerating all <see cref="Node"/>s in this <see cref="Trie"/>.
    /// </summary>
    /// <returns>An <see cref="IEnumerator"/></returns>
    IEnumerator<Node> IEnumerable<Node>.GetEnumerator() => AsEnumerable().GetEnumerator();

    /// <summary>
    /// Returns the <see cref="Node"/> represented by the given array of indexes.
    /// </summary>
    /// <param name="nodeIndexes">The array of indexes</param>
    /// <returns>The <see cref="Node"/></returns>
    private ref Node GetNode(ref int[] nodeIndexes)
    {
        return ref GetNode(ref mRoot, ref nodeIndexes, 0);
    }

    /// <summary>
    /// Recursive function that walks through the <see cref="Trie"/> by index and returns the resulting <see cref="Node"/> by reference.
    /// An index of <code>-1</code> returns the current <see cref="Node"/>.
    /// </summary>
    /// <param name="curNode">The current <see cref="Node"/> to walk</param>
    /// <param name="nodeIndexes">The array of indexes</param>
    /// <param name="curDepth">The depth of the walk down the <see cref="Trie"/> so far</param>
    /// <returns>The <see cref="Node"/> represented by the index array</returns>
    private static ref Node GetNode(ref Node curNode, ref int[] nodeIndexes, int curDepth)
    {
        if (nodeIndexes.Length == curDepth || nodeIndexes[curDepth] == -1)
        {
            return ref curNode;
        }
        return ref GetNode(ref curNode.Children[nodeIndexes[curDepth]], ref nodeIndexes, ++curDepth);
    }

    /// <summary>
    /// Returns the first word that carries the given <see cref="Node.Value"/>, starting from the given node.
    /// </summary>
    /// <param name="chars">The <see cref="List{char}"/> to append characters to</param>
    /// <param name="node">The <see cref="Node"/> from which to start</param>
    /// <param name="value">The value> of which to find the word</param>
    private static bool GetWord(List<char> chars, Node node, string value)
    {
        foreach (var item in node.Children)
        {
            var childNode = item;

            if (childNode.Value == value)
            {
                chars.Add(item.Character);
                return true;
            }
            else
            {
                if (GetWord(chars, childNode, value))
                {
                    chars.Add(item.Character);
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Walks depth-first through the tree starting at the given node and returns every <see cref="Node"/> that is encountered.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{Node}"/></returns>
    internal static List<Node> Walk(NodeReference nodeRef)
    {
        var nodes = new List<Node>([nodeRef.Node]);

        for (var i = 0; i < nodeRef.Node.Children.Length; i++)
        {
            foreach (var c in Walk(new NodeReference(ref nodeRef.Node.Children[i])))
            {
               nodes.Add(c);
            }
        }
        return nodes;
    }

    /// <summary>
    /// Gets all the words recursively, starting from the given <see cref="Node"/>.
    /// </summary>
    /// <param name="node">The <see cref="Node"/> to start from</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    private static List<string> Walk(NodeReference nodeRef, StringBuilder buffer)
    {
        var words = new List<string>();

        if (nodeRef.Node.IsWord)
        {
            words.Add(buffer.ToString());
        }
        for (var i = 0; i < nodeRef.Node.Children.Length; i++)
        {
            buffer.Append(nodeRef.Node.Children[i].Character);
            foreach (var word in Walk(new NodeReference(ref nodeRef.Node.Children[i]), buffer))
            {
                words.Add(word);
            }
            buffer.Length--;
        }
        return words;
    }

    /// <summary>
    /// Tries to retrieve all words that match the given <see cref="PatternMatch"/> starting from the given <see cref="Node"/>.
    /// </summary>
    /// <param name="node">The <see cref="Node"/> to start from</param>
    /// <param name="pattern">The <see cref="PatternMatch"/> to use</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    /// <returns>An <see cref="IEnumerable{string}"/></returns>
    private static List<string> Walk(NodeReference nodeRef, PatternMatch pattern, StringBuilder buffer, int length, int matchCount)
    {
        var words = new List<string>();

        if (matchCount == pattern.Count) // all words in this subtree are a match for prefix pattern types, and for word pattern type if word length matches as well
        {
            foreach (var word in Walk(nodeRef, buffer))
            {
                if (pattern.Type != PatternMatchType.IsWord || word.Length == length)
                {
                    words.Add(word);
                }
            }
        }
        else if (nodeRef.Node.RemainingDepth >= pattern.Count - matchCount) // words are available that may be matched to the (remaining) pattern
        {
            var curMatch = pattern[matchCount];

            for (var i = 0; i < nodeRef.Node.Children.Length; i++)
            {
                buffer.Append(nodeRef.Node.Children[i].Character);
                if (curMatch.IsMatch(nodeRef.Node.Children[i].Character)) // keep matching
                {
                    foreach (var word in Walk(new NodeReference(ref nodeRef.Node.Children[i]), pattern, buffer, length, matchCount + 1))
                    {
                        words.Add(word);
                    }
                }
                buffer.Length--;
            }
        }
        return words;
    }

    /// <summary>
    /// Retrieves all words that match the given <see cref="PatternMatch"/> starting from the given <see cref="Node"/>.
    /// </summary>
    /// <param name="node">The <see cref="NodeReference"/> to the <see cref="Node"/> to start from</param>
    /// <param name="pattern">The <see cref="PatternMatch"/> to use</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    /// <param name="matchCount">The number of positive matches sofar</param>
    /// <returns>An <see cref="IEnumerable{string}"/></returns>
    private static List<string> WalkContaining(ref NodeReference nodeRef, PatternMatch pattern, StringBuilder buffer, int matchCount)
    {
        var words = new List<string>();

        if (!nodeRef.Visited)
        {
            if (nodeRef.Node.RemainingDepth >= pattern.Count)
            {
                var curMatch = pattern[0];
                for (var i = 0; i < nodeRef.Node.Children.Length; i++)
                {
                    var childRef = new NodeReference(ref nodeRef.Node.Children[i], curMatch.Primary == null);

                    buffer.Append(childRef.Node.Character);
                    if (curMatch.IsMatch(childRef.Node.Character)) // char is a match; try to match remaining strings to remaining pattern if possible
                    {
                        foreach (var word in WalkWithRetry(ref childRef, ref childRef, pattern, buffer, matchCount + 1)) // keep matching
                        {
                            words.Add(word);
                        }
                    }
                    else // look deeper in the branch
                    {
                        foreach (var word in WalkContaining(ref childRef, pattern, buffer, 0))
                        {
                            words.Add(word);
                        }
                    }
                    buffer.Length--;
                }
            }
            nodeRef.Visited = true;
        }
        return words;
    }

    /// <summary>
    /// Tries to retrieve all words that match the given <see cref="PatternMatch"/> starting from the given <see cref="Trie.Node"/>.
    /// When a 'chain' of matchings breaks, matching will start over from the children of the node that was the first match.
    /// </summary>
    /// <param name="node">The <see cref="NodeReference"/> to the <see cref="Node"/> to start from</param>
    /// <param name="firstMatch">The <see cref="NodeReference"/> to the <see cref="Node"/> that represents the first matching character</param>
    /// <param name="pattern">The <see cref="PatternMatch"/> to use</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    /// <param name="matchCount">The number of positive matches sofar</param>
    /// <returns>An <see cref="IEnumerable{string}"/></returns>
    private static List<string> WalkWithRetry(ref NodeReference nodeRef, ref NodeReference firstMatch, PatternMatch pattern, StringBuilder buffer, int matchCount)
    {
        var words = new List<string>();

        if (!nodeRef.Visited)
        {
            if (matchCount == pattern.Count) // all words in this subtree are a match
            {
                foreach (var word in Walk(nodeRef, buffer))
                {
                    words.Add(word);
                }
            }
            else if (nodeRef.Node.RemainingDepth >= pattern.Count - matchCount) // words are available that may be matched to the (remaining) pattern
            {
                var curMatch = pattern[matchCount];

                for (var i = 0; i < nodeRef.Node.Children.Length; i++)
                {
                    if (!nodeRef.Visited)
                    {
                        var childRef = new NodeReference(ref nodeRef.Node.Children[i], curMatch.Primary == null);

                        buffer.Append(childRef.Node.Character);
                        if (curMatch.IsMatch(childRef.Node.Character)) // keep matching
                        {
                            foreach (var word in WalkWithRetry(ref childRef, ref firstMatch, pattern, buffer, matchCount + 1))
                            {
                                words.Add(word);
                            }

                        }
                        else // start over from the children of the first matching node
                        {
                            if (!firstMatch.Visited)
                            {
                                var retryBuffer = new StringBuilder(buffer.ToString()[..(buffer.Length - matchCount)]);

                                foreach (var word in WalkContaining(ref firstMatch, pattern, retryBuffer, 0))
                                {
                                    words.Add(word);
                                }
                                firstMatch.Visited = true;
                            }
                        }
                        buffer.Length--;
                    }
                }
            }
            nodeRef.Visited = true;
        }
        return words;
    }

    /// <summary>
    /// Gets all the values recursively starting from the given <see cref="Trie.Node"/>.
    /// </summary>
    /// <param name="curDepth">The depth of the current node</param>
    /// <param name="length">The length of the word</param>
    private static List<string?> Walk(NodeReference nodeRef, int curDepth, int length)
    {
        var values = new List<string?>();

        if (nodeRef.Node.IsWord && (length == 0 || length == curDepth))
        {
            values.Add(nodeRef.Node.Value);
        }
        for (var i = 0; i < nodeRef.Node.Children.Length; i++)
        {
            foreach (var value in Walk(new NodeReference(ref nodeRef.Node.Children[i]), ++curDepth, length))
            {
                values.Add(value);
            }
        }
        return values;
    }

    /// <summary>
    /// Retrieves all values that match the given <see cref="PatternMatch"/> starting from the given <see cref="Node"/>.
    /// </summary>
    /// <param name="node">The <see cref="Node"/> to start from</param>
    /// <param name="pattern">The <see cref="PatternMatch"/> to match</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re-)use</param>
    /// <returns>An <see cref="IEnumerable{double?}"/></returns>
    private static List<string?> WalkValues(NodeReference nodeRef, PatternMatch pattern, StringBuilder buffer, int curDepth, int length, int matchCount)
    {
        var values = new List<string?>();

        if (matchCount == pattern.Count) // all words in this subtree are a match for prefix pattern types, and for word pattern type when word length matches as well
        {
            foreach (var value in Walk(nodeRef, curDepth, length))
            {
                if (pattern.Type != PatternMatchType.IsWord || curDepth == length)
                {
                    values.Add(value);
                }
            }
        }
        else if (nodeRef.Node.RemainingDepth >= pattern.Count - matchCount) // words are available that may be matched to the (remaining) pattern
        {
            var curMatch = pattern[matchCount];

            for (var i = 0; i < nodeRef.Node.Children.Length; i++)
            {
                var childRef = new NodeReference(ref nodeRef.Node.Children[i]);

                buffer.Append(childRef.Node.Character);
                if (curMatch.IsMatch(childRef.Node.Character)) // keep matching
                {
                    foreach (var value in WalkValues(childRef, pattern, buffer, curDepth + 1, length, matchCount + 1))
                    {
                        values.Add(value);
                    }
                }
                buffer.Length--;
            }
        }
        return values;
    }

    /// <summary>
    /// Tries to retrieve all values that match the given <see cref="PatternMatch"/> starting from the given <see cref="Node"/>.
    /// </summary>
    /// <param name="node">The <see cref="Node"/> to start from</param>
    /// <param name="pattern">The <see cref="PatternMatch"/> to match</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re-)use</param>
    /// <returns>An <see cref="IEnumerable{double?}"/></returns>
    private static List<string?> WalkValuesContaining(ref NodeReference nodeRef, PatternMatch pattern, StringBuilder buffer, int curDepth, int length, int matchCount)
    {
        var values = new List<string?>();

        if (!nodeRef.Visited)
        {
            nodeRef.Visited = true;
            if (nodeRef.Node.RemainingDepth >= pattern.Count)
            {
                var curMatch = pattern[0];

                nodeRef.Visited = true;
                for (var i = 0; i < nodeRef.Node.Children.Length; i++)
                {
                    var childRef = new NodeReference(ref nodeRef.Node.Children[i], curMatch.Primary == null);

                    buffer.Append(childRef.Node.Character);
                    if (curMatch.IsMatch(childRef.Node.Character)) // char is a match; try to match remaining strings to remaining pattern if possible
                    {
                        foreach (var value in WalkValuesWithRetry(ref childRef, ref childRef, pattern, buffer, curDepth + 1, curDepth + 1, length, matchCount + 1))
                        {
                            values.Add(value);
                        }
                    }
                    else // look deeper in the branch
                    {
                        foreach (var value in WalkValuesContaining(ref childRef, pattern, buffer, curDepth + 1, length, 0)) // look further
                        {
                            values.Add(value);
                        }
                    }
                    buffer.Length--;
                }
            }
        }
        return values;
    }

    /// <summary>
    /// Tries to retrieve all values that match the given <see cref="PatternMatch"/> starting from the given <see cref="Node"/>.
    /// When a 'chain' of matchings breaks, matching will start over from the children of the node that was the first match.
    /// </summary>
    /// <param name="node">The <see cref="Node"/> to start from</param>
    /// <param name="firstMatch">The <see cref="NodeReference"/> that represents the first matching character</param>
    /// <param name="pattern">The <see cref="PatternMatch"/> to match</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re-)use</param>
    /// <returns>An <see cref="IEnumerable{double?}"/></returns>
    private static List<string?> WalkValuesWithRetry(ref NodeReference nodeRef, ref NodeReference firstMatch, PatternMatch pattern, StringBuilder buffer, int curDepth, int originalDepth, int length, int matchCount)
    {
        var values = new List<string?>();

        if (!nodeRef.Visited)
        {
            if (matchCount == pattern.Count) // all words in this subtree are a match for prefix pattern types, and for word pattern type when word length matches as well
            {
                foreach (var value in Walk(nodeRef, curDepth, length))
                {
                    values.Add(value);
                }
            }
            else if (nodeRef.Node.RemainingDepth >= pattern.Count - matchCount) // words are available that may be matched to the (remaining) pattern
            {
                var curMatch = pattern[matchCount];

                for (var i = 0; i < nodeRef.Node.Children.Length; i++)
                {
                    var childRef = new NodeReference(ref nodeRef.Node.Children[i], curMatch.Primary == null);

                    buffer.Append(childRef.Node.Character);
                    if (curMatch.IsMatch(childRef.Node.Character)) // keep matching
                    {
                        foreach (var value in WalkValuesWithRetry(ref childRef, ref firstMatch, pattern, buffer, curDepth + 1, originalDepth, length, matchCount + 1))
                        {
                            values.Add(value);
                        }
                    }
                    else // start over from the children of the first matching node
                    {
                        if (!firstMatch.Visited)
                        {
                            var retryBuffer = new StringBuilder(buffer.ToString()[..(buffer.Length - matchCount)]);
                            foreach (var value in WalkValuesContaining(ref firstMatch, pattern, retryBuffer, originalDepth, length, 0))
                            {
                                values.Add(value);
                            }
                        }
                    }
                    buffer.Length--;
                }
            }
        }
        if (!firstMatch.IsWildCard)
        {
            firstMatch.Visited = true;
        }
        return values;
    }

    #region Array

    /// <summary>
    /// Removes the given character from the given array of <see cref="Node"/>s and resizes the array without affecting the sort.
    /// If the character was removed, <see langword="true"/> is returned, else <see langword="false"/>.
    /// </summary>
    /// <param name="nodes">The array of <see cref="Node"/>s</param>
    /// <param name="character">The character to remove</param>
    private static bool Delete(ref Node[] nodes, char character)
    {
        var n = nodes.Length - 1;
        var pos = Search(ref nodes, 0, n, character);

        if (pos >= 0)
        {
            if (n > pos)
            {
                for (var i = pos; i < n; i++)
                {
                    nodes[i] = nodes[i + 1];
                }
            }
            Array.Resize(ref nodes, n);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Inserts the given <see cref="Node"/> at the correct position in the given Array and returns its index.
    /// </summary>
    /// <param name="nodes">The <see cref="Node[]"/> in which to insert the node</param>
    /// <param name="n">The length of the array</param>
    /// <param name="node">The <see cref="Node"/></param>
    /// <returns>The index of the inserted <see cref="Node"/></returns>
    private static int Insert(ref Node[] nodes, Node node)
    {
        var n = nodes.Length - 1; int i;
        for (i = n - 1; i >= 0 && nodes[i].Character > node.Character; i--)
        {
            nodes[i + 1] = nodes[i];
        }
        nodes[i + 1] = node;
        return i + 1;
    }

    /// <summary>
    /// Returns the index of the <see cref="Node"/> representing the given character in the given array of <see cref="Node"/>s if it exists. If it doesn't, -1 is returned.
    /// </summary>
    /// <param name="nodes">The <see cref="Node"/> array in which to search</param>
    /// <param name="low">The lower bound of the search range</param>
    /// <param name="high">The upper bound of the search range</param>
    /// <param name="character">The character for which to find the <see cref="Node"/></param>
    /// <returns>The index of the node that represents this character if it exists, and if not, -1 </returns>
    private static int Search(ref Node[] nodes, int low, int high, char character)
    {
        if (high < low)
        {
            return -1;
        }
        var mid = (low + high) / 2;
        if (character == nodes[mid].Character)
        {
            return mid;
        }
        if (character > nodes[mid].Character)
        {
            return Search(ref nodes, (mid + 1), high, character);
        }
        return Search(ref nodes, low, (mid - 1), character);
    }

    #endregion

    #endregion
}