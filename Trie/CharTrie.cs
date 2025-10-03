using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using BlueHeron.Collections.Trie.Search;
using BlueHeron.Collections.Trie.Serialization;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// Object that represents a list of words, stored in a Trie structure for efficient lookup.
/// </summary>
[JsonConverter(typeof(CharTrieConverter))]
public sealed class CharTrie
{
    #region Fields

    internal readonly char[] mCharacters;
    internal readonly List<CharNode> mNodes;
    private List<List<int>> mChildBuffers; // temporary per-node child lists
    internal readonly List<int> mChildIndices; // final flattened list

    /// <summary>
    /// Represents a character node in the <see cref="CharTrie"/>.
    /// </summary>
    [JsonConverter(typeof(CharNodeConverter))]
    [StructLayout(LayoutKind.Explicit)]
    internal struct CharNode : IEquatable<CharNode>
    {
        [FieldOffset(0)]
        public int FirstChildIndex;
        [FieldOffset(4)]
        public byte CharIndex;
        [FieldOffset(5)]
        public byte ChildCount;
        [FieldOffset(6)]
        public bool IsWordEnd;

        #region Operators and overrides

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj) => object.Equals(CharIndex, obj);

        /// <inheritdoc/>
        public readonly override int GetHashCode() => CharIndex.GetHashCode();

        /// <summary>
        /// Compares by comparing <see cref="CharIndex"/> values.
        /// </summary>
        public static bool operator ==(CharNode left, CharNode right) => left.Equals(right);

        /// <summary>
        /// Compares by comparing <see cref="CharIndex"/> values.
        /// </summary>
        public static bool operator !=(CharNode left, CharNode right) => !(left == right);

        /// <summary>
        /// Compares by comparing <see cref="CharIndex"/> values.
        /// </summary>
        /// <param name="other">The <see cref="CharNode"/> to compare</param>
        public readonly bool Equals(CharNode other) => CharIndex == other.CharIndex;

        #endregion
    }

    #endregion

    #region Construction

    /// <summary>
    /// Creates a new <see cref="CharTrie"/>. Used by <see cref="CharTrieConverter"/>.
    /// </summary>
    /// <param name="characters">The array of supported characters</param>
    /// <param name="nodes">The list of all <see cref="CharNode"/>s in the <see cref="CharTrie"/></param>
    /// <param name="childIndices">The list of all child node indices</param>
    /// <param name="numWords">The number of words in the <see cref="Trie"/></param>
    internal CharTrie(char[] characters, List<CharNode> nodes, List<int> childIndices, int numWords)
    {
        mCharacters = characters;
        mNodes = nodes;
        mChildIndices = childIndices;
        Count = numWords;
        mChildBuffers = null!;
        IsLocked = true;
    }

    /// <summary>
    /// Creates a new <see cref="CharTrie"/>. Used by <see cref="CharTrieFactory"/>.
    /// </summary>
    /// <param name="characters">The array of all distinct characters that are present in the words to be added or imported</param>
    /// <exception cref="ArgumentException">The array must contain at least one character.</exception>
    /// <exception cref="NotSupportedException">The array cannot contain more than 255 characters.</exception>
    internal CharTrie(char[] characters)
    {
        mCharacters = characters;
        mChildIndices = [];
        mNodes = [];
        mNodes.Add(new CharNode { CharIndex = 0, FirstChildIndex = 0, ChildCount = 0, IsWordEnd = false }); // Root node (empty character)
        mChildBuffers = [];
        mChildBuffers.Add([]);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the number of words in the <see cref="CharTrie"/>.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Returns a value, indicating whether words can be added to the <see cref="CharTrie"/> (i.e. it has not been finalized yet).
    /// </summary>
    public bool IsLocked { get; set; }

#if DEBUG
    /// <summary>
    /// Gets the number of nodes in the <see cref="CharTrie"/>.
    /// </summary>
    public int NumNodes => mNodes.Count;
#endif

#endregion

    #region Public methods and functions

    /// <summary>
    /// Adds the given word to the <see cref="CharTrie"/>.
    /// </summary>
    /// <param name="word">The word to add</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="word"/> is <see langword="null"/> or empty</exception>
    public void Add(string word)
    {
        ArgumentException.ThrowIfNullOrEmpty(word);
        if (!IsLocked)
        {
            var currentIndex = 0;

            for (var c = 0; c <= word.Length - 1; c++)
            {
                var charIndex = GetCharIndex(word[c]);
                var children = mChildBuffers[currentIndex];
                var foundIndex = -1;

                foreach (var childIndex in children)
                {
                    if (mNodes[childIndex].CharIndex == charIndex)
                    {
                        foundIndex = childIndex;
                        break;
                    }
                }
                if (foundIndex == -1) // create new node
                {
                    var newIndex = mNodes.Count;

                    mNodes.Add(new CharNode { CharIndex = charIndex, FirstChildIndex = mChildIndices.Count });
                    mChildBuffers.Add([]);
                    children.Add(newIndex);
                    foundIndex = newIndex;

                }
                currentIndex = foundIndex;
            }
            var terminalNode = mNodes[currentIndex];
            terminalNode.IsWordEnd = true;
            mNodes[currentIndex] = terminalNode; // update terminal node
            Count++;
        }
    }

    /// <summary>
    /// Returns all words in the <see cref="CharTrie"/>.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{string}"/></returns>
    public IEnumerable<string> All()
    {
        List<string> results = [];

        for (var i = 0; i < mNodes[0].ChildCount; i++)
        {
            var childIndex = mChildIndices[mNodes[0].FirstChildIndex + i];

            Walk(childIndex, $"{mCharacters[mNodes[childIndex].CharIndex]}", ref results);
        }
        return results;
    }

    /// <summary>
    /// Returns a value, determining whether the <see cref="CharTrie"/> contains the given word.
    /// </summary>
    /// <param name="word">The word to locate</param>
    /// <returns><see langword="true"/> if the word exists in the <see cref="CharTrie"/>; else <see langword="false"/></returns>
    public bool Contains(string word)
    {
        ArgumentException.ThrowIfNullOrEmpty(word);

        var currentIndex = 0;

        for (var c = 0; c < word.Length; c++)
        {
            var charIndex = GetCharIndex(word[c]);
            var childStart = mNodes[currentIndex].FirstChildIndex;
            var childCount = mNodes[currentIndex].ChildCount;
            var found = false;

            for (var i = 0; i < childCount; i++)
            {
                var childIndex = mChildIndices[childStart + i];

                if (mNodes[childIndex].CharIndex == charIndex)
                {
                    currentIndex = childIndex;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                return false;
            }
        }
        return mNodes[currentIndex].IsWordEnd;
    }

    /// <summary>
    /// Returns all words that match the given <see cref="PatternMatch"/>. If the pattern is empty, all words will be returned.
    /// </summary>
    /// <param name="pattern">The <see cref="PatternMatch"/> to evaluate</param>
    /// <returns>An <see cref="IEnumerable{string}"/> containing all matching words</returns>
    /// <exception cref="ArgumentNullException">Yhe pattern is <see langword="null"/></exception>
    public IEnumerable<string> Find(PatternMatch pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern, nameof(pattern));
        
        if (pattern.Count == 0)
        {
            return All();
        }
        var results = new List<string>();

        switch (pattern.MatchType)
        {
            case PatternMatchType.IsPrefix:
                FindPrefix(pattern, ref results);
                break;

            case PatternMatchType.IsWord:
                FindExact(pattern, ref results);
                break;

            case PatternMatchType.IsFragment:
                FindFragment(pattern, ref results);
                break;

            case PatternMatchType.IsSuffix:
                FindSuffix(pattern, ref results);
                break;
        }
        return results;
    }

    /// <summary>
    /// Optimizes the <see cref="CharTrie"/> by flattening the child node lists and preventing further additions.
    /// Optionally, the <see cref="Trie"/> will be sorted alphabetically. This is needed only when words were not added to the <see cref="Trie"/> in alphabetic order.
    /// </summary>
    /// <param name="sort"></param>If <see langword="true"/>, child nodes are alphabetically before flattening</param>
    public void Prune(bool sort = false)
    {
        mChildIndices.Clear();
        for (var i = 0; i < mNodes.Count; i++)
        {
            var children = mChildBuffers[i];
            var node = mNodes[i];

            if (sort)
            {
                children.Sort((a, b) => mNodes[a].CharIndex.CompareTo(mNodes[b].CharIndex));
            }
            node.FirstChildIndex = mChildIndices.Count;
            node.ChildCount = (byte)children.Count;
            mNodes[i] = node; // update node
            mChildIndices.AddRange(children);
        }
        mChildBuffers.Clear();
        mChildBuffers = null!;
        IsLocked = true;
    }

    #endregion

    #region Private methods and functions

    /// <summary>
    /// Matches all words that start with the pattern, represented by the given <see cref="PatternMatch"/> and whose length is the same as the pattern length.
    /// </summary>
    /// <param name="pattern">The <see cref="PatternMatch"/> to evaluate</param>
    /// <param name="results">Reference to the result list</param>
    private void FindExact(PatternMatch pattern, ref List<string> results)
    {
        var stack = new Stack<(int nodeIndex, int depth, string word)>();

        for (var i = 0; i < mNodes[0].ChildCount; i++)
        {
            var childIdx = mChildIndices[mNodes[0].FirstChildIndex + i];
            var ch = mCharacters[mNodes[childIdx].CharIndex];

            if (pattern[0].IsMatch(ch))
            {
                stack.Push((childIdx, 1, ch.ToString()));
            }
        }
        while (stack.Count > 0)
        {
            var (nodeIndex, depth, word) = stack.Pop();

            if (depth == pattern.Count)
            {
               if (mNodes[nodeIndex].IsWordEnd)
                {
                    results.Add(word);
                }
                continue;
            }

            var charMatch = pattern[depth];

            for (var i = 0; i < mNodes[nodeIndex].ChildCount; i++)
            {
                var childIdx = mChildIndices[mNodes[nodeIndex].FirstChildIndex + i];
                var ch = mCharacters[mNodes[childIdx].CharIndex];

                if (charMatch.IsMatch(ch))
                {
                    stack.Push((childIdx, depth + 1, word + ch));
                }
            }
        }
    }

    /// <summary>
    /// Matches all words that contain the pattern, represented by the given <see cref="PatternMatch"/>.
    /// </summary>
    /// <param name="pattern">The <see cref="PatternMatch"/> to evaluate</param>
    /// <param name="results">Reference to the result list</param>
    private void FindFragment(PatternMatch pattern, ref List<string> results)
    {
        //var stack = new Stack<(int nodeIndex, int depth, string matchedPrefix)>();

        //for (var i = 1; i < mNodes.Count; i++)
        //{
        //    var ch = mCharacters[mNodes[i].CharIndex];

        //    if (pattern[0].IsMatch(ch))
        //    {
        //        stack.Push((i, 1, $"{ch}"));
        //    }
        //}

        //while (stack.Count > 0)
        //{
        //    var (nodeIndex, depth, matchedPrefix) = stack.Pop();

        //    if (depth == pattern.Count)
        //    {
        //        if (mNodes[nodeIndex].IsWordEnd || mNodes[nodeIndex].ChildCount > 0)
        //        {
        //            Walk(nodeIndex, matchedPrefix, ref results);
        //        }
        //        continue;
        //    }

        //    var charMatch = pattern[depth];

        //    for (var i = 0; i < mNodes[nodeIndex].ChildCount; i++)
        //    {
        //        var childIdx = mChildIndices[mNodes[nodeIndex].FirstChildIndex + i];
        //        var ch = mCharacters[mNodes[childIdx].CharIndex];

        //        if (charMatch.IsMatch(ch))
        //        {
        //            stack.Push((childIdx, depth + 1, matchedPrefix + ch));
        //        }
        //    }
        //}

        var stack = new Stack<(int nodeIndex, int depth)>();
        var buffer = new char[256]; // Adjust if needed

        // Start from root's children
        for (var i = 0; i < mNodes[0].ChildCount; i++)
        {
            var childIdx = mChildIndices[mNodes[0].FirstChildIndex + i];
            //buffer[0] = mCharacters[mNodes[childIdx].CharIndex];
            stack.Push((childIdx, 1));
        }

        while (stack.Count > 0)
        {
            var (nodeIndex, depth) = stack.Pop();

            buffer[depth - 1] = mCharacters[mNodes[nodeIndex].CharIndex];
            if (depth >= pattern.Count) // check for fragment match
            {
                for (var offset = 0; offset <= depth - pattern.Count; offset++)
                {
                    var match = true;

                    for (var i = 0; i < pattern.Count; i++)
                    {
                        if (!pattern[i].IsMatch(buffer[offset + i]))
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        if (mNodes[nodeIndex].IsWordEnd)
                        {
                            results.Add(new string(buffer, 0, depth));
                        }
                        break; // no need to check other offsets
                    }
                }
            }
            for (var i = mNodes[nodeIndex].ChildCount - 1; i >= 0; i--) // continue traversal
            {
                var childIdx = mChildIndices[mNodes[nodeIndex].FirstChildIndex + i];
                buffer[depth] = mCharacters[mNodes[childIdx].CharIndex];
                stack.Push((childIdx, depth + 1));
            }
        }
    }

    /// <summary>
    /// Matches all words that start with the pattern, represented by the given <see cref="PatternMatch"/>.
    /// </summary>
    /// <param name="pattern">The <see cref="PatternMatch"/> to evaluate</param>
    /// <param name="results">Reference to the result list</param>
    private void FindPrefix(PatternMatch pattern, ref List<string> results)
    {
        var stack = new Stack<(int nodeIndex, int depth, string prefix)>();

        for (var i = 0; i < mNodes[0].ChildCount; i++) // step 1: Match first CharMatch against all root children
        {
            var childIdx = mChildIndices[mNodes[0].FirstChildIndex + i];
            var ch = mCharacters[mNodes[childIdx].CharIndex];

            if (pattern[0].IsMatch(ch))
            {
                stack.Push((childIdx, 1, ch.ToString()));
            }
        }
        while (stack.Count > 0) // step 2: Traverse pattern
        {
            var (nodeIndex, depth, prefix) = stack.Pop();

            if (depth == pattern.Count)
            {
                Walk(nodeIndex, prefix, ref results);
                continue;
            }

            var charMatch = pattern[depth];

            for (var i = 0; i < mNodes[nodeIndex].ChildCount; i++)
            {
                var childIdx = mChildIndices[mNodes[nodeIndex].FirstChildIndex + i];
                var ch = mCharacters[mNodes[childIdx].CharIndex];

                if (charMatch.IsMatch(ch))
                {
                    stack.Push((childIdx, depth + 1, prefix + ch));
                }
            }
        }
    }

    /// <summary>
    /// Matches all words that end with the pattern, represented by the given <see cref="PatternMatch"/>.
    /// </summary>
    /// <param name="pattern">The <see cref="PatternMatch"/> to evaluate</param>
    /// <param name="results">Reference to the result list</param>
    private void FindSuffix(PatternMatch pattern, ref List<string> results)
    {
        var stack = new Stack<(int nodeIndex, int depth)>();
        var buffer = new char[256];

        for (var i = 0; i < mNodes[0].ChildCount; i++) // start from root's children
        {
            var childIdx = mChildIndices[mNodes[0].FirstChildIndex + i];
            buffer[0] = mCharacters[mNodes[childIdx].CharIndex];
            stack.Push((childIdx, 1));
        }
        while (stack.Count > 0)
        {
            var (nodeIndex, depth) = stack.Pop();

            buffer[depth - 1] = mCharacters[mNodes[nodeIndex].CharIndex];
            if (mNodes[nodeIndex].IsWordEnd)
            {
                if (depth >= pattern.Count)
                {
                    var match = true;
                    for (var i = 0; i < pattern.Count; i++)
                    {
                        var ch = buffer[depth - pattern.Count + i];
                        if (!pattern[i].IsMatch(ch))
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        results.Add(new string(buffer, 0, depth));
                    }
                }
            }
            for (var i = mNodes[nodeIndex].ChildCount - 1; i >= 0; i--)
            {
                var childIdx = mChildIndices[mNodes[nodeIndex].FirstChildIndex + i];
                buffer[depth] = mCharacters[mNodes[childIdx].CharIndex];
                stack.Push((childIdx, depth + 1));
            }
        }
    }

    /// <summary>
    /// Returns all words matching the given prefix.
    /// </summary>
    /// <param name="prefix">The prefix to match. If <see langword="null"/> or empty, all words are returned</param>
    /// <returns>A <see cref="List{string}"/> containing all words that match the prefix</returns>
    private IEnumerable<string> Find(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return All();
        }
        List<string> results = [];
        var currentIndex = 0;

        foreach (var c in prefix)
        {
            var charIndex = GetCharIndex(c);
            var found = false;
            for (var i = 0; i < mNodes[currentIndex].ChildCount; i++)
            {
                var childIdx = mChildIndices[mNodes[currentIndex].FirstChildIndex + i];
                if (mNodes[childIdx].CharIndex == charIndex)
                {
                    currentIndex = childIdx;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return results;
            }
        }
        Walk(currentIndex, prefix, ref results);
        return results;
    }

    /// <summary>
    /// Returns the index of the given character in the characters array.
    /// </summary>
    /// <param name="character">The character for which to find the index</param>
    /// <returns>The index of the character if it exists, else -1</returns>
    private byte GetCharIndex(char character)
    {
        var low = 0;
        var high = mCharacters.Length - 1;
        int mid;
        char midChar;

        while (low <= high) // reduce number of equality operations significantly by playing the higher/lower game (e.g. 100 chars -> average number of operations is reduced from 50 to about 7).
        {                   // this function assumes that the characters array is sorted!
            mid = low + (high - low) / 2;
            midChar = mCharacters[mid];

            if (character == midChar)
            {
                return (byte)mid;
            }
            else if (character < midChar)
            {
                high = mid - 1; // narrow downwards
            }
            else
            {
                low = mid + 1; // narrow upwards
            }
        }
        throw new NotSupportedException(nameof(character));
    }

    /// <summary>
    /// Returns all words in the <see cref="CharTrie"/> starting from the given node index and using the given prefix.
    /// </summary>
    /// <param name="startIndex">The index of the <see cref="CharNode"/> to start searching from</param>
    /// <param name="prefix">The string assembled so far</param>
    /// <param name="results">Reference to the <see cref="List{string}"/> containing the matched words</param>
    private void Walk(int startIndex, string prefix, ref List<string> results)
    {
        //if (mNodes[startIndex].IsWordEnd)
        //{
        //    results.Add(prefix);
        //}

        //var childStart = mNodes[startIndex].FirstChildIndex;
        //var childCount = mNodes[startIndex].ChildCount;

        //for (var i = 0; i < childCount; i++)
        //{
        //    var childIndex = mChildIndices[childStart + i];

        //    Walk(childIndex, prefix + mCharacters[mNodes[childIndex].CharIndex], ref results);
        //}

        var stack = new Stack<(int nodeIndex, int depth)>();
        var buffer = new char[256]; // Max word length
        prefix.CopyTo(0, buffer, 0, prefix.Length);

        stack.Push((startIndex, prefix.Length));

        while (stack.Count > 0)
        {
            var (nodeIndex, depth) = stack.Pop();

            buffer[depth - 1] = mCharacters[mNodes[nodeIndex].CharIndex];

            if (mNodes[nodeIndex].IsWordEnd)
            {
                results.Add(new string(buffer, 0, depth));
            }
            for (var i = mNodes[nodeIndex].ChildCount - 1; i >= 0; i--)
            {
                var childIndex = mChildIndices[mNodes[nodeIndex].FirstChildIndex + i];

                buffer[depth] = mCharacters[mNodes[childIndex].CharIndex];
                stack.Push((childIndex, depth + 1));
            }
        }

    }

    #endregion
}