using System.Text.Json.Serialization;
using BlueHeron.Collections.Trie.Search;
using BlueHeron.Collections.Trie.Serialization;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// A Trie (or prefix tree) for storing strings made up of unicode characters.
/// </summary>
[JsonConverter(typeof(CharTrieConverter))]
public class CharTrie
{
    #region Fields

    internal CharNode[] mNodes;
    internal int[][] mChildren;
    internal bool[] mIsWordEnd;
    internal int mNodeCount;

    #endregion

    #region Construction

    /// <summary>
    /// Creates a new <see cref="CharTrie"/>.
    /// </summary>
    public CharTrie()
    {
        mNodes = new CharNode[1]; // start with 1 node (root)
        mChildren = new int[1][]; // children of each node
        mIsWordEnd = new bool[1]; // word end markers

        mNodeCount = 1; // root is at index 0
        mNodes[0] = new CharNode('\0');
        mChildren[0] = [];
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the number of nodes in this <see cref="CharTrie"/>.
    /// </summary>
    public int NumNodes => mNodeCount;

    /// <summary>
    /// Gets the number of words in this <see cref="CharTrie"/>.
    /// </summary>
    public int NumWords => mIsWordEnd.Length;

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

        var current = 0; // index of root node

        for (var charIndex = 0; charIndex < word.Length; charIndex++)
        {
            var next = -1;
            var c = word[charIndex];

            for (var i = 0; i < mChildren[current].Length; i++) // search for existing child
            {
                var childIndex = mChildren[current][i];
                if (mNodes[childIndex].Character == c)
                {
                    next = childIndex;
                    break;
                }
            }

            if (next == -1) // if not found, create new node
            {
                if (mNodeCount >= mNodes.Length) // resize arrays if needed
                {
                    Array.Resize(ref mNodes, mNodes.Length + 1);
                    Array.Resize(ref mChildren, mChildren.Length + 1);
                    Array.Resize(ref mIsWordEnd, mIsWordEnd.Length + 1);
                }

                next = mNodeCount++;
                mNodes[next] = new CharNode(c);
                mChildren[next] = [];

                var updated = new int[mChildren[current].Length + 1]; // add new child to current node
                Array.Copy(mChildren[current], updated, mChildren[current].Length);
                updated[^1] = next; // add index of new child at the end
                mChildren[current] = updated;
            }
            mNodes[current].RemainingDepth = (byte)Math.Max(mNodes[current].RemainingDepth, word.Length - charIndex);
            current = next;
        }
        mIsWordEnd[current] = true;
    }

    /// <summary>
    /// Returns all words in the <see cref="CharTrie"/>.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{string}"/></returns>
    public IEnumerable<string> All()
    {
        foreach (var word in Walk(0, string.Empty))
        {
            yield return word;
        }
    }

    /// <summary>
    /// Returns an enumerable collection of words that match the specified <see cref="PatternMatch"/>.
    /// </summary>
    /// <param name="pattern">The <see cref="PatternMatch"/> to match. if <see langword="null"/> or empty, all words will be returned.</param>
    /// <returns>An enumerable collection of strings containing all words that match the given <see cref="PatternMatch"/>. The collection is empty if no words match.</returns>
    public IEnumerable<string> Containing(PatternMatch pattern)
    {
        if (pattern == null || pattern.Count == 0) // 
        {
            foreach (var word in All())
            {
                yield return word;
            }
        }
        else
        {
            switch (pattern.MatchType)
            {
                case PatternMatchType.IsPrefix:
                    var current = 0;
                    var prefix = string.Empty;

                    foreach (var c in pattern)
                    {
                        var found = false;
                        for (var i = 0; i < mChildren[current].Length; i++)
                        {
                            var childIndex = mChildren[current][i];
                            if (c.IsMatch(mNodes[childIndex].Character))
                            {
                                prefix += mNodes[childIndex].Character;
                                current = childIndex;
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            yield break; // no words with this prefix
                        }
                    }
                    foreach (var word in Walk(current, prefix[..^1])) // the last prefix character is represented by current
                    {
                        yield return word;
                    }
                    break;
                case PatternMatchType.IsSuffix:
                    throw new NotImplementedException("Suffix search is not implemented yet.");
                case PatternMatchType.IsFragment:
                    throw new NotImplementedException("Fragment search is not implemented yet.");
                case PatternMatchType.IsWord:
                    throw new NotImplementedException("Exact word search is not implemented yet.");
            }
        }
    }

    #endregion

    #region Private methods and functions

    /// <summary>
    /// Recursively walks the trie from the given index, yielding all words found that match the prefix followed by the character represented by the <see cref="CharNode"/> at the given index.
    /// </summary>
    /// <param name="index">The index of the <see cref="CharNode"/> to start from</param>
    /// <param name="prefix">The characters already added to the output</param>
    /// <returns>An <see cref="IEnumerable{string}"/></returns>
    private IEnumerable<string> Walk(int index, string prefix)
    {
        if (index != 0)
        {
            prefix += mNodes[index].Character;
        }

        if (mIsWordEnd[index])
        {
            yield return prefix;
        }

        for (var i = 0; i < mChildren[index].Length; i++)
        {
            foreach (var word in Walk(mChildren[index][i], prefix))
            {
                yield return word;
            }
        }
    }

    #endregion
}