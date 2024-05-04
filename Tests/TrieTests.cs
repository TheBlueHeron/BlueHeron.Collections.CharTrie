using System.Text.Json;
using BlueHeron.Collections.Trie.Search;

namespace BlueHeron.Collections.Trie.Tests;

[TestClass]
public class A_TrieTests
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

        Assert.IsTrue(totalWords == 6 && blExistsWoo && numWords == 2 && !woordeIsWord);
    }

    [TestMethod]
    public void Removal()
    {
        var trie = Create();

        trie.RemovePrefix("woo");
        Assert.IsTrue(trie.NumWords == 4);
        trie.RemoveWord("wapens");
        Assert.IsTrue(trie.NumWords == 3);
    }

    [TestMethod]
    public void Traversal()
    {
        var trie =  Create();
        var words = trie.Find(string.Empty,true); // find all

        Assert.IsTrue(words != null && words.ToList().Count == 6);

        words = trie.Find("woord", true);
        Assert.IsTrue(words != null && words.ToList().Count == 2);
    }

    [TestMethod]
    public void Find()
    {
        var trie = Create();
        IEnumerable<string> words;
        
        words = trie.Find("oo", false); // same as string.Contains("oo")
        Assert.IsTrue(words.Count() == 3);
        words = trie.Find("ord", false);
        Assert.IsTrue(words.Count() == 3);
    }

    [TestMethod]
    public void Serialization()
    {
        var trie = Create();
        var json = JsonSerializer.Serialize(trie);

        Assert.IsTrue(!string.IsNullOrEmpty(json));
        var reconstituted = JsonSerializer.Deserialize<Trie>(json);
        Assert.IsTrue(reconstituted != null && reconstituted.NumWords == trie.NumWords);
        Assert.IsTrue(reconstituted.Find("w", true).ToList().Count == 3);
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
    public static Trie Create()
    {
        var tree = new Trie();

        tree.Add("woord");
        tree.Add("woorden");
        tree.Add("zijn");
        tree.Add("wapens");
        tree.Add("logos");
        tree.Add("lustoord");

        return tree;
    }
}

[TestClass]
public class B_TrieMapTests
{
    [TestMethod]
    public void CreateAndValidate()
    {
        var trie = Create();
        var totalWords = trie.NumWords;        

        Assert.IsTrue(totalWords == 6);
    }

    [TestMethod]
    public void Traversal()
    {
        var trie = Create();
        var values = trie.FindValues(string.Empty, true).ToList(); // find all
        var blExistsVal = trie.Exists(3.0f); // should exist

        Assert.IsTrue(values.Count == 6 && blExistsVal);

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

        values = trie.FindValues("oo", false);
        Assert.IsTrue(values.Count() == 3);
        values = trie.FindValues("ord", false);
        Assert.IsTrue(values.Count() == 3);
    }

    [TestMethod]
    public void Serialization()
    {
        var trie = Create();
        var json = JsonSerializer.Serialize(trie);

        Assert.IsTrue(!string.IsNullOrEmpty(json));
        var reconstituted = JsonSerializer.Deserialize<Trie>(json);
        Assert.IsTrue(reconstituted != null && reconstituted.NumWords == trie.NumWords);
        Assert.IsTrue(reconstituted.Find("w", true).ToList().Count == 3);
        
        var node = trie.GetNode("logos");

        Assert.IsFalse(node is null || node.Value is null);
        
        var d = (dynamic)node.Value;
        Assert.IsTrue(d.PropertyA == true && d.PropertyB == 3.1415);
    }

    /// <summary>
    /// Creates a <see cref="TrieMap{Int32}"/> with 5 test values.
    /// </summary>
    /// <returns>A <see cref="TrieMap{Int32}"/></returns>
    public static Trie Create()
    {
        var tree = new Trie();

        tree.Add("woord", 1); // typeindex -> 0
        tree.Add("woorden", 2.14); // typeindex -> 1
        tree.Add("zijn", 3.0f); // typeindex -> 2
        tree.Add("wapens", DateTime.Now); // typeindex -> 3
        tree.Add("logos", new {PropertyA = true, PropertyB = 3.1415 }); // typeindex -> 4
        tree.Add("lustoord", 7); // typeindex -> 0

        return tree;
    }
}

[TestClass]
public class C_PatternMatchTests
{
    [TestMethod]
    public void PrefixMatching()
    {
        var trie = A_TrieTests.Create();

        if (trie != null)
        {
            IEnumerable<string> words;
            var prefixPattern = new PatternMatch
            {
                new CharMatch('w') // Default: { Type = CharMatchType.First } -> same as prefix 'w'
            }; // Default: { Type = PatternMatchType.IsPrefix };
            words = trie.Find(prefixPattern);
            Assert.IsTrue(words.Count() == 3);
            prefixPattern.Add('o'); // same as prefix 'wo'; using PatternMatch.Add(...) convenience method
            words = trie.Find(prefixPattern);
            Assert.IsTrue(words.Count() == 2);
            prefixPattern.Clear();
            prefixPattern.AddRange([CharMatch.Wildcard, new CharMatch('o')]); // where second letter is an 'o'
            words = trie.Find(prefixPattern);
            Assert.IsTrue(words.Count() == 3);
            prefixPattern.AddRange([CharMatch.Wildcard, new CharMatch('o')]); // where second and fourth letter is an 'o'
            words = trie.Find(prefixPattern);
            Assert.IsTrue(words.Count() == 1);
            prefixPattern.Type = PatternMatchType.IsWord;
            words = trie.Find(prefixPattern); // where second and fourth letter is an 'o' and word is 4 letters long
            Assert.IsFalse(words.Any());
            prefixPattern.Add(CharMatch.Wildcard);
            words = trie.Find(prefixPattern); // where second and fourth letter is an 'o' and word is 5 letters long
            Assert.IsTrue(words.Count() == 1); // logos :)
        }
    }

    [TestMethod]
    public void PrefixMatchingWithValues()
    {
        var trie = B_TrieMapTests.Create();

        if (trie != null)
        {
            IEnumerable<object?> values;
            var prefixPattern = new PatternMatch
            {
                new CharMatch('w') // Default: { Type = CharMatchType.First } -> same as prefix 'w'
            }; // Default: { Type = PatternMatchType.IsPrefix };
            values = trie.FindValues(prefixPattern);
            Assert.IsTrue(values.Count() == 3);
            prefixPattern.Add('o'); // same as prefix 'wo'; using PatternMatch.Add(...) convenience method
            values = trie.FindValues(prefixPattern);
            Assert.IsTrue(values.Count() == 2);
            prefixPattern.Clear();
            prefixPattern.AddRange([CharMatch.Wildcard, new CharMatch('o')]); // where second letter is an 'o'
            values = trie.FindValues(prefixPattern);
            Assert.IsTrue(values.Count() == 3);
            prefixPattern.AddRange([CharMatch.Wildcard, new CharMatch('o')]); // where second and fourth letter is an 'o'
            values = trie.FindValues(prefixPattern);
            Assert.IsTrue(values.Count() == 1);
            prefixPattern.Type = PatternMatchType.IsWord;
            values = trie.FindValues(prefixPattern); // where second and fourth letter is an 'o' and word is 4 letters long
            Assert.IsFalse(values.Any());
            prefixPattern.Add(CharMatch.Wildcard);
            values = trie.Find(prefixPattern); // where second and fourth letter is an 'o' and word is 5 letters long
            Assert.IsTrue(values.Count() == 1); // logos :)
        }
    }

    [TestMethod]
    public void WordMatching()
    {
        IEnumerable<string> words;
        var trie = A_TrieTests.Create();

        if (trie != null)
        {
            var woordPattern = new PatternMatch([
                new CharMatch('w'),
                CharMatch.Wildcard,
                CharMatch.Wildcard,
                CharMatch.Wildcard,
                new CharMatch('d')
                ], PatternMatchType.IsWord); // all words that start with 'w', end with 'd' and are 5 letters long

            words = trie.Find(woordPattern);
            Assert.IsTrue(words.Count() == 1); // 'woord' and not 'woordEN' or 'wAPENS'

            var zijnPattern = new PatternMatch([
                CharMatch.Wildcard,
                CharMatch.Wildcard,
                CharMatch.Wildcard,
                new CharMatch('n')
                ], PatternMatchType.IsWord); // all 4 letter words that end with 'n'

            words = trie.Find(zijnPattern);
            Assert.IsTrue(words.Count() == 1); // 'zijN' and not 'woordeN'
        }
    }

    [TestMethod]
    public void WordMatchingWithValues()
    {
        var trie = B_TrieMapTests.Create();

        if (trie != null)
        {
            IEnumerable<object?> values;
            var wordPattern = new PatternMatch() { Type = PatternMatchType.IsWord };

            wordPattern.AddRange([CharMatch.Wildcard, new CharMatch('o'), CharMatch.Wildcard, new CharMatch('o')]); // where second and fourth letter is an 'o'
            values = trie.FindValues(wordPattern); // where second and fourth letter is an 'o' and word is 4 letters long
            Assert.IsFalse(values.Any());
            wordPattern.Add(CharMatch.Wildcard);
            values = trie.Find(wordPattern); // where second and fourth letter is an 'o' and word is 5 letters long
            Assert.IsTrue(values.Count() == 1); // logos :)
        }
    }

    [TestMethod]
    public void FragmentMatching()
    {
        IEnumerable<string> words;
        var trie = A_TrieTests.Create();

        if (trie != null)
        {
            var oordPattern = new PatternMatch([
                new CharMatch('o'),
                new CharMatch('o'),
                new CharMatch('r'),
                new CharMatch('d')
                ], PatternMatchType.IsFragment); // all words that contain 'oord'

            words = trie.Find(oordPattern);
            Assert.IsTrue(words.Count() == 3);

            var nPattern = new PatternMatch([
                new CharMatch('n')
                ], PatternMatchType.IsFragment); // all words that contain an 'n'

            words = trie.Find(nPattern);
            Assert.IsTrue(words.Count() == 3); // 'woordeN', 'zijN', 'wapeNs'

            var us_oPattern = new PatternMatch([
                new CharMatch('u'),
                new CharMatch('s'),
                CharMatch.Wildcard,
                new CharMatch('o')
                ], PatternMatchType.IsFragment); // all words that contain 'us*o'

            words = trie.Find(us_oPattern);
            Assert.IsTrue(words.Count() == 1); // 'lUStOord'
        }
    }

    [TestMethod]
    public void FragmentMatchingWithValues()
    {
        var trie = B_TrieMapTests.Create();

        if (trie != null)
        {
            IEnumerable<object?> values;
            // all words that contain the pattern 'o*d' -> 'woord', 'woorden', 'lustoord'
            var fragmentPattern = new PatternMatch([new CharMatch('o'), CharMatch.Wildcard, new CharMatch('d')], PatternMatchType.IsFragment);

            values = trie.FindValues(fragmentPattern);
            Assert.IsTrue(values.Count() == 3);
        }
    }

    [TestMethod]
    public void Serialization()
    {
        var pattern = new PatternMatch { Type = PatternMatchType.IsWord };

        pattern.Add(CharMatch.Wildcard);
        pattern.Add('q');
        pattern.Add('a', ['á', 'à', 'ä']);
        
        var json = JsonSerializer.Serialize(pattern);
        var reconstituded = JsonSerializer.Deserialize<PatternMatch>(json);

        Assert.IsNotNull(reconstituded);
        Assert.IsTrue(pattern.Count == reconstituded.Count);
        Assert.IsTrue(reconstituded[0].Primary == null);
        Assert.IsTrue(reconstituded[1].Primary == pattern[1].Primary);
        Assert.IsTrue(reconstituded[2].Alternatives?.Length == pattern[2].Alternatives?.Length && reconstituded[2].Alternatives?[0] == pattern[2].Alternatives?[0] && reconstituded[2].Alternatives?[1] == pattern[2].Alternatives?[1] && reconstituded[2].Alternatives?[2] == pattern[2].Alternatives?[2]);
    }

}

[TestClass]
public class D_ExtensionsTests
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

[TestClass]
public class E_BenchMarking
{
}