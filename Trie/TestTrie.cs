//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace BlueHeron.Collections.Trie;

//public sealed class TestTrie
//{
//    #region Fields

//    //private const byte IsWordMask = 1 << 0;     // 0000_0001
//    //private const byte IsVisitedMask = 1 << 1;  // 0000_0010

//    private readonly char[] mCharacters;
//    private byte[] mRootNodes;

//    #endregion

//    #region Construction

//    /// <summary>
//    /// Creates a new <see cref="TestTrie"/>.
//    /// </summary>
//    /// <param name="characters">The array of all distinct characters that are present in the words to be added or imported</param>
//    /// <exception cref="ArgumentException">The array must contain at least one character.</exception>
//    /// <exception cref="NotSupportedException">The array cannot contain more than 255 characters.</exception>
//    public TestTrie(char[] characters)
//    {
//        ArgumentNullException.ThrowIfNull(characters);
//        if (characters.Length == 0)
//        {
//            throw new ArgumentException("The array must contain at least one character.", nameof(characters));
//        }
//        if (characters.Length > 255)
//        {
//            throw new NotSupportedException("The array cannot contain more than 255 characters.");
//        }
//        mCharacters = new char[1];
//        mCharacters[0] = '\0'; // root character
//        Array.Resize(ref mCharacters, characters.Length + 1);
//        Array.Copy(characters, 0, mCharacters, 1, characters.Length);
//        mCharacters = characters;
//        mRootNodes = [];
//    }

//    #endregion

//    #region Public methods and functions

//    /// <summary>
//    /// Adds the given word to the <see cref="CharTrie"/>.
//    /// </summary>
//    /// <param name="word">The word to add</param>
//    /// <exception cref="ArgumentException">Thrown if <paramref name="word"/> is <see langword="null"/> or empty</exception>
//    public void Add(string word)
//    {
//        ArgumentException.ThrowIfNullOrEmpty(word);

//        var lastIndex = word.Length - 1;

//        for (var i = 0; i <= lastIndex; i++)
//        {
//            var charIndex = Array.IndexOf(mCharacters, word[i]);
//            if (charIndex == -1)
//            {
//                throw new ArgumentException($"Character '{word[i]}' is not in the character set.", nameof(word));
//            }
//        }
//    }

//    ///// <summary>
//    ///// Returns all words in the <see cref="TestTrie"/>.
//    ///// </summary>
//    ///// <returns>An <see cref="IEnumerable{string}"/></returns>
//    //public IEnumerable<string> All()
//    //{
//    //    for (var i = 0; i < mNodes[0].Length; i++)
//    //    {
//    //        if (mNodes[0].Length > 0)
//    //        {
//    //            foreach (var word in Walk(0, i, $"{mCharacters[mNodes[0][i]]}"))
//    //            {
//    //                yield return word;
//    //            }
//    //        }
//    //    }
//    //}

//    #endregion

//    #region Private methods and functions

//    ///// <summary>
//    ///// Gets whether the specified node represents the end of a word.
//    ///// </summary>
//    ///// <param name="parentIndex">The index of the parent node</param>
//    ///// <param name="nodeIndex">The index of the node</param>
//    ///// <returns>A <see cref="bool"/></returns>
//    //private bool GetIsWordEnd(int parentIndex, int nodeIndex) => (mFlags[parentIndex][nodeIndex] & IsWordMask) != 0;

//    ///// <summary>
//    ///// Gets whether the specified node has been visited.
//    ///// </summary>
//    ///// <param name="parentIndex">The index of the parent node</param>
//    ///// <param name="nodeIndex">The index of the node</param>
//    ///// <returns>A <see cref="bool"/></returns>
//    //private bool GetIsVisited(int parentIndex, int nodeIndex) => (mFlags[parentIndex][nodeIndex] & IsVisitedMask) != 0;

//    ///// <summary>
//    ///// Sets the flag values for the specified node.
//    ///// </summary>
//    ///// <param name="parentIndex">The index of the parent node</param>
//    ///// <param name="nodeIndex">The index of the node</param>
//    ///// <param name="isWordEnd"><see langword="true"/> if the node represents the end of a word</param>
//    ///// <param name="isVisited"><see langword="true"/> if the node has been visited</param>
//    //[DebuggerStepThrough()]
//    //private void SetFlags(int parentIndex, int nodeIndex, bool isWordEnd, bool isVisited = false)
//    //{
//    //    byte flags = 0;
//    //    if (isWordEnd)
//    //    {
//    //        flags |= IsWordMask;
//    //    }
//    //    if (isVisited)
//    //    {
//    //        flags |= IsVisitedMask;
//    //    }
//    //    mFlags[parentIndex][nodeIndex] = flags;
//    //}

//    ///// <summary>
//    ///// Recursively walks the child trie from the given depth, yielding all words found.
//    ///// </summary>
//    ///// <param name="depth">The depth at which to start</param>
//    ///// <param name="parentIndex">The index of the parent node</param>
//    ///// <returns>An <see cref="IEnumerable{string}"/></returns>
//    //private IEnumerable<string> Walk(int depth, int parentIndex, string buffer)
//    //{
//    //    for (var nodeIndex = 0; nodeIndex < mChildren[depth][parentIndex].Length; nodeIndex++)
//    //    {
//    //        var charIndex = mChildren[depth][parentIndex][nodeIndex];
//    //        var newBuffer = buffer + mCharacters[charIndex];
//    //        if (GetIsWordEnd(depth, parentIndex, nodeIndex))
//    //        {
//    //            yield return newBuffer;
//    //        }
//    //        if (mChildren.Length > depth + 1 && mChildren[depth + 1].Length > 0)
//    //        {
//    //            foreach (var word in Walk(depth + 1, nodeIndex, newBuffer))
//    //            {
//    //                yield return word;
//    //            }
//    //        }
//    //    }
//    //}

//    #endregion
//}