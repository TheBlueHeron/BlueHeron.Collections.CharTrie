using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// A search optimized data structure for words.
/// </summary>
public class Trie2
{
    #region Fields

    /// <summary>
    /// A node in the <see cref="Trie2"/>, which represents a character.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct Node: IComparable<Node>, IEquatable<Node>
    {
        #region Properties

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
        public readonly bool IsWord;
        /// <summary>
        /// The maximum depth of this <see cref="Node"/>'s tree of children.
        /// </summary>
        public int? RemainingDepth;
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

        #region IComparable

        public readonly int CompareTo(Node other) => Character.HasValue? Character.Value.CompareTo(other.Character) : 1;

        public readonly override bool Equals(object? obj) => obj is Node node && Equals(node);

        public readonly override int GetHashCode() => Character.GetHashCode();

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

    private Node mRoot;

    #endregion

    #region Construction

    /// <summary>
    /// Creates a new <see cref="Trie2"/>.
    /// </summary>
    public Trie2()
    {
        mRoot = new Node();
    }

    #endregion

    #region Properties

    #endregion

    #region Public methods and functions

    /// <summary>
    /// Adds the given word to the collection.
    /// </summary>
    /// <param name="word">The word to add</param>
    /// <param name="value">Option value to assign</param>
    public void Add(string word, double? value = null)
    {
        var nodeIndexes = new int[word.Length];
        Array.Fill(nodeIndexes, -1);

        for (var i = 0; i < word.Length; i++)
        {
            var curChar = word[i];
            var isWord = i == word.Length - 1;

            nodeIndexes[i] = AddNode(curChar, ref GetNode(ref nodeIndexes),  isWord, value);
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
    /// Removes the given character from the given array of <see cref="Node"/>s and resizes the array without affecting the sort.
    /// </summary>
    /// <param name="nodes">The array of <see cref="Node"/>s</param>
    /// <param name="character">The character to remove</param>
    private static void Delete(ref Node[] nodes, char character)
    {
        var n = nodes.Length - 1;
        var pos = Search(nodes, 0, n, character);

        if (pos >= 0)
        {
            int i;
            for (i = pos; i <= n; i++)
            {
                nodes[i] = nodes[i + 1];
            }
            Array.Resize(ref nodes, n);
        }
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
        if (nodeIndexes[curDepth] == -1 || nodeIndexes.Length == curDepth)
        {
            return ref curNode;
        }
        return ref GetNode(ref curNode.Children[nodeIndexes[curDepth]], ref nodeIndexes, ++curDepth);
    }

    #endregion
}