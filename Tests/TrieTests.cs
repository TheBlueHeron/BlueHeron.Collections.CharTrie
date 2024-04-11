using System.Diagnostics;

namespace BlueHeron.Collections.Trie.Tests;

[TestClass]
public class TestTrie
{
    [TestMethod]
    public void CreateAndValidate()
    {
        var numWords = 0; // should be set to 2
        var woordeIsWord = true; // should be set to false
        var trie = Create();
        var totalWords = trie.NumWords;
        var blExistsWoo = trie.Exists("woo", true);

        var node = trie.GetNode("woord");
        if (node != null)
        {
            numWords = node.NumWords;
        }
        node = trie.GetNode("woorde");
        if (node != null)
        {
            woordeIsWord = node.IsWord;
        }

        Debug.Assert(totalWords == 5 && blExistsWoo && numWords == 2 && woordeIsWord == false);
    }

    [TestMethod]
    public void Removal()
    {
        var trie = Create();

        trie.RemovePrefix("woo");
        Debug.Assert(trie.NumWords == 3);
        trie.RemoveWord("wapens");
        Debug.Assert(trie.NumWords == 2);
    }

    [TestMethod]
    public void Traversal()
    {
        var trie =  Create();
        var words = trie.GetWords(null);

        Debug.Assert(words != null && words.ToList().Count == 5);

        words = trie.GetWords("woord");
        Debug.Assert(words != null && words.ToList().Count == 2);
    }

    [TestMethod]
    public void Find()
    {
        var trie = Create();
        IEnumerable<string> words;
        
        words = trie.Find(['w']);
        Debug.Assert(words.Count() == 3);
        words = trie.Find(['w', 'o']);
        Debug.Assert(words.Count() == 2);
        words = trie.Find([null, 'o']);
        Debug.Assert(words.Count() == 3);
        words = trie.Find([null, 'o', null, 'o']);
        Debug.Assert(words.Count() == 1);
    }

    /// <summary>
    /// Creates a <see cref="Trie"/> with 5 test values.
    /// </summary>
    /// <returns>A <see cref="Trie"/></returns>
    private static Trie Create()
    {
        var tree = new Trie();

        tree.Add("woord");
        tree.Add("woorden");
        tree.Add("zijn");
        tree.Add("wapens");
        tree.Add("logos");

        return tree;
    }
}

[TestClass]
public class TestTrieMap
{
    [TestMethod]
    public void CreateAndValidate()
    {
        var trie = Create();
        var totalWords = trie.NumWords;        

        Debug.Assert(totalWords == 5);
    }

    [TestMethod]
    public void Traversal()
    {
        var trie = Create();
        var values = trie.GetValues(null).ToList();
        var blExistsVal = trie.Exists(3); // should exist

        Debug.Assert(values.Count == 5 && blExistsVal);

        blExistsVal = trie.Exists(123); // should not exist
        Debug.Assert(!blExistsVal);

        var value = trie.GetValue("zijn");
        Debug.Assert(value == 3);

        var word = trie.GetWord(3);
        Debug.Assert(word == "zijn");
    }

    [TestMethod]
    public void Find()
    {
        var trie = Create();
        IEnumerable<int> values;

        values = trie.FindValue(['w']);
        Debug.Assert(values.Count() == 3);
        values = trie.FindValue(['w', 'o']);
        Debug.Assert(values.Count() == 2);
        values = trie.FindValue([null, 'o']);
        Debug.Assert(values.Count() == 3);
        values = trie.FindValue([null, 'o', null, 'o']);
        Debug.Assert(values.Count() == 1);
    }

    /// <summary>
    /// Creates a <see cref="TrieMap{Int32}"/> with 5 test values.
    /// </summary>
    /// <returns>A <see cref="TrieMap{Int32}"/></returns>
    private static TrieMap<MapNode<int>, int> Create()
    {
        var tree = new TrieMap<MapNode<int>, int>();

        tree.Add("woord", 1);
        tree.Add("woorden", 2);
        tree.Add("zijn", 3);
        tree.Add("wapens", 4);
        tree.Add("logos", 5);

        return tree;
    }
}

[TestClass]
public class ExtensionsTests
{
    [TestMethod]
    public void GuidConversion()
    {
        var guid = Guid.NewGuid();
        var stringified = guid.ToWord();
        var reconstituded = stringified.ToGuid();

        Debug.Assert(guid == reconstituded);
    }
}