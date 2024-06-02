using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using BlueHeron.Collections.Trie.Search;
using System.Text;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// A search optimized data structure for words.
/// </summary>
[SuppressMessage("Performance", "CA1710:Rename to end with Collection or Dictionary", Justification = "Nomen est omen.")]
public class Trie2 : IEnumerable, IEnumerable<Trie2.Node>
{
    #region Fields

    private Node mRoot;

    #endregion

    #region Nodes

    /// <summary>
    /// A node in the <see cref="Trie2"/>, which represents a character.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct Node: IComparable<Node>, IEquatable<Node>
    {
        private int mRemainingDepth;

        /// <summary>
        /// The character. Is only <see langword="null"/> on the root node.
        /// </summary>
        public readonly char? Character;
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
        public double? Value;

        #endregion

        #region Construction

        /// <summary>
        /// Creates a new, empty <see cref="Node"/>.
        /// </summary>
        public Node()
        {
            Children = [];
            mRemainingDepth = -1;
        }

        /// <summary>
        /// Creates a new <see cref="Node"/>.
        /// </summary>
        /// <param name="character">The character</param>
        /// <param name="isWord">Determines whether this <see cref="Node"/> finishes a word</param>
        /// <param name="value">The value, represented by this <see cref="Node"/></param>
        public Node(char? character, bool isWord, double? value) : this()
        {
            Character = character;
            IsWord = isWord;
            Value = value;
        }

        #endregion

        #region Public methods and functions

        /// <summary>
        /// Returns the child <see cref="Node"/> that represent the given <see cref="char"/> if it exists, else <see langword="null"/>.
        /// </summary>
        /// <param name="character">The <see cref="char"/> to match</param>
        /// <returns>The <see cref="Node"/> representing the given <see cref="char"/> if it exists; else <see langword="null"/></returns>
        public readonly Node? GetNode(char character)
        {
            var idx = Search(Children, 0, Children.Length - 1, character);
            return idx < 0 ? null : Children[idx];
        }

        /// <summary>
        /// Returns the child <see cref="Node"/> that represent the given prefix if it exists, else <see langword="null"/>.
        /// </summary>
        /// <param name="character">The <see cref="string"/> prefix to match</param>
        /// <returns>The <see cref="Node"/> representing the given <see cref="string"/> if it exists; else <see langword="null"/></returns>
        public readonly Node? GetNode(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                throw new ArgumentNullException(nameof(prefix));
            }
            var node = this;
            foreach (var prefixChar in prefix)
            {
                var child = node.GetNode(prefixChar);
                if (child == null)
                {
                    return null;
                }
                node = (Node)child;
            }
            return node;
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
        /// If <paramref name="other"/>.Character is null, 1 is returned.
        /// </summary>
        /// <param name="other">The <see cref="Node"/> to compare</param>
        public readonly int CompareTo(Node other) => Character.HasValue? Character.Value.CompareTo(other.Character) : 1;

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj) => obj is Node node && Equals(node);

        /// <inheritdoc/>
        public readonly override int GetHashCode() => Character.GetHashCode();

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="other">An object to compare with this instance or null</param>
        /// <returns><see cref="bool"/>, <see langword="true"/> if the specified object is equal to this instance</returns>
        public readonly bool Equals(Node other) => Character.HasValue && Character.Value.Equals(other.Character);

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

    #region Construction

    /// <summary>
    /// Creates a new <see cref="Trie2"/>.
    /// </summary>
    public Trie2()
    {
        mRoot = new Node();
    }

    #endregion

    #region Public methods and functions

    /// <summary>
    /// Adds the given word to the collection.
    /// </summary>
    /// <param name="word">The word to add</param>
    /// <param name="value">Option value to assign</param>
    public void Add(string word, double? value = null)
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
        return Walk(mRoot);
    }

    /// <summary>
    /// Tries to find the given <see cref="string"/> and returns <see langword="true"/> if there is a match.
    /// </summary>
    /// <param name="word">The word to find</param>
    /// <param name="isPrefix">If <see langword="true"/> return <see langword="true"/> if words starting with the given word exist, else only return <see langword="true"/> if an exact match is present</param>
    /// <returns>Boolean, <see langword="true"/> if the word exists in the <see cref="Trie"/></returns>
    public bool Contains(string word, bool isPrefix)
    {
        ArgumentException.ThrowIfNullOrEmpty(word, nameof(word));
        var node = mRoot;

        foreach (var c in word)
        {
            var childIndex = Search(node.Children,0, node.Children.Length - 1, c);
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
        if (pattern.Type == PatternMatchType.IsFragment)
        {
            foreach (var word in WalkContaining(mRoot, pattern, new StringBuilder(), 0, 0))
            {
                yield return word;
            }
        }
        else
        {
            foreach (var word in Walk(mRoot, pattern, new StringBuilder(), pattern.Type == PatternMatchType.IsWord ? pattern.Count : 0, 0))
            {
                yield return word;
            }
        }
    }

    /// <summary>
    /// Returns an <see cref="IEnumerator{Node}"/> that allows for enumerating all <see cref="Node"/>s in this <see cref="Trie2"/>.
    /// </summary>
    /// <returns>An <see cref="IEnumerator{Node}"/></returns>
    public IEnumerator GetEnumerator() => AsEnumerable().GetEnumerator();

    /// <summary>
    /// Gets the <see cref="Node"/> in this <see cref="Trie"/> that represents the given prefix, if it exists. Else <see langword="null"/> is returned.
    /// </summary>
    /// <param name="prefix">The <see cref="string"/> to match</param>
    /// <returns>A <see cref="Node"/> representing the given <see cref="string"/>, else <see langword="null"/></returns>
    public Node? GetNode(string prefix)
    {
        ArgumentException.ThrowIfNullOrEmpty(prefix);
        return mRoot.GetNode(prefix);
    }

    /// <summary>
    /// Returns the number of <see cref="Node"/>s in this <see cref="Trie"/>.
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
            var curIdx = Search(node.Children, 0, node.Children.Length - 1, fragment[i]);
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
    private static int AddNode(char character, ref Node parentNode, bool isWord, double? value)
    {
        int idx;
        if (parentNode.Children.Length == 0)
        {
            parentNode.Children = [new Node(character, isWord, value)];
            idx = parentNode.Children.Length - 1;
        }
        else
        {
            idx = Search(parentNode.Children, 0, parentNode.Children.Length - 1, character);
            if (idx < 0)
            {
                parentNode.Children = [.. parentNode.Children, new Node()];
                idx = Insert(ref parentNode.Children, new Node(character, isWord, value));
            }
        }
        return idx;
    }

    /// <summary>
    /// Gets the <see cref="Node"/>s that form the given string as a <see cref="Stack{Node}"/>.
    /// </summary>
    /// <param name="s">The <see cref="string"/> to match</param>
    /// <param name="isWord">The <paramref name="s"/> parameter is a word and not a prefix</param>
    /// <returns>A <see cref="Stack{Node}"/></returns>
    private Stack<Node> AsStack(string s, bool isWord = true)
    {
        var nodes = new Stack<Node>(s.Length + 1); // root node is included
        var node = mRoot;

        nodes.Push(node);
        foreach (var c in s)
        {
            var child = node.GetNode(c);

            if (child == null)
            {
                nodes.Clear();
                break;
            }
            nodes.Push(child.Value);
            node = child.Value;
        }
        if (isWord)
        {
            if (!node.IsWord)
            {
                throw new ArgumentOutOfRangeException(s);
            }
        }
        return nodes;
    }

    /// <summary>
    /// Returns an <see cref="IEnumerator"/> that allows for enumerating all <see cref="Node"/>s in this <see cref="Trie2"/>.
    /// </summary>
    /// <returns>An <see cref="IEnumerator"/></returns>
    IEnumerator<Node> IEnumerable<Node>.GetEnumerator() => throw new NotImplementedException();

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
    /// Recursive function that walks through the <see cref="Trie2"/> by index and returns the resulting <see cref="Node"/> by reference.
    /// An index of <code>-1</code> returns the current <see cref="Node"/>.
    /// </summary>
    /// <param name="curNode">The current <see cref="Node"/> to walk</param>
    /// <param name="nodeIndexes">The array of indexes</param>
    /// <param name="curDepth">The depth of the walk down the <see cref="Trie2"/> so far</param>
    /// <returns>The <see cref="Node"/> represented by the index array</returns>
    private static ref Node GetNode(ref Node curNode, ref int[] nodeIndexes, int curDepth)
    {
        if (nodeIndexes.Length == curDepth || nodeIndexes[curDepth] == -1)
        {
            return ref curNode;
        }
        return ref GetNode(ref curNode.Children[nodeIndexes[curDepth]], ref nodeIndexes, ++curDepth);
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
        var pos = Search(nodes, 0, n, character);

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
    private static int Search(Node[] nodes, int low, int high, char character)
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
            return Search(nodes, (mid + 1), high, character);
        }
        return Search(nodes, low, (mid - 1), character);
    }

    /// <summary>
    /// Walks depth-first through the tree starting at the given node and returns every <see cref="Node"/> that is encountered.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{Node}"/></returns>
    internal static IEnumerable<Node> Walk(Node node)
    {
        yield return node;
        foreach (var child in node.Children)
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
        foreach (var child in node.Children)
        {
            buffer.Append(child.Character);
            foreach (var word in Walk(child, buffer))
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
        if (matchCount == pattern.Count) // all words in this subtree are a match for prefix pattern types, and for word pattern type if word length matches as well
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

            foreach (var child in node.Children)
            {
                buffer.Append(child.Character);
                if (child.Character.HasValue && curMatch.IsMatch(child.Character.Value)) // keep matching
                {
                    foreach (var word in Walk(child, pattern, buffer, length, matchCount + 1))
                    {
                        yield return word;
                    }
                }
                buffer.Length--;
            }
        }
    }

    /// <summary>
    /// Tries to retrieve all words that match the given <see cref="PatternMatch"/> starting from the given <see cref="Node"/>.
    /// When a 'chain' of matchings breaks, matching will start over from the children of the node that was the first match.
    /// </summary>
    /// <param name="node">The <see cref="Node"/> to start from</param>
    /// <param name="firstMatch">The <see cref="RetryNode"/> that represents the first matching character</param>
    /// <param name="pattern">The <see cref="PatternMatch"/> to use</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    /// <returns>An <see cref="IEnumerable{string}"/></returns>
    private static IEnumerable<string> WalkWithRetry(Node node, RetryNode firstMatch, PatternMatch pattern, StringBuilder buffer, int length, int matchCount)
    {
        if (matchCount == pattern.Count) // all words in this subtree are a match
        {
            foreach (var word in Walk(node, buffer))
            {
                yield return word;
            }
        }
        else if (node.RemainingDepth >= pattern.Count - matchCount) // words are available that may be matched to the (remaining) pattern
        {
            var curMatch = pattern[matchCount];

            foreach (var child in node.Children)
            {
                buffer.Append(child.Character);
                if (child.Character.HasValue && curMatch.IsMatch(child.Character.Value)) // keep matching
                {
                    foreach (var word in WalkWithRetry(child, firstMatch, pattern, buffer, length, matchCount + 1))
                    {
                        yield return word;
                    }
                }
                else // start over from the children of the first matching node
                {
                    if (!firstMatch.Visited)
                    {
                        firstMatch.Visited = true;
                        foreach (var word in WalkContaining(firstMatch.ToNode(), pattern, buffer, length, 0))
                        {
                            yield return word;
                        }
                    }
                }
                buffer.Length--;
            }
        }
    }

    /// <summary>
    /// Retrieves all words that match the given <see cref="PatternMatch"/> starting from the given <see cref="Node"/>.
    /// </summary>
    /// <param name="node">The <see cref="Node"/> to start from</param>
    /// <param name="pattern">The <see cref="PatternMatch"/> to use</param>
    /// <param name="buffer">The <see cref="StringBuilder"/> to (re)use</param>
    /// <returns>An <see cref="IEnumerable{string}"/></returns>
    private static IEnumerable<string> WalkContaining(Node node, PatternMatch pattern, StringBuilder buffer, int length, int matchCount)
    {
        if (node.RemainingDepth >= pattern.Count)
        {
            var charToMatch = pattern[0];
            foreach (var child in node.Children)
            {
                buffer.Append(child.Character);
                if (child.Character.HasValue && charToMatch.IsMatch(child.Character.Value)) // char is a match; try to match remaining strings to remaining pattern if possible
                {
                    foreach (var word in WalkWithRetry(child, child.ToRetry(), pattern, buffer, length, matchCount + 1)) // keep matching
                    {
                        yield return word;
                    }
                }
                else // look deeper in the branch
                {
                    foreach (var word in WalkContaining(child, pattern, buffer, length, 0))
                    {
                        yield return word;
                    }
                }
                buffer.Length--;
            }
        }
    }

    #endregion

    #endregion
}