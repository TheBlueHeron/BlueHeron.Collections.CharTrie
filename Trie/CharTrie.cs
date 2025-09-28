namespace BlueHeron.Collections.Trie;

/// <summary>
/// Object that represents a list of words, stored in a Trie structure for efficient lookup.
/// </summary>
public sealed class CharTrie
{
    #region Fields

    private readonly char[] mCharacters;
    private readonly List<CharNode> mNodes;
    private List<List<int>> mChildBuffers = []; // temporary per-node child lists
    private readonly List<int> mChildIndices; // final flattened list

    #endregion

    #region Construction

    /// <summary>
    /// Creates a new <see cref="CharTrie"/>.
    /// </summary>
    /// <param name="characters">The array of all distinct characters that are present in the words to be added or imported</param>
    /// <exception cref="ArgumentException">The array must contain at least one character.</exception>
    /// <exception cref="NotSupportedException">The array cannot contain more than 255 characters.</exception>
    public CharTrie(char[] characters)
    {
        ArgumentNullException.ThrowIfNull(characters);
        if (characters.Length == 0)
        {
            throw new ArgumentException("The array must contain at least one character.", nameof(characters));
        }
        if (characters.Length > 255)
        {
            throw new NotSupportedException("The array cannot contain more than 255 characters.");
        }
        mCharacters = new char[1];
        mCharacters[0] = '\0'; // root character
        Array.Resize(ref mCharacters, characters.Length + 1);
        Array.Copy(characters, 0, mCharacters, 1, characters.Length);
        mChildIndices = [];
        mNodes = [];
        mNodes.Add(new CharNode { CharIndex = 0, FirstChildIndex = 0, ChildCount = 0, IsWordEnd = false }); // Root node (empty character)
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
        if (IsLocked) { throw new InvalidOperationException("The CharTrie is locked"); };
        ArgumentException.ThrowIfNullOrEmpty(word);

        var currentIndex = 0;

        for (var c = 0; c <= word.Length - 1; c++)
        {
            var charIndex = (byte)Array.IndexOf(mCharacters, word[c]);
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
            var charIndex = (byte)Array.IndexOf(mCharacters, word[c]);
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
    /// Optimizes the <see cref="CharTrie"/> by flattening the child node lists and preventing further additions.
    /// </summary>
    public void FinalizeTrie()
    {
        mChildIndices.Clear();
        for (var i = 0; i < mNodes.Count; i++)
        {
            var children = mChildBuffers[i];
            var node = mNodes[i];

            node.FirstChildIndex = mChildIndices.Count;
            node.ChildCount = (byte)children.Count;
            mNodes[i] = node; // update node
            mChildIndices.AddRange(children);
        }
        mChildBuffers.Clear();
        mChildBuffers = null!;
        IsLocked = true;
    }


    /// <summary>
    /// Returns all words matching the given prefix.
    /// </summary>
    /// <param name="prefix">The prefix to match. If <see langword="null"/> or empty, all words are returned</param>
    /// <returns>A <see cref="List{string}"/> containing all words that match the prefix</returns>
    public IEnumerable<string> Get(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return All();
        }
        List<string> results = [];
        var currentIndex = 0;

        for (var c = 0; c < prefix.Length; c++)
        {
            var charIndex = (byte)Array.IndexOf(mCharacters, prefix[c]);
            var firstChildIndex = mNodes[currentIndex].FirstChildIndex;
            var found = false;

            for (var i = 0; i < mNodes[currentIndex].ChildCount; i++)
            {
                var childIndex = mChildIndices[firstChildIndex + i];

                if (mNodes[childIndex].CharIndex == charIndex)
                {
                    currentIndex = childIndex;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                return results;
            }
        }
        Walk(currentIndex, prefix, ref results); // prefix branch has been found; return all words in this branch
        return results;
    }


    #endregion

    #region Private methods and functions

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