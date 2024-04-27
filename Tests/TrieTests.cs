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

        Assert.IsTrue(totalWords == 5 && blExistsWoo && numWords == 2 && !woordeIsWord);
    }

    [TestMethod]
    public void Removal()
    {
        var trie = Create();

        trie.RemovePrefix("woo");
        Assert.IsTrue(trie.NumWords == 3);
        trie.RemoveWord("wapens");
        Assert.IsTrue(trie.NumWords == 2);
    }

    [TestMethod]
    public void Traversal()
    {
        var trie =  Create();
        var words = trie.Find((string?)null);

        Assert.IsTrue(words != null && words.ToList().Count == 5);

        words = trie.Find("woord");
        Assert.IsTrue(words != null && words.ToList().Count == 2);
    }

    [TestMethod]
    public void Find()
    {
        var trie = Create();
        IEnumerable<string> words;
        
        words = trie.Find(['w']); // same as prefix 'w'
        Assert.IsTrue(words.Count() == 3);
        words = trie.Find(['w', 'o']); // same as prefix 'wo'
        Assert.IsTrue(words.Count() == 2);
        words = trie.Find([null, 'o']); // where second letter is an 'o'
        Assert.IsTrue(words.Count() == 3);
        words = trie.Find([null, 'o', null, 'o']); // where second and fourth letter is an 'o'
        Assert.IsTrue(words.Count() == 1);
        words = trie.Find([null, 'o', null, 'o'], true); // where second and fourth letter is an 'o' and word is 4 letters long
        Assert.IsFalse(words.Any());
        words = trie.Find([null, 'o', null, 'o', null], true); // where second and fourth letter is an 'o' and word is 5 letters long
        Assert.IsTrue(words.Count() == 1);
        words = trie.FindContaining("oo"); // same as string.Contains("oo")
        Assert.IsTrue(words.Count() == 2);
        words = trie.FindContaining("ord"); // first 'o' is a 'false' match ('wOord', 'wOorden'), but should not be a problem
        Assert.IsTrue(words.Count() == 2);
    }

    [TestMethod]
    public void Serialization()
    {
        var trie = Create();
        var json = JsonSerializer.Serialize(trie);

        Assert.IsTrue(!string.IsNullOrEmpty(json));
        var reconstituted = JsonSerializer.Deserialize<Trie>(json);
        Assert.IsTrue(reconstituted != null && reconstituted.NumWords == trie.NumWords);
        Assert.IsTrue(reconstituted.Find("w").ToList().Count == 3);
    }

    [TestMethod]
    public async Task ImportExport()
    {
        var trie = await Trie.ImportAsync(new FileInfo("dictionaries\\nl.dic"));

        Assert.IsTrue(trie != null && trie.NumWords == 374622);
        
        Assert.IsTrue(await trie.ExportAsync("dictionaries\\nl.json"));
    }

    [TestMethod]
    public async Task Load()
    {
        var trie = await Trie.LoadAsync(new FileInfo("dictionaries\\nl.json"));

        Assert.IsTrue(trie != null && trie.NumWords == 374622);
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

        Assert.IsTrue(totalWords == 5);
    }

    [TestMethod]
    public void Traversal()
    {
        var trie = Create();
        var values = trie.FindValues(null).ToList();
        var blExistsVal = trie.Exists(3.0f); // should exist

        Assert.IsTrue(values.Count == 5 && blExistsVal);

        blExistsVal = trie.Exists(123); // should not exist
        Assert.IsFalse(blExistsVal);

        var value = trie.FindValue("zijn");
        Assert.IsTrue(value != null && value.Equals(3.0f));

        var word = trie.GetWord(3.0f);
        Assert.IsTrue(word == "zijn");

        word = trie.GetWord(3); // Int32: not equal
        Assert.IsTrue(word is null);
    }

    [TestMethod]
    public void Find()
    {
        var trie = Create();
        IEnumerable<object?> values;

        values = trie.FindValues(['w']); // same as prefix 'w'
        Assert.IsTrue(values.Count() == 3);
        values = trie.FindValues(['w', 'o']); // same as prefix 'wo'
        Assert.IsTrue(values.Count() == 2);
        values = trie.FindValues([null, 'o']); // where second letter is an 'o'
        Assert.IsTrue(values.Count() == 3);
        values = trie.FindValues([null, 'o', null, 'o']); // where second and fourth letter is an 'o'
        Assert.IsTrue(values.Count() == 1);
        values = trie.FindValuesContaining("oo"); // same as string.Contains("oo")
        Assert.IsTrue(values.Count() == 2);
        values = trie.FindValuesContaining("ord"); // first 'o' is a 'false' match ('wOord', 'wOorden'), but should not be a problem
        Assert.IsTrue(values.Count() == 2);
    }

    [TestMethod]
    public void Serialization()
    {
        var trie = Create();
        var json = JsonSerializer.Serialize(trie);

        Assert.IsTrue(!string.IsNullOrEmpty(json));
        var reconstituted = JsonSerializer.Deserialize<Trie>(json);
        Assert.IsTrue(reconstituted != null && reconstituted.NumWords == trie.NumWords);
        Assert.IsTrue(reconstituted.Find("w").ToList().Count == 3);
        
        var node = trie.GetNode("logos");

        Assert.IsFalse(node is null || node.Value is null);
        
        var d = (dynamic)node.Value;
        Assert.IsTrue(d.PropertyA == true && d.PropertyB == 3.1415);
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

        Assert.IsTrue(guid == reconstituded);
    }
}