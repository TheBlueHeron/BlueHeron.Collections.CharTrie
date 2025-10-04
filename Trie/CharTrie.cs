using System.Buffers;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
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

    // Fast map from Unicode codepoint (char) to character index in mCharacters
    // 0xFF (255) means not found / invalid (note: 0 is reserved for root)
    private readonly byte[] mCharMap;

    /// <summary>
    /// Represents a character node in the <see cref="CharTrie"/>.
    /// Compact: FirstChildIndex (int) + Meta (uint) -> 8 bytes total on most runtimes.
    /// Meta layout: bits 0-7: CharIndex; bits 8-15: ChildCount; bit 16: IsWordEnd
    /// </summary>
    [JsonConverter(typeof(CharNodeConverter))]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct CharNode : IEquatable<CharNode>
    {
        public int FirstChildIndex;
        private uint _meta;

        public byte CharIndex
        {
            readonly get => (byte)(_meta & 0xFFu);
            set => _meta = (_meta & ~0xFFu) | value;
        }

        public byte ChildCount
        {
            readonly get => (byte)((_meta >> 8) & 0xFFu);
            set => _meta = (_meta & ~0xFF00u) | ((uint)value << 8);
        }

        public bool IsWordEnd
        {
            readonly get => ((_meta >> 16) & 1u) == 1u;
            set
            {
                if (value)
                {
                    _meta |= (1u << 16);
                }
                else
                {
                    _meta &= ~(1u << 16);
                }
            }
        }

        #region Operators and overrides

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj) => obj is CharNode n && Equals(n);

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
    /// Creates a new <see cref="CharTrie"/>. Used by <see cref="CharTrieFactory"/>.
    /// </summary>
    /// <param name="characters">The array of all distinct characters that are present in the words to be added or imported</param>
    /// <exception cref="ArgumentException">The array must contain at least one character.</exception>
    /// <exception cref="NotSupportedException">The array cannot contain more than 255 characters.</exception>
    internal CharTrie(char[] characters)
    {
        mCharacters = characters;
        mChildIndices = [];
        mNodes = new List<CharNode>(16)
        {
            new() { CharIndex = 0, FirstChildIndex = 0, ChildCount = 0, IsWordEnd = false } // root node
        };
        mChildBuffers = new List<List<int>>(4) { new(4) };

        mCharMap = new byte[ushort.MaxValue + 1]; // build char map: direct char code -> index  => O(1) lookups
        for (var i = 0; i < mCharMap.Length; i++) // initialize to 0xFF
        {
            mCharMap[i] = 0xFF;
        }

        for (var i = 0; i < mCharacters.Length; i++) // clamp: char -> index fits in a byte (factory limits characters to <=255 + root)
        {
            var ch = mCharacters[i];
            mCharMap[ch] = (byte)i;
        }
    }

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

        mCharMap = new byte[ushort.MaxValue + 1]; // build map for fast lookups when loaded from serialized data
        for (var i = 0; i < mCharMap.Length; i++)
        {
            mCharMap[i] = 0xFF;
        }
        for (var i = 0; i < mCharacters.Length; i++)
        {
            mCharMap[mCharacters[i]] = (byte)i;
        }
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

    /// <summary>
    /// Gets the number of nodes in the <see cref="CharTrie"/>.
    /// </summary>
    public int NumNodes => mNodes.Count;

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
        if (IsLocked)
        {
            throw new InvalidOperationException("Trie is locked");
        }

        var currentIndex = 0;
        var nodesLocal = mNodes;
        var buffersLocal = mChildBuffers!; // not null while unlocked

        for (var c = 0; c < word.Length; c++)
        {
            var charIndex = GetCharIndex(word[c]);
            var children = buffersLocal[currentIndex];
            var foundIndex = -1;

            for (var i = 0; i < children.Count; i++)
            {
                var childIdx = children[i];
                if (nodesLocal[childIdx].CharIndex == (byte)charIndex)
                {
                    foundIndex = childIdx;
                    break;
                }
            }
            if (foundIndex == -1)
            {
                var newNodeIndex = nodesLocal.Count;

                children.Add(newNodeIndex); // set up child list for new node
                nodesLocal.Add(new CharNode { CharIndex = (byte)charIndex, FirstChildIndex = 0, ChildCount = 0, IsWordEnd = false });
                buffersLocal.Add(new List<int>(2));
                currentIndex = newNodeIndex;
            }
            else
            {
                currentIndex = foundIndex;
            }
        }

        var node = nodesLocal[currentIndex]; // mark end-of-word
        if (!node.IsWordEnd)
        {
            node.IsWordEnd = true;
            nodesLocal[currentIndex] = node;
            Count++;
        }
    }

    /// <summary>
    /// Adds the given words to the <see cref="CharTrie"/>.
    /// </summary>
    /// <param name="words">An <see cref="IEnumerable{string}"/> containing the words to add</param>
    /// <exception cref="ArgumentNullException"><paramref name="words"/> is <see langword="null"/></exception>
    public void AddRange(IEnumerable<string> words)
    {
        ArgumentNullException.ThrowIfNull(words);
        foreach (var word in words)
        {
            Add(word);
        }
    }

    /// <summary>
    /// Returns all words in the <see cref="CharTrie"/>.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{string}"/></returns>
    public IEnumerable<string> All()
    {
        var results = new List<string>();
        // if no children, return empty
        var rootChildCount = mNodes[0].ChildCount;
        var childStart = mNodes[0].FirstChildIndex;
        for (var i = 0; i < rootChildCount; i++)
        {
            var childIndex = mChildIndices[childStart + i];
            var ch = mCharacters[mNodes[childIndex].CharIndex]; // start prefix with the character for this child
            Walk(childIndex, ch, ref results);
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
        var nodesLocal = mNodes;
        var childIndicesLocal = mChildIndices;

        for (var c = 0; c < word.Length; c++)
        {
            var charIndex = GetCharIndex(word[c]);
            var childStart = nodesLocal[currentIndex].FirstChildIndex;
            var childCount = nodesLocal[currentIndex].ChildCount;
            var found = false;

            for (var i = 0; i < childCount; i++)
            {
                var childIndex = childIndicesLocal[childStart + i];
                if (nodesLocal[childIndex].CharIndex == (byte)charIndex)
                {
                    found = true;
                    currentIndex = childIndex;
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
        mNodes.TrimExcess();
        mChildIndices.TrimExcess();
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
        var rootNode = mNodes[0];
        var childStart = rootNode.FirstChildIndex;
        var childCount = rootNode.ChildCount;

        for (var i = 0; i < childCount; i++)
        {
            var childIdx = mChildIndices[childStart + i];
            var ch = mCharacters[mNodes[childIdx].CharIndex];

            if (pattern[0].IsMatch(ch))
            {
                stack.Push((childIdx, 1, ch.ToString()));
            }
        }
        while (stack.Count > 0)
        {
            var (nodeIndex, depth, word) = stack.Pop();
            var node = mNodes[nodeIndex];

            childStart = node.FirstChildIndex;
            childCount = node.ChildCount;
            if (depth == pattern.Count)
            {
               if (node.IsWordEnd)
                {
                    results.Add(word);
                }
                continue;
            }

            var charMatch = pattern[depth];

            for (var i = 0; i < childCount; i++)
            {
                var childIdx = mChildIndices[childStart + i];
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
        var stack = new Stack<(int nodeIndex, int depth)>();
        var buffer = ArrayPool<char>.Shared.Rent(256);

        for (var i = 0; i < mNodes[0].ChildCount; i++) // start from root's children
        {
            stack.Push((mChildIndices[mNodes[0].FirstChildIndex + i], 1));
        }

        while (stack.Count > 0)
        {
            var (nodeIndex, depth) = stack.Pop();
            var node = mNodes[nodeIndex];

            buffer[depth - 1] = mCharacters[node.CharIndex];
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
                        if (node.IsWordEnd)
                        {
                            results.Add(new string(buffer, 0, depth));
                        }
                        break; // no need to check other offsets
                    }
                }
            }
            for (var i = node.ChildCount - 1; i >= 0; i--) // continue traversal
            {
                var childIdx = mChildIndices[node.FirstChildIndex + i];
                buffer[depth] = mCharacters[mNodes[childIdx].CharIndex];
                stack.Push((childIdx, depth + 1));
            }
        }
        ArrayPool<char>.Shared.Return(buffer);
    }

    /// <summary>
    /// Matches all words that start with the pattern, represented by the given <see cref="PatternMatch"/>.
    /// </summary>
    /// <param name="pattern">The <see cref="PatternMatch"/> to evaluate</param>
    /// <param name="results">Reference to the result list</param>
    private void FindPrefix(PatternMatch pattern, ref List<string> results)
    {
        var stack = new Stack<(int nodeIndex, int depth, string prefix)>();
        var rootNode = mNodes[0];
        var childStart = rootNode.FirstChildIndex;
        var childCount = rootNode.ChildCount;

        for (var i = 0; i < childCount; i++) // step 1: Match first CharMatch against all root children
        {
            var childIdx = mChildIndices[childStart + i];
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
            var node = mNodes[nodeIndex];

            childStart = node.FirstChildIndex;
            childCount = node.ChildCount;
            for (var i = 0; i < childCount; i++)
            {
                var childIdx = mChildIndices[childStart + i];
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
        var buffer = ArrayPool<char>.Shared.Rent(256);

        for (var i = 0; i < mNodes[0].ChildCount; i++) // start from root's children
        {
            stack.Push((mChildIndices[mNodes[0].FirstChildIndex + i], 1));
        }
        while (stack.Count > 0)
        {
            var (nodeIndex, depth) = stack.Pop();
            var node = mNodes[nodeIndex];
            var childStart = node.FirstChildIndex;
            var childCount = node.ChildCount;

            buffer[depth - 1] = mCharacters[node.CharIndex];
            if (node.IsWordEnd)
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
            for (var i = childCount - 1; i >= 0; i--)
            {
                var childIdx = mChildIndices[childStart + i];
                buffer[depth] = mCharacters[mNodes[childIdx].CharIndex];
                stack.Push((childIdx, depth + 1));
            }
        }
        ArrayPool<char>.Shared.Return(buffer);
    }

    /// <summary>
    /// Returns the index of the given character in the characters array.
    /// Uses O(1) table lookup instead of binary search.
    /// </summary>
    private int GetCharIndex(char character)
    {
        var idx = mCharMap[character];

        if (idx == 0xFF) // not present
        {
            throw new NotSupportedException(nameof(character));
        }
        return idx;
    }

    /// <summary>
    /// Returns all words in the <see cref="CharTrie"/> starting from the given first char.
    /// </summary>
    /// <param name="startIndex">The index of the <see cref="CharNode"/> to start searching from</param>
    /// <param name="firstChar">The first character</param>
    /// <param name="results">Reference to the <see cref="List{string}"/> containing the matched words</param>
    private void Walk(int startIndex, char firstChar, ref List<string> results)
    {
        var stack = new Stack<(int nodeIndex, int depth)>();
        var buffer = ArrayPool<char>.Shared.Rent(256);

        buffer[0] = firstChar;
        stack.Push((startIndex, 1));

        while (stack.Count > 0)
        {
            var (nodeIndex, depth) = stack.Pop();
            var node = mNodes[nodeIndex];
            var childStart = node.FirstChildIndex;
            var childCount = node.ChildCount;

            if (node.IsWordEnd) // buffer holds the character for this node already
            {
                results.Add(new string(buffer, 0, depth));
            }

            for (var i = childCount - 1; i >= 0; i--) // push children in reverse order to maintain correct output order
            {
                var childIndex = mChildIndices[childStart + i];

                buffer[depth] = mCharacters[mNodes[childIndex].CharIndex];
                stack.Push((childIndex, depth + 1));
            }
        }
        ArrayPool<char>.Shared.Return(buffer);
    }

    /// <summary>
    /// Returns all words in the <see cref="CharTrie"/> starting from the given node index and using the given prefix.
    /// </summary>
    /// <param name="startIndex">The index of the <see cref="CharNode"/> to start searching from</param>
    /// <param name="prefix">The string assembled so far</param>
    /// <param name="results">Reference to the <see cref="List{string}"/> containing the matched words</param>
    private void Walk(int startIndex, string prefix, ref List<string> results)
    {
        var stack = new Stack<(int nodeIndex, int depth)>();
        var buffer = ArrayPool<char>.Shared.Rent(256);
        prefix.CopyTo(0, buffer, 0, prefix.Length);

        stack.Push((startIndex, prefix.Length));

        while (stack.Count > 0)
        {
            var (nodeIndex, depth) = stack.Pop();
            var node = mNodes[nodeIndex];
            var childStart = node.FirstChildIndex;
            var childCount = node.ChildCount;

            buffer[depth - 1] = mCharacters[node.CharIndex];

            if (node.IsWordEnd)
            {
                results.Add(new string(buffer, 0, depth));
            }
            for (var i = childCount - 1; i >= 0; i--)
            {
                var childIndex = mChildIndices[childStart + i];

                buffer[depth] = mCharacters[mNodes[childIndex].CharIndex];
                stack.Push((childIndex, depth + 1));
            }
        }
        ArrayPool<char>.Shared.Return(buffer);
    }

    #endregion
}