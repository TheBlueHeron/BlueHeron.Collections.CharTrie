using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using BlueHeron.Collections.Trie.Search;

namespace BlueHeron.Collections.Trie.Tests;

[TestClass]
public class A_TrieTests()
{
    private static CharTrie mTrie = null!;

    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task A_CreateAndValidate()
    {
        var trie = await Create();

        Assert.IsTrue(trie.Contains("logos"));
        Assert.IsFalse(trie.Contains("oneiros"));

        //var words = trie.Find("w");
        //Assert.HasCount(3, words);

        var allWords = trie.All().ToList();
        Assert.HasCount(6, allWords);
        Assert.AreEqual("logos", allWords[0]); // 'logos' should be first word as result of sorting the trie during finalization
    }

    [TestMethod]
    public async Task B_Serialization()
    {
        var trie = await Create();
        var json = JsonSerializer.Serialize(trie);

        Assert.IsFalse(string.IsNullOrEmpty(json));
        var reconstituted = JsonSerializer.Deserialize<CharTrie>(json);
        Assert.IsTrue(reconstituted != null && reconstituted.Count == trie.Count);

        var rw = reconstituted.All();
        Assert.HasCount(6, rw);
    }

    [TestMethod]
    public async Task C_ImportExport()
    {
        var trie = await CharTrieFactory.ImportAsync(new FileInfo("dictionaries\\nl.dic"));
        //using var stream = File.OpenRead("dictionaries\\nl.dic");
        //var trie = await CharTrieFactory.ImportAsync(stream);

        Assert.IsTrue(trie != null && trie.Count == 343075);

        trie.Prune();
        Assert.IsTrue(await CharTrieFactory.ExportAsync(trie, "dictionaries\\nl_2.json"));

        //var memStream = await CharTrieFactory.ExportAsync(trie);
        //Assert.IsNotNull(memStream);
        //Assert.IsTrue(memStream.CanRead);
    }

    [TestMethod]
    public async Task D_Load()
    {
        //using var stream = File.OpenRead("dictionaries\\nl_2.json");
        //var trie = await CharTrieFactory.LoadAsync(stream);
        var trie = await CharTrieFactory.LoadAsync(new FileInfo("dictionaries\\nl_2.json"));

        Assert.IsTrue(trie != null && trie.Count == 343075);
    }

    /// <summary>
    /// Creates a <see cref="CharTrie"/> with 6 test values.
    /// </summary>
    /// <returns>A <see cref="CharTrie"/></returns>
    internal static async Task<CharTrie> Create()
    {
        if (mTrie is null)
        {
            var factory = await CharTrieFactory.FromDictionary("dictionaries\\nl.dic");

            mTrie = factory.Create();
            mTrie.Add("woord");
            mTrie.Add("woorden");
            mTrie.Add("zijn");
            mTrie.Add("wapens");
            mTrie.Add("logos");
            mTrie.Add("lustoord");
            mTrie.Prune(true);
        }
        return mTrie;
    }
}

[TestClass]
public class B_PatternMatchTests
{
    [TestMethod]
    public async Task PrefixMatching()
    {
        var trie = await A_TrieTests.Create();

        if (trie != null)
        {
            IEnumerable<string> words;
            var prefixPattern = new PatternMatch
            {
                new CharMatch('w') // Default: { MatchType = CharMatchType.First } -> same as prefix 'w'
            }; // Default: { MatchType = PatternMatchType.IsPrefix };
            words = trie.Find(prefixPattern);
            Assert.AreEqual(3, words.Count());
            prefixPattern.Add('o'); // same as prefix 'wo'; using PatternMatch.Add(...) convenience method
            words = trie.Find(prefixPattern);
            Assert.AreEqual(2, words.Count());
            prefixPattern.Clear();
            prefixPattern.AddRange([CharMatch.Wildcard, new CharMatch('o')]); // where second letter is an 'o'
            words = trie.Find(prefixPattern);
            Assert.AreEqual(3, words.Count());
            prefixPattern.AddRange([CharMatch.Wildcard, new CharMatch('o')]); // where second and fourth letter is an 'o'
            words = trie.Find(prefixPattern);
            Assert.AreEqual(1, words.Count());
            prefixPattern.MatchType = PatternMatchType.IsWord;
            words = trie.Find(prefixPattern); // where second and fourth letter is an 'o' and word is 4 letters long
            Assert.IsFalse(words.Any());
            prefixPattern.Add(CharMatch.Wildcard);
            words = trie.Find(prefixPattern); // where second and fourth letter is an 'o' and word is 5 letters long
            Assert.AreEqual(1, words.Count()); // logos :)
        }
    }

    [TestMethod]
    public async Task WordMatching()
    {
        IEnumerable<string> words;
        var trie = await A_TrieTests.Create();

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
            Assert.AreEqual(1, words.Count()); // 'woord' and not 'woordEN' or 'wAPENS'

            var zijnPattern = new PatternMatch([
                CharMatch.Wildcard,
                CharMatch.Wildcard,
                CharMatch.Wildcard,
                new CharMatch('n')
                ], PatternMatchType.IsWord); // all 4 letter words that end with 'n'

            words = trie.Find(zijnPattern);
            Assert.AreEqual(1, words.Count()); // 'zijN' and not 'woordeN'
        }
    }

    [TestMethod]
    public async Task FragmentMatching()
    {
        IEnumerable<string> words;
        var trie = await A_TrieTests.Create();

        if (trie != null)
        {
            var oordPattern = new PatternMatch([
                new CharMatch('o'),
                new CharMatch('o'),
                new CharMatch('r'),
                new CharMatch('d')
                ], PatternMatchType.IsFragment); // all words that contain 'oord'

            words = trie.Find(oordPattern);
            Assert.AreEqual(3, words.Count()); // wOORD, wOORDen, lustOORD

            var nPattern = new PatternMatch([
                new CharMatch('n')
                ], PatternMatchType.IsFragment); // all words that contain an 'n'

            words = trie.Find(nPattern);
            Assert.AreEqual(3, words.Count()); // 'woordeN', 'zijN', 'wapeNs'

            var us_oPattern = new PatternMatch([
                new CharMatch('u'),
                new CharMatch('s'),
                CharMatch.Wildcard,
                new CharMatch('o')
                ], PatternMatchType.IsFragment); // all words that contain 'us*o'

            words = trie.Find(us_oPattern);
            Assert.AreEqual(1, words.Count()); // 'lUStOord'

            var fragmentPattern = new PatternMatch([
                new CharMatch('o'),
                CharMatch.Wildcard,
                new CharMatch('d')
                ], PatternMatchType.IsFragment); // o*d

            words = trie.Find(fragmentPattern);
            Assert.AreEqual(3, words.Count()); // wOORD, wOORDen, lustOORD
        }
    }

    [TestMethod]
    public async Task SuffixMatching()
    {
        IEnumerable<string> words;
        var trie = await A_TrieTests.Create();

        var nPattern = new PatternMatch([
                new CharMatch('n')
                ], PatternMatchType.IsSuffix); // all words that end with an 'n'

        words = trie.Find(nPattern);
        Assert.AreEqual(2, words.Count()); // 'woordeN', 'zijN'

        var i_nPattern = new PatternMatch([
                new CharMatch('i'),
                CharMatch.Wildcard,
                new CharMatch('n')
                ], PatternMatchType.IsSuffix); // all words that end with 'i*n'

        words = trie.Find(i_nPattern);
        Assert.AreEqual(1, words.Count()); // 'zijN'
    }

    [TestMethod]
    public void Serialization()
    {
        var pattern = new PatternMatch { MatchType = PatternMatchType.IsWord };

        pattern.Add(CharMatch.Wildcard);
        pattern.Add('q');
        pattern.Add('a', ['á', 'à', 'ä']);

        var json = JsonSerializer.Serialize(pattern);
        var reconstituded = JsonSerializer.Deserialize<PatternMatch>(json);

        Assert.IsNotNull(reconstituded);
        Assert.HasCount(reconstituded.Count, pattern);
        Assert.IsNull(reconstituded[0].Primary);
        Assert.AreEqual(pattern[1].Primary, reconstituded[1].Primary);
        Assert.IsTrue(reconstituded[2].Alternatives?.Count == pattern[2].Alternatives?.Count && reconstituded[2].Alternatives?[0] == pattern[2].Alternatives?[0] && reconstituded[2].Alternatives?[1] == pattern[2].Alternatives?[1] && reconstituded[2].Alternatives?[2] == pattern[2].Alternatives?[2]);
    }

    [TestMethod]
    public async Task Z_Pitfalls()
    {
        var factory = await CharTrieFactory.FromDictionary("dictionaries\\nl.dic");
        List<string> words;
        var tree = factory.Create();

        tree.Add("os");
        tree.Add("orakel");
        tree.Add("ordeverstoorders");
        tree.Add("ordewacht");
        tree.Add("ordewoord");
        tree.Add("ordewoorden");
        tree.Add("woordvolgorde");
        tree.Add("woordje");
        tree.Prune();

        words = [.. tree.Find(PatternMatch.FromFragment("ord"))];  // the children of 'o' or its child 'r' should not be visited twice
        Assert.HasCount(6, words);

        tree = factory.Create();
        tree.AddRange(["ges", "gres", "grges"]);
        tree.Prune();
        words = [.. tree.Find(PatternMatch.FromFragment("ges"))]; // should move past false matches
        Assert.HasCount(2, words);
    }
}

[TestClass]
public class C_ExtensionsTests
{
    [TestMethod]
    public void GuidConversion()
    {
        var guid = Guid.NewGuid();
        var stringified = guid.ToWord();
        var reconstituded = stringified.ToGuid();

        Assert.AreEqual(guid, reconstituded);
    }
}

[TestClass]
public class D_BenchMarking
{
    #region Constants

    private const string fmtMemoryHeader = "| Object |    # Nodes |         Size |";
    private const string fmtMemoryRowBorder = "|--------|------------|--------------|";
    private const string fmtMemoryRow = "| {0,6:###} | {1,10:###} | {2,10:###} B |";
    private const string fmtSpeedHeader = "|            Operation | # Runs | Minimum (µsec.) | Maximum (µsec.) | Average (µsec.) | Median (µsec.) |";
    private const string fmtSpeedRowBorder = "|----------------------|--------|-----------------|-----------------|-----------------|----------------|";
    private const string fmtSpeedRow = "| {0,20:###} | {1,6:###} | {2,15:##0.0} | {3,15:##0.0} | {4,15:##0.0} | {5,14:##0.0} |";

    #endregion

    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task Run()
    { // TODO: add suffix benchmark
        List<string> lstWords = [];
        List<string> lstTestWords = [];
        List<string> lstPrefixes = ["aan", "op", "in", "ver", "mee", "hoog", "laag", "tussen", "ter", "over"]; // high prevalence prefixes (in dutch anyway)
        List<char?[]> lstPatterns = [['o', 'r', 'd'], ['g', 'e', 's'], ['o', null, 'o']];

        BenchMarkResult bmListContains = new();
        BenchMarkResult bmCharTrieContains = new();
        BenchMarkResult bmListPattern = new();
        BenchMarkResult bmCharTriePattern = new();
        BenchMarkResult bmListPrefix = new();
        BenchMarkResult bmCharTriePrefix = new();

        Assert.IsTrue(File.Exists("dictionaries\\nl.json"));
        Assert.IsTrue(File.Exists("dictionaries\\nl_2.json"));
        Assert.IsTrue(File.Exists("dictionaries\\nl.dic"));

        GC.GetTotalMemory(true);
        var startMemory = GC.GetTotalAllocatedBytes(true);
        var charTrie = await CharTrieFactory.LoadAsync(new FileInfo("dictionaries\\nl_2.json")); // create trie from export created earlier
        GC.Collect();
        GC.GetTotalMemory(true);
        var charTrieMem = GC.GetTotalAllocatedBytes(true) - startMemory;

        if (charTrie != null)
        {
            GC.GetTotalMemory(true);
            startMemory = GC.GetTotalAllocatedBytes(true);

            using var reader = new FileInfo("dictionaries\\nl.dic").OpenText(); // load list of words
            string? line;

            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                if (line != null)
                {
                    lstWords.Add(line);
                }
            }
            GC.Collect();
            GC.GetTotalMemory(true);
            var listMem = GC.GetTotalAllocatedBytes(true) - startMemory;

            // output memory benchmark results
            WriteMemoryBenchmarkHeader();
            WriteMemoryBenchmarkRow("List", lstWords.Count, listMem);
            WriteMemoryBenchmarkBorder();
            WriteMemoryBenchmarkRow("ChTr", charTrie.NumNodes, charTrieMem);
            WriteMemoryBenchmarkBorder();
            Debug.WriteLine(string.Empty);

            for (var i = 10; i < lstWords.Count; i += 1000) // create test list (~ 344 words)
            {
                lstTestWords.Add(lstWords[i]);
            }

            lstTestWords = [..lstTestWords.OrderBy(x => Random.Shared.Next())]; // shuffle to improve median calculation accuracy

            foreach (var word in lstTestWords) // x.Contains(...) tests
            {
                var existsList = false;
                var existsCharTrie = false;

                bmListContains.AddResult(TestListContains(lstWords, word, ref existsList));
                bmCharTrieContains.AddResult(TestCharTrieContains(charTrie, word, ref existsCharTrie));

                Assert.IsTrue(existsList && existsCharTrie); // tegridy check
            }
            foreach (var prefix in lstPrefixes) // prefix test
            {
                var numList = 0;
                var numCharTrie = 0;

                bmListPrefix.AddResult(TestListStartsWith(lstWords, prefix, ref numList));
                bmCharTriePrefix.AddResult(TestCharTrieStartsWith(charTrie, prefix, ref numCharTrie));

                Assert.AreEqual(numCharTrie, numList); // tegridy check
            }
            foreach (var pattern in lstPatterns) // pattern test
            {
                var numList = 0;
                var numCharTrie = 0;

                bmListPattern.AddResult(TestListContainsPattern(lstWords, pattern, ref numList));
                bmCharTriePattern.AddResult(TestCharTrieContainsPattern(charTrie, pattern, ref numCharTrie));

                Assert.AreEqual(numCharTrie, numList); // tegridy check -> OK
            }
            // output speed benchmark results
            WriteSpeedBenchmarkHeader();
            WriteSpeedBenchmarkRow("List Contains", bmListContains);
            WriteSpeedBenchmarkRow("ChTr Contains", bmCharTrieContains);
            WriteSpeedBenchmarkBorder();
            WriteSpeedBenchmarkRow("List StartsWith", bmListPrefix);
            WriteSpeedBenchmarkRow("ChTr Find(prefix)", bmCharTriePrefix);
            WriteSpeedBenchmarkBorder();
            WriteSpeedBenchmarkRow("List Regex", bmListPattern);
            WriteSpeedBenchmarkRow("ChTr Find(pattern)", bmCharTriePattern);
            WriteSpeedBenchmarkBorder();
        }
    }

    #region Tests

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
        var lst = list.Where(w => match.IsMatch(w)).ToList(); // .Order()

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
    /// Calls <see cref="CharTrie.Contains(string, bool)"/> and returns the duration of the call.
    /// </summary>
    /// <param name="trie">The <see cref="CharTrie"/> to use</param>
    /// <param name="word">The word to find</param>
    /// <param name="exists">Result of the function call</param>
    /// <returns>A <see cref="TimeSpan"/></returns>
    private static TimeSpan TestCharTrieContains(CharTrie trie, string word, ref bool exists)
    {
        var start = DateTime.Now;

        exists = trie.Contains(word);

        return DateTime.Now - start;
    }

    /// <summary>
    /// Calls <see cref="CharTrie.Find(PatternMatch)"/>.ToList() and returns the duration of the call.
    /// </summary>
    /// <param name="trie">The <see cref="CharTrie"/> to use</param>
    /// <param name="pattern">The pattern to match</param>
    /// <param name="num">Result of the function call</param>
    /// <returns>A <see cref="TimeSpan"/></returns>
    private static TimeSpan TestCharTrieContainsPattern(CharTrie trie, IEnumerable<char?> pattern, ref int num)
    {
        var start = DateTime.Now;
        var match = new PatternMatch(pattern, PatternMatchType.IsFragment);

        num = trie.Find(match).ToList().Count;

        return DateTime.Now - start;
    }

    /// <summary>
    /// Calls <see cref="CharTrie.Find(string, bool)"/>.ToList() and returns the duration of the call.
    /// </summary>
    /// <param name="trie">The <see cref="CharTrie"/> to use</param>
    /// <param name="prefix">The prefix to match</param>
    /// <param name="num">Result of the function call</param>
    /// <returns>A <see cref="TimeSpan"/></returns>
    private static TimeSpan TestCharTrieStartsWith(CharTrie trie, string prefix, ref int num)
    {
        var start = DateTime.Now;
        var pattern = PatternMatch.FromPrefix(prefix);

        num = trie.Find(pattern).ToList().Count;

        return DateTime.Now - start;
    }

    #endregion

    #region Output

    /// <summary>
    /// Outputs result table separator row to TestContext window.
    /// </summary>
    private void WriteMemoryBenchmarkBorder()
    {
        TestContext.WriteLine(fmtMemoryRowBorder);
    }

    /// <summary>
    /// Outputs result table column headers to TestContext window.
    /// </summary>
    private void WriteMemoryBenchmarkHeader()
    {
        TestContext.WriteLine(fmtMemoryRowBorder);
        TestContext.WriteLine(fmtMemoryHeader);
        TestContext.WriteLine(fmtMemoryRowBorder);
    }

    /// <summary>
    /// Outputs result table result row to TestContext window.
    /// </summary>
    /// <param name="objectName">The name of the object</param>
    /// <param name="numNodes">The number of nodes in the object</param>
    /// <param name="memUsage">The memory usage in bytes</param>
    private void WriteMemoryBenchmarkRow(string objectName, int numNodes, long memUsage)
    {
        TestContext.WriteLine(fmtMemoryRow, objectName, numNodes, memUsage);
    }

    /// <summary>
    /// Outputs result table separator row to TestContext window.
    /// </summary>
    private void WriteSpeedBenchmarkBorder()
    {
        TestContext.WriteLine(fmtSpeedRowBorder);
    }

    /// <summary>
    /// Outputs result table column headers to TestContext window.
    /// </summary>
    private void WriteSpeedBenchmarkHeader()
    {
        TestContext.WriteLine(fmtSpeedRowBorder);
        TestContext.WriteLine(fmtSpeedHeader);
        TestContext.WriteLine(fmtSpeedRowBorder);
    }

    /// <summary>
    /// Outputs result table result row to TestContext window.
    /// </summary>
    private void WriteSpeedBenchmarkRow(string testName, BenchMarkResult row)
    {
        TestContext.WriteLine(fmtSpeedRow, testName, row.NumTests, row.MinDuration, row.MaxDuration, row.AverageDuration, row.MedianDuration);
    }

    #endregion
}