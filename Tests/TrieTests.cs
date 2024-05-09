using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        var blExistsWoo = trie.Contains("woo", true);

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

        trie.Remove("woo", true);
        Assert.IsTrue(trie.NumWords == 4);
        trie.Remove("wapens", false);
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
        var blExistsVal = trie.ContainsValue(3.0f); // should exist

        Assert.IsTrue(values.Count == 6 && blExistsVal);

        blExistsVal = trie.ContainsValue(123); // should not exist
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
    #region Constants

    private const string fmtHeader = "|            Operation | # Runs | Minimum (µsec.) | Maximum (µsec.) | Average (µsec.) | Median (µsec.) |";
    private const string fmtRowBorder = "|----------------------|--------|-----------------|-----------------|-----------------|----------------|";
    private const string fmtRow = "| {0,20:###} | {1,6:###} | {2,15:##0.0} | {3,15:##0.0} | {4,15:##0.0} | {5,14:##0.0} |";

    #endregion

    [TestMethod]
    public async Task Run()
    {
        List<string> lstWords = [];
        List<string> lstTestWords = [];
        List<string> lstPrefixes = ["aan", "op", "in", "ver", "mee", "hoog", "laag", "tussen", "ter", "over"];
        List<char?[]> lstPatterns = [['o', 'r', 'd'], ['g', null, 's'], ['o', null, 'o']];

        BenchMarkResult bmListContains = new();
        BenchMarkResult bmTrieContains = new();
        BenchMarkResult bmListPattern = new();
        BenchMarkResult bmTriePattern = new();
        BenchMarkResult bmListPrefix = new();
        BenchMarkResult bmTriePrefix = new();

        Assert.IsTrue(File.Exists("dictionaries\\nl.json"));
        Assert.IsTrue(File.Exists("dictionaries\\nl.dic"));

        var trie = await Trie.LoadAsync(new FileInfo("dictionaries\\nl.json")); // create trie from export created earlier
        
        if (trie != null)
        {
            var reader = new FileInfo("dictionaries\\nl.dic").OpenText(); // load list of words
            string? line;

            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                if (line != null)
                {
                    lstWords.Add(line);
                }
            }
            for (var i = 10; i < lstWords.Count; i += 1000) // create test list (~ 375 words)
            {
                lstTestWords.Add(lstWords[i]);
            }

            lstTestWords = [..lstTestWords.OrderBy(x => Random.Shared.Next())]; // shuffle to improve median calculation accuracy

            foreach (var word in lstTestWords) // x.Contains(...) tests
            {
                var existsList = false;
                var existsTrie = false;

                bmListContains.AddResult(TestListContains(lstWords, word, ref existsList));
                bmTrieContains.AddResult(TestTrieContains(trie, word, ref existsTrie));

                Assert.IsTrue(existsList && existsTrie); // tegridy check
            }
            foreach (var pattern in lstPatterns) // pattern test
            {
                var numList = 0;
                var numTrie = 0;

                bmListPattern.AddResult(TestListContainsPattern(lstWords, pattern, ref numList));
                bmTriePattern.AddResult(TestTrieContainsPattern(trie, pattern, ref numTrie));

                Assert.IsTrue(numList <= numTrie); // tegridy check
            }
            foreach (var prefix in lstPrefixes) // prefix test
            {
                var numList = 0;
                var numTrie = 0;

                bmListPrefix.AddResult(TestListStartsWith(lstWords, prefix, ref numList));
                bmTriePrefix.AddResult(TestTrieStartsWith(trie, prefix, ref numTrie));

                Assert.IsTrue(numList == numTrie); // tegridy check
            }

            WriteBenchmarkHeader();
            WriteBenchmarkRow("List Contains", bmListContains);
            WriteBenchmarkRow("Trie Contains", bmTrieContains);
            WriteBenchmarkFooter();
            WriteBenchmarkRow("List Pattern", bmListPattern);
            WriteBenchmarkRow("Trie Find(pattern)", bmTriePattern);
            WriteBenchmarkFooter();
            WriteBenchmarkRow("List StartsWith", bmListPrefix);
            WriteBenchmarkRow("Trie Find(prefix)", bmTriePrefix);
            WriteBenchmarkFooter();
        }
    }

    /// <summary>
    /// Calls <see cref="List{string}.Contains(string)"/> and returns the duration of the call.
    /// </summary>
    /// <param name="list">The <see cref="List{string}"/> to use</param>
    /// <param name="word">The word to find</param>
    /// <param name="exists">Result of the function call</param>
    /// <returns>A <see cref="TimeSpan"/></returns>
    private static TimeSpan TestListContains(List<string> list, string word, ref bool exists)
    {
        var start = DateTime.Now;

        exists = list.Contains(word);

        return DateTime.Now - start; 
    }

    /// <summary>
    /// Calls <see cref="Regex.IsMatch(string)"/> on each item in the list and returns the duration of the call.
    /// </summary>
    /// <param name="list">The <see cref="List{string}"/> to use</param>
    /// <param name="pattern">The pattern to match</param>
    /// <param name="num">Result of the function call</param>
    /// <returns>A <see cref="TimeSpan"/></returns>
    private static TimeSpan TestListContainsPattern(List<string> list, IEnumerable<char?> pattern, ref int num)
    {
        var start = DateTime.Now;
        var match = pattern.ToRegex();
        var lst = list.Where(w => match.IsMatch(w)).ToList();

        num = lst.Count;

        return DateTime.Now - start;
    }

     /// <summary>
    /// Calls <see cref="List{string}"/>.Where(w => w.StartsWith(prefix, StringComparison.CurrentCulture)).ToList() and returns the duration of the call.
    /// </summary>
    /// <param name="list">The <see cref="List{string}"/> to use</param>
    /// <param name="prefix">The prefix to match</param>
    /// <param name="num">Result of the function call</param>
    /// <returns>A <see cref="TimeSpan"/></returns>
    private static TimeSpan TestListStartsWith(List<string> list, string prefix, ref int num)
    {
        var start = DateTime.Now;

        num = list.Where(w => w.StartsWith(prefix, StringComparison.CurrentCulture)).ToList().Count;

        return DateTime.Now - start;
    }

    /// <summary>
    /// Calls <see cref="Trie.Contains(string, bool)"/> and returns the duration of the call.
    /// </summary>
    /// <param name="trie">The <see cref="Trie"/> to use</param>
    /// <param name="word">The word to find</param>
    /// <param name="exists">Result of the function call</param>
    /// <returns>A <see cref="TimeSpan"/></returns>
    private static TimeSpan TestTrieContains(Trie trie, string word, ref bool exists)
    {
        var start = DateTime.Now;

        exists = trie.Contains(word, false);

        return DateTime.Now - start;
    }

    /// <summary>
    /// Calls <see cref="Trie.Find(PatternMatch)"/>.ToList() and returns the duration of the call.
    /// </summary>
    /// <param name="trie">The <see cref="Trie"/> to use</param>
    /// <param name="pattern">The pattern to match</param>
    /// <param name="num">Result of the function call</param>
    /// <returns>A <see cref="TimeSpan"/></returns>
    private static TimeSpan TestTrieContainsPattern(Trie trie, IEnumerable<char?> pattern, ref int num)
    {
        var start = DateTime.Now;
        var match = new PatternMatch(pattern, PatternMatchType.IsFragment);
        var lst = trie.Find(match).ToList();

        num = lst.Count;

        return DateTime.Now - start;
    }

    /// <summary>
    /// Calls <see cref="Trie.Find(string, bool)"/>.ToList() and returns the duration of the call.
    /// </summary>
    /// <param name="trie">The <see cref="Trie"/> to use</param>
    /// <param name="prefix">The prefix to match</param>
    /// <param name="num">Result of the function call</param>
    /// <returns>A <see cref="TimeSpan"/></returns>
    private static TimeSpan TestTrieStartsWith(Trie trie, string prefix, ref int num)
    {
        var start = DateTime.Now;

        num = trie.Find(prefix, true).ToList().Count;

        return DateTime.Now - start;
    }

    /// <summary>
    /// Outputs result table separator row to debug window.
    /// </summary>
    private static void WriteBenchmarkFooter()
    {
        Debug.WriteLine(fmtRowBorder);
    }

    /// <summary>
    /// Outputs result table column headers to debug window.
    /// </summary>
    private static void WriteBenchmarkHeader()
    {
        Debug.WriteLine(fmtRowBorder);
        Debug.WriteLine(fmtHeader);
        Debug.WriteLine(fmtRowBorder);
    }

    /// <summary>
    /// Outputs result table result row to debug window.
    /// </summary>
    private static void WriteBenchmarkRow(string testName, BenchMarkResult row)
    {
        Debug.WriteLine(fmtRow, testName, row.NumTests, row.MinDuration, row.MaxDuration, row.AverageDuration, row.MedianDuration);
    }
}