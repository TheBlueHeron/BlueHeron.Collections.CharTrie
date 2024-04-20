using System.Diagnostics;
using System.Text.Json;

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
        var words = trie.Find((string?)null);

        Debug.Assert(words != null && words.ToList().Count == 5);

        words = trie.Find("woord");
        Debug.Assert(words != null && words.ToList().Count == 2);
    }

    [TestMethod]
    public void Find()
    {
        var trie = Create();
        IEnumerable<string> words;
        
        words = trie.Find(['w']); // same as prefix 'w'
        Debug.Assert(words.Count() == 3);
        words = trie.Find(['w', 'o']); // same as prefix 'wo'
        Debug.Assert(words.Count() == 2);
        words = trie.Find([null, 'o']); // where second letter is an 'o'
        Debug.Assert(words.Count() == 3);
        words = trie.Find([null, 'o', null, 'o']); // where second and fourth letter is an 'o'
        Debug.Assert(words.Count() == 1);
        words = trie.Find([null, 'o', null, 'o'], true); // where second and fourth letter is an 'o' and word is 4 letters long
        Debug.Assert(!words.Any());
        words = trie.Find([null, 'o', null, 'o', null], true); // where second and fourth letter is an 'o' and word is 5 letters long
        Debug.Assert(words.Count() == 1);
        words = trie.FindContaining("oo"); // same as string.Contains("oo")
        Debug.Assert(words.Count() == 2);
        words = trie.FindContaining("ord"); // first 'o' is a 'false' match ('wOord', 'wOorden'), but should not be a problem
        Debug.Assert(words.Count() == 2);
    }

    [TestMethod]
    public void Serialization()
    {
        var trie = Create();
        var json = JsonSerializer.Serialize(trie);

        Debug.Assert(!string.IsNullOrEmpty(json));
        var reconstituted = JsonSerializer.Deserialize<Trie>(json);
        Debug.Assert(reconstituted != null && reconstituted.NumWords == trie.NumWords);
        Debug.Assert(reconstituted.Find("w").ToList().Count == 3);
    }

    [TestMethod]
    public async Task Import()
    {
        var trie = await Trie.ImportAsync(new FileInfo("Dictionaries\\nl.dic"));

        Debug.Assert(trie != null && trie.NumWords == 374622);
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
        var values = trie.FindValues(null).ToList();
        var blExistsVal = trie.Exists(3.0f); // should exist

        Debug.Assert(values.Count == 5 && blExistsVal);

        blExistsVal = trie.Exists(123); // should not exist
        Debug.Assert(!blExistsVal);

        var value = trie.FindValue("zijn");
        Debug.Assert(value != null && value.Equals(3.0f));

        var word = trie.GetWord(3.0f);
        Debug.Assert(word == "zijn");

        word = trie.GetWord(3); // Int32: not equal
        Debug.Assert(word is null);
    }

    [TestMethod]
    public void Find()
    {
        var trie = Create();
        IEnumerable<object?> values;

        values = trie.FindValues(['w']); // same as prefix 'w'
        Debug.Assert(values.Count() == 3);
        values = trie.FindValues(['w', 'o']); // same as prefix 'wo'
        Debug.Assert(values.Count() == 2);
        values = trie.FindValues([null, 'o']); // where second letter is an 'o'
        Debug.Assert(values.Count() == 3);
        values = trie.FindValues([null, 'o', null, 'o']); // where second and fourth letter is an 'o'
        Debug.Assert(values.Count() == 1);
        values = trie.FindValuesContaining("oo"); // same as string.Contains("oo")
        Debug.Assert(values.Count() == 2);
        values = trie.FindValuesContaining("ord"); // first 'o' is a 'false' match ('wOord', 'wOorden'), but should not be a problem
        Debug.Assert(values.Count() == 2);
    }

    [TestMethod]
    public void Serialization()
    {
        var trie = Create();
        var json = JsonSerializer.Serialize(trie);

        Debug.Assert(!string.IsNullOrEmpty(json));
        var reconstituted = JsonSerializer.Deserialize<Trie>(json);
        Debug.Assert(reconstituted != null && reconstituted.NumWords == trie.NumWords);
        Debug.Assert(reconstituted.Find("w").ToList().Count == 3);
        
        var node = trie.GetNode("logos");
        Debug.Assert(node != null && node.Value != null && ((dynamic)node.Value).PropertyA == true && ((dynamic)node.Value).PropertyB == 3.1415);
    }

    /// <summary>
    /// Creates a <see cref="TrieMap{Int32}"/> with 5 test values.
    /// </summary>
    /// <returns>A <see cref="TrieMap{Int32}"/></returns>
    private static Trie Create()
    {
        var tree = new Trie();

        tree.Add("woord", 1); // typeindex -> 0
        tree.Add("woorden", 2.14); // typeindex -> 1
        tree.Add("zijn", 3.0f); // typeindex -> 2
        tree.Add("wapens", DateTime.Now); // typeindex -> 3
        tree.Add("logos", new {PropertyA = true, PropertyB = 3.1415 }); // typeindex -> 4

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