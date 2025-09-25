using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueHeron.Collections.Trie;

public sealed class TestTrie
{
    #region Fields

    private const byte IsWordMask = 1 << 0;     // 0000_0001
    private const byte IsVisitedMask = 1 << 1;  // 0000_0010

    private readonly char[] mCharacters;
    private byte[][][] mNodes; // [Depth][ParentIndex][NodeIndex] = CharIndex
    internal byte[][][] mFlags;

    #endregion

    #region Construction

    public TestTrie(char[] characters)
    {
        ArgumentNullException.ThrowIfNull(characters);
        if (characters.Length == 0)
        {
            throw new ArgumentException("The array must contain at least one character.", nameof(characters));
        }      

        mCharacters = characters;
        mNodes = new byte[1][][];
        mFlags = new byte[1][][];
    }

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

        var lastIndex = word.Length - 1;
        var currentDepth = 0;
        var currentParentIndex = 0;
        int currentNodeIndex;

        for (var i = 0; i <= lastIndex; i++)
        {
            var charIndex = Array.IndexOf(mCharacters, word[i]);
            if (charIndex == -1)
            {
                throw new ArgumentException($"Character '{word[i]}' is not in the character set.", nameof(word));
            }
            // Ensure the current depth level exists
            if (currentDepth >= mNodes.Length)
            {
                Array.Resize(ref mNodes, currentDepth + 1);
                Array.Resize(ref mFlags, currentDepth + 1);
            }
            if (mNodes[currentDepth] == null)
            {
                mNodes[currentDepth] = [];
                mFlags[currentDepth] = [];
            }
            if (mNodes[currentDepth][currentParentIndex] == null)
            {
                mNodes[currentDepth][currentParentIndex] = [];
                mFlags[currentDepth][currentParentIndex] = [];
            }
            currentNodeIndex = Array.IndexOf(mNodes[currentDepth][currentParentIndex], charIndex);
            
            if (currentNodeIndex == -1) // If the node does not exist, create it
            {
                currentNodeIndex = mNodes[currentDepth][currentParentIndex].Length; // new child index
                Array.Resize(ref mNodes[currentDepth][currentParentIndex], currentNodeIndex + 1);
                Array.Resize(ref mFlags[currentDepth][currentParentIndex], currentNodeIndex + 1);
                mNodes[currentDepth][currentParentIndex][currentNodeIndex] = (byte)charIndex; // set node to charIndex
                SetFlags(currentDepth, currentParentIndex, currentNodeIndex, i == lastIndex);
            }
            currentDepth++; // Move to the next depth level
            currentParentIndex = currentNodeIndex;
        }
    }

    #endregion

    #region Private methods and functions

    /// <summary>
    /// Gets whether the specified node represents the end of a word.
    /// </summary>
    /// <param name="depth">The depth of the node</param>
    /// <param name="parentIndex">The index of the parent node</param>
    /// <param name="nodeIndex">The index of the node</param>
    /// <returns>A <see cref="bool"/></returns>
    private bool GetIsWordEnd(int depth, int parentIndex, int nodeIndex) => (mFlags[depth][parentIndex][nodeIndex] & IsWordMask) != 0;

    /// <summary>
    /// Gets whether the specified node has been visited.
    /// </summary>
    /// <param name="depth">The depth of the node</param>
    /// <param name="parentIndex">The index of the parent node</param>
    /// <param name="nodeIndex">The index of the node</param>
    /// <returns>A <see cref="bool"/></returns>
    private bool GetIsVisited(int depth, int parentIndex, int nodeIndex) => (mFlags[depth][parentIndex][nodeIndex] & IsVisitedMask) != 0;

    /// <summary>
    /// Sets the flag values for the specified node.
    /// </summary>
    /// <param name="depth">The depth of the node</param>
    /// <param name="parentIndex">The index of the parent node</param>
    /// <param name="nodeIndex">The index of the node</param>
    /// <param name="isWordEnd"><see langword="true"/> if the node represents the end of a word</param>
    /// <param name="isVisited"><see langword="true"/> if the node has been visited</param>
    [DebuggerStepThrough()]
    private void SetFlags(int depth, int parentIndex, int nodeIndex, bool isWordEnd, bool isVisited = false)
    {
        byte flags = 0;
        if (isWordEnd)
        {
            flags |= IsWordMask;
        }
        if (isVisited)
        {
            flags |= IsVisitedMask;
        }
        mFlags[depth][parentIndex][nodeIndex] = flags;
    }

    #endregion
}