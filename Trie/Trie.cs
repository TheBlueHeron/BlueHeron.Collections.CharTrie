using System.Collections;
using System.Diagnostics;
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
public sealed partial class Trie : IEnumerable, IEnumerable<TrieNode>
{
    #region Fields

    private TrieNode mRoot;

    private const string _Export = "export";
    private const string _Import = "import";

    private static readonly CompositeFormat errImpEx = CompositeFormat.Parse("Unable to {0} '{1}'. See inner exception for details.");
    private static readonly JsonSerializerOptions mSerializerOptions = new() { WriteIndented = false };

    #endregion

    #region Construction

    /// <summary>
    /// Creates a new <see cref="Trie"/>.
    /// </summary>
    public Trie()
    {
        mRoot = TrieNode.Create(char.MaxValue, false);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the root <see cref="TrieNode"/>.
    /// </summary>
    public TrieNode RootNode
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
    public void Add(string word)
    {
        ArgumentException.ThrowIfNullOrEmpty(word);
        var nodeIndexes = new int[word.Length];
        Array.Fill(nodeIndexes, -1);

        for (var i = 0; i < word.Length; i++)
        {
            var curChar = word[i];
            var isWord = i == word.Length - 1;

            nodeIndexes[i] = AddNode(curChar, ref GetNode(ref nodeIndexes),  isWord);
        }
    }

    /// <summary>
    /// Walks depth-first through the tree and returns every <see cref="TrieNode"/> that is encountered, accompanied with its key.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{Node}"/></returns>
    public IEnumerable<TrieNode> AsEnumerable()
    {
        return Walk(mRoot);
    }

    /// <summary>
    /// Tries to find the given <see cref="string"/> and returns <see langword="true"/> if there is a match.
    /// </summary>
    /// <param name="word">The word to find</param>
    /// <param name="isPrefix">If <see langword="true"/> return <see langword="true"/> if words starting with the given word exist, else only return <see langword="true"/> if an exact match is present</param>
    /// <returns>Boolean, <see langword="true"/> if the word exists in this <see cref="Trie"/></returns>
    public bool Contains(string word, bool isPrefix)
    {
        ArgumentException.ThrowIfNullOrEmpty(word, nameof(word));
        var node = mRoot;

        foreach (var c in word)
        {
            var childIndex = Search(ref node.Children,0, node.Children.Length - 1, c);
            if (childIndex < 0)
            {
                return false;
            }
            node = node.Children[childIndex];
        }
        return isPrefix || node.IsWord;
    }

    /// <summary>
    /// Gets all words that match the given fragment.
    /// </summary>
    /// <param name="fragment">The <see cref="string"/> to match; if <see langword="null"/>: all words are returned</param>
    /// <param name="isPrefix">If <see langword="true"/>, the word should start with this fragment; else the word should contain this fragment</param>
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
        var node = mRoot;

        node.IsVisited = false;
        if (pattern.Type == PatternMatchType.IsFragment)
        {
            foreach (var word in WalkContaining(ref node, pattern, new StringBuilder(), 0))
            {
                yield return word;
            }
        }
        else
        {
            foreach (var word in Walk(node, pattern, new StringBuilder(), pattern.Type == PatternMatchType.IsWord ? pattern.Count : 0, 0))
            {
                yield return word;
            }
        }
    }

    /// <summary>
    /// Returns an <see cref="IEnumerator{TrieNode}"/> that allows for enumerating all <see cref="TrieNode"/>s in this <see cref="Trie"/>.
    /// </summary>
    /// <returns>An <see cref="IEnumerator{TrieNode}"/></returns>
    public IEnumerator GetEnumerator() => AsEnumerable().GetEnumerator();

    /// <summary>
    /// Returns the number of <see cref="TrieNode"/>s in this <see cref="Trie"/>.
    /// </summary>
    public int NumNodes() => mRoot.NumNodes();

    /// <summary>
    /// Returns the number of words represented by this <see cref="Trie"/>.
    /// </summary>
    public int NumWords() => mRoot.NumWords();

    /// <summary>
    /// Removes all words matching the given prefix from the <see cref="Trie"/>.
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
    /// <returns>A Task, resulting in a <see cref="bool"/> that signifies the success of the operation</returns>
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
    /// <returns>A Task, resulting in a <see cref="Trie"/> if successful; else <see langword="null"/></returns>
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
#if DEBUG
                var numLinesRead = 0;
                var numLinesAdded = 0;
#endif
                while (curLine != null)
                {
#if DEBUG
                    numLinesRead++;
#endif
                    if (curLine.Length > 0)
                    {
                        trie.Add(curLine.Trim());
#if DEBUG
                        numLinesAdded++;
#endif
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
    /// <returns>A Task, resulting in a <see cref="Trie"/> if successful; else <see langword="null"/></returns>
    /// <exception cref="InvalidOperationException">The file could not be opened and read as a text file or its contents could not be parsed into a <see cref="Trie"/>.</exception>
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
    /// Adds the given character as a <see cref="TrieNode"/> to the given parent <see cref="TrieNode"/>'s <see cref="TrieNode.Children"/> collection and returns its index in the collection.
    /// If a <see cref="TrieNode"/> representing this character already exists, just its index is returned.
    /// </summary>
    /// <param name="character">The character</param>
    /// <param name="parentNode">The parent <see cref="TrieNode"/></param>
    /// <param name="isWord">Determines whether the character finishes a word</param>
    /// <returns>The index of the <see cref="TrieNode"/> in the parent's <see cref="TrieNode.Children"/> collection</returns>
    private static int AddNode(char character, ref TrieNode parentNode, bool isWord)
    {
        int idx;
        if (parentNode.Children.Length == 0)
        {
            parentNode.Children = [TrieNode.Create(character, isWord)];
            idx = parentNode.Children.Length - 1;
        }
        else
        {
            idx = Search(ref parentNode.Children, 0, parentNode.Children.Length - 1, character);
            if (idx < 0)
            {
                parentNode.Children = [.. parentNode.Children, new TrieNode()];
                idx = Insert(ref parentNode.Children, TrieNode.Create(character, isWord));
            }
        }
        return idx;
    }

    /// <summary>
    /// Returns an <see cref="IEnumerator"/> that allows for enumerating all <see cref="TrieNode"/>s in this <see cref="Trie"/>.
    /// </summary>
    /// <returns>An <see cref="IEnumerator"/></returns>
    IEnumerator<TrieNode> IEnumerable<TrieNode>.GetEnumerator() => AsEnumerable().GetEnumerator();

    /// <summary>
    /// Returns the <see cref="TrieNode"/> represented by the given array of indexes.
    /// </summary>
    /// <param name="nodeIndexes">The array of indexes</param>
    /// <returns>The <see cref="TrieNode"/></returns>
    private ref TrieNode GetNode(ref int[] nodeIndexes)
    {
        return ref GetNode(ref mRoot, ref nodeIndexes, 0);
    }

    /// <summary>
    /// Recursive function that walks through the <see cref="Trie"/> by index and returns the resulting <see cref="TrieNode"/> by reference.
    /// An index of <code>-1</code> returns the current <see cref="TrieNode"/>.
    /// </summary>
    /// <param name="curNode">The current <see cref="TrieNode"/> to walk</param>
    /// <param name="nodeIndexes">The array of indexes</param>
    /// <param name="curDepth">The depth of the walk down the <see cref="Trie"/> so far</param>
    /// <returns>The <see cref="TrieNode"/> represented by the index array</returns>
    private static ref TrieNode GetNode(ref TrieNode curNode, ref int[] nodeIndexes, int curDepth)
    {
        if (nodeIndexes.Length == curDepth || nodeIndexes[curDepth] == -1)
        {
            return ref curNode;
        }
        return ref GetNode(ref curNode.Children[nodeIndexes[curDepth]], ref nodeIndexes, ++curDepth);
    }

    /// <summary>
    /// Walks depth-first through the tree starting at the given <see cref="TrieNode"/> and returns every <see cref="TrieNode"/> that is encountered.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{TrieNode}"/></returns>
    internal static List<TrieNode> Walk(TrieNode node)
    {
        var nodes = new List<TrieNode>([node]);

        for (var i = 0; i < node.Children.Length; i++)
        {
            foreach (var c in Walk(node.Children[i]))
            {
               nodes.Add(c);
            }
        }
        return nodes;
    }

    /// <summary>
    /// Gets all the words recursively, starting from the given <see cref="TrieNode"/>.
    /// </summary>
    /// <param name="node">The <see cref="TrieNode"/> to start from</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    private static List<string> Walk(TrieNode node, StringBuilder buffer)
    {
        var words = new List<string>();

        if (node.IsWord)
        {
            words.Add(buffer.ToString());
        }
        for (var i = 0; i < node.Children.Length; i++)
        {
            buffer.Append(node.Children[i].Character);
            foreach (var word in Walk(node.Children[i], buffer))
            {
                words.Add(word);
            }
            buffer.Length--;
        }
        return words;
    }

    /// <summary>
    /// Tries to retrieve all words that match the given <see cref="PatternMatch"/> starting from the given <see cref="TrieNode"/>.
    /// </summary>
    /// <param name="node">The <see cref="TrieNode"/> to start from</param>
    /// <param name="pattern">The <see cref="PatternMatch"/> to use</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    /// <returns>An <see cref="IEnumerable{string}"/></returns>
    private static List<string> Walk(TrieNode node, PatternMatch pattern, StringBuilder buffer, int length, int matchCount)
    {
        var words = new List<string>();

        if (matchCount == pattern.Count) // all words in this subtree are a match for prefix pattern types, and for word pattern type if word length matches as well
        {
            foreach (var word in Walk(node, buffer))
            {
                if (pattern.Type != PatternMatchType.IsWord || word.Length == length)
                {
                    words.Add(word);
                }
            }
        }
        else if (node.RemainingDepth >= pattern.Count - matchCount) // words may be available that can be matched to the (remaining) pattern
        {
            var curMatch = pattern[matchCount];

            for (var i = 0; i < node.Children.Length; i++)
            {
                buffer.Append(node.Children[i].Character);
                if (curMatch.IsMatch(node.Children[i].Character)) // keep matching
                {
                    foreach (var word in Walk(node.Children[i], pattern, buffer, length, matchCount + 1))
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
    /// Retrieves all words that match the given <see cref="PatternMatch"/> starting from the given <see cref="TrieNode"/>.
    /// </summary>
    /// <param name="node">The <see cref="TrieNode"/> to start from</param>
    /// <param name="pattern">The <see cref="PatternMatch"/> to use</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    /// <param name="matchCount">The number of positive matches sofar</param>
    /// <returns>An <see cref="IEnumerable{string}"/></returns>
    private static List<string> WalkContaining(ref TrieNode node, PatternMatch pattern, StringBuilder buffer, int matchCount, bool isRetry = false)
    {
        var words = new List<string>();

        if (!node.IsVisited)
        {
            if (node.RemainingDepth >= pattern.Count)
            {
                var curMatch = pattern[0];
                for (var i = 0; i < node.Children.Length; i++)
                {
                    ref var child = ref node.Children[i];

                    if (!isRetry)
                    {
                        child.IsVisited = false;
                    }
                    if (!child.IsVisited)
                    {
                        buffer.Append(child.Character);
                        if (curMatch.IsMatch(child.Character)) // char is a match; try to match remaining strings to remaining pattern if possible
                        {
                            foreach (var word in WalkWithRetry(ref child, ref child, pattern, buffer, matchCount + 1)) // keep matching
                            {
                                words.Add(word);
                            }
                        }
                        else // look deeper in the branch
                        {
                            foreach (var word in WalkContaining(ref child, pattern, buffer, 0))
                            {
                                words.Add(word);
                            }
                        }
                        buffer.Length--;
                    }
                }
            }
            node.IsVisited = true;
        }
        return words;
    }

    /// <summary>
    /// Tries to retrieve all words that match the given <see cref="PatternMatch"/> starting from the given <see cref="TrieNode"/>.
    /// When a 'chain' of matchings breaks, matching will start over from the children of the node that was the first match.
    /// </summary>
    /// <param name="node">The <see cref="TrieNode"/> to start from</param>
    /// <param name="firstMatch">The <see cref="TrieNode"/> that represents the first matching character</param>
    /// <param name="pattern">The <see cref="PatternMatch"/> to use</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    /// <param name="matchCount">The number of positive matches sofar</param>
    /// <returns>An <see cref="IEnumerable{string}"/></returns>
    private static List<string> WalkWithRetry(ref TrieNode node, ref TrieNode firstMatch, PatternMatch pattern, StringBuilder buffer, int matchCount)
    {
        var words = new List<string>();

        if (!node.IsVisited)
        {
            if (matchCount == pattern.Count) // all words in this subtree are a match
            {
                foreach (var word in Walk(node, buffer))
                {
                    words.Add(word);
                }
            }
            else if (node.RemainingDepth >= pattern.Count - matchCount) // words are available that may be matched to the (remaining) pattern
            {
                var curMatch = pattern[matchCount];

                for (var i = 0; i < node.Children.Length; i++)
                {
                    ref var child = ref node.Children[i];

                    child.IsVisited = false;
                    buffer.Append(child.Character);
                    if (curMatch.IsMatch(child.Character)) // keep matching
                    {
                        foreach (var word in WalkWithRetry(ref child, ref firstMatch, pattern, buffer, matchCount + 1))
                        {
                            words.Add(word);
                        }
                    }
                    else // start over from the children of the first matching node
                    {
                        if (!firstMatch.IsVisited)
                        {
                            var retryBuffer = new StringBuilder(buffer.ToString()[..(buffer.Length - matchCount)]);

                            foreach (var word in WalkContaining(ref firstMatch, pattern, retryBuffer, 0, true))
                            {
                                words.Add(word);
                            }
                            firstMatch.IsVisited = true;
                        }
                    }
                    buffer.Length--;
                }
                node.IsVisited = true;
            }
        }
        return words;
    }

    #region Array

    /// <summary>
    /// Removes the given character from the given array of <see cref="TrieNode"/>s and resizes the array without affecting the sort.
    /// If the character was removed, <see langword="true"/> is returned, else <see langword="false"/>.
    /// </summary>
    /// <param name="nodes">The array of <see cref="TrieNode"/>s</param>
    /// <param name="character">The character to remove</param>
    private static bool Delete(ref TrieNode[] nodes, char character)
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
    /// Inserts the given <see cref="TrieNode"/> at the correct position in the given Array and returns its index.
    /// </summary>
    /// <param name="nodes">The <see cref="TrieNode[]"/> in which to insert the node</param>
    /// <param name="node">The <see cref="TrieNode"/></param>
    /// <returns>The index of the inserted <see cref="TrieNode"/></returns>
    private static int Insert(ref TrieNode[] nodes, TrieNode node)
    {
        var n = nodes.Length - 1;
        int i;
        for (i = n - 1; i >= 0 && nodes[i].Character > node.Character; i--)
        {
            nodes[i + 1] = nodes[i];
        }
        nodes[i + 1] = node;
        return i + 1;
    }

    /// <summary>
    /// Returns the index of the <see cref="TrieNode"/> representing the given character in the given array of <see cref="TrieNode"/>s if it exists. If it doesn't, -1 is returned.
    /// </summary>
    /// <param name="nodes">The <see cref="TrieNode"/> array in which to search</param>
    /// <param name="low">The lower bound of the search range</param>
    /// <param name="high">The upper bound of the search range</param>
    /// <param name="character">The character for which to find the <see cref="TrieNode"/></param>
    /// <returns>The index of the node that represents this character if it exists, else -1</returns>
    internal static int Search(ref TrieNode[] nodes, int low, int high, char character)
    {
        while (low <= high)
        {
            var mid = low + (high - low) / 2;
            var midChar = nodes[mid].Character;

            if (character == midChar)
            {
                return mid;
            }
            else if (character < midChar)
            {
                high = mid - 1;
            }
            else
            {
                low = mid + 1;
            }
        }
        return -1;
    }

    #endregion

    #endregion
}