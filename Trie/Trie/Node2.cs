using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace BlueHeron.Collections.Trie;

[StructLayout(LayoutKind.Auto, CharSet = CharSet.Auto)]
public struct Node2(char? character)
{
    #region Properties

    public bool IsWord;
    public int? RemainingDepth;
    public char? Character = character;
    public double? Value;
    public int? parentIndex;

    #endregion

}

[StructLayout(LayoutKind.Auto, CharSet = CharSet.Auto)]
public struct Trie2
{
    #region Construction

    public Trie2()
    {
        mNodes = [new Node2()];
    }

    #endregion

    #region Properties

    private Node2[] mNodes;

    #endregion

    #region Public methods and functions

    public void Add(string word)
    {
        var idx = 0;
        for (var i = 0; i < word.Length; i++)
        {
            var curChar = word[i];
            var isWord = i == word.Length - 1;

            idx = AddNode(curChar, idx, isWord);
        }
    }

    #endregion

    #region Private methods and functions

    private int AddNode(char character, int parentIndex, bool isWord, double? value = null)
    {
        mNodes = [ ..mNodes, new Node2() { Character = character, parentIndex = parentIndex, IsWord = isWord, Value = value}];
        return mNodes.Length - 1;
    }

    #endregion
}