using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace BlueHeron.Collections.Trie;

/// <summary>
/// Provides functionality to create, import, export and load <see cref="CharTrie"/> instances.
/// </summary>
public class CharTrieFactory
{
    #region Objects and variables

    private const string errEmpty = "The array must contain at least one character.";
    private const string errTooLarge = "The array cannot contain more than 255 characters.";
    private const string _Export = "export";
    private const string _Import = "import";

    private static readonly CompositeFormat errImpEx = CompositeFormat.Parse("Unable to {0} '{1}'. See inner exception for details.");
    private static readonly JsonSerializerOptions mSerializerOptions = new() { WriteIndented = false };
    
    private readonly char[] mCharacters;

    #endregion

    #region Construction

    /// <summary>
    /// Creates a new <see cref="CharTrieFactory"/>, using the given character set.
    /// </summary>
    /// <param name="characters">The characters to support. Must be less than 256 characters long</param>
    /// <exception cref="ArgumentNullException">The array is <see langword="null"/></exception>
    /// <exception cref="ArgumentException">The character array is empty</exception>
    /// <exception cref="NotSupportedException">The character array is longer than 255 characters</exception>
    public CharTrieFactory(char[] characters)
    {
        ArgumentNullException.ThrowIfNull(characters);
        if (characters.Length == 0)
        {
            throw new ArgumentException(errEmpty, nameof(characters));
        }
        if (characters.Length > 255)
        {
            throw new NotSupportedException(errTooLarge);
        }
        Array.Sort(characters);
        mCharacters = new char[1];
        mCharacters[0] = '\0'; // root character
        Array.Resize(ref mCharacters, characters.Length + 1);
        Array.Copy(characters, 0, mCharacters, 1, characters.Length);
    }

    /// <summary>
    /// Creates a new <see cref="CharTrieFactory"/> from the dictionary at the given location.
    /// The dictionary must consist of a list of words, one per line.
    /// </summary>
    /// <param name="path">The full path to the dictionary file</param>
    /// <returns></returns>
    public static async Task<CharTrieFactory> FromDictionary(string path)
    {
        var characters = new List<char>();

        using var reader = new FileInfo(path).OpenText();
        var curLine = await reader.ReadLineAsync().ConfigureAwait(false);

        while (curLine != null)
        {
            if (curLine.Length > 0)
            {
                foreach (var c in curLine)
                {
                    if (!characters.Contains(c))
                    {
                        characters.Add(c);
                        characters.Sort();
                    }
                }
            }
            curLine = await reader.ReadLineAsync().ConfigureAwait(false);
        }
        characters.Sort();
        return new CharTrieFactory([.. characters]);
    }

    #endregion

    #region Public methods and functions

    /// <summary>
    /// Returns a new, empty <see cref="CharTrie"/>.
    /// </summary>
    /// <returns>A <see cref="CharTrie"/> instance</returns>
    public CharTrie Create()
    {
        return new CharTrie(mCharacters);
    }

    /// <summary>
    /// Exports the given <see cref="CharTrie"/> to the file with the given name asynchronously.
    /// </summary>
    /// <param name="fileName">The full path and file name, including extension</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use</param>
    /// <returns>A Task, resulting in a <see cref="bool"/> that signifies the success of the operation</returns>
    /// <exception cref="InvalidOperationException">The file could not be created or written to</exception>
    public static async Task<bool> ExportAsync(CharTrie trie, string fileName, JsonSerializerOptions? options = null)
    {
        try
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                using var writer = File.CreateText(fileName);
                await writer.WriteAsync(JsonSerializer.Serialize(trie, options ?? mSerializerOptions)).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
                writer.Close();
                return true;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(string.Format(null, errImpEx, _Export, fileName), ex);
        }
        return false;
    }

    /// <summary>
    /// Serializes the given <see cref="CharTrie"/> asynchronously and returns it as a <see cref="Stream"/>.
    /// </summary>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use</param>
    /// <returns>A Task, resulting in a <see cref="bool"/> that signifies the success of the operation</returns>
    /// <exception cref="InvalidOperationException">The file could not be created or written to</exception>
    public static async Task<Stream> ExportAsync(CharTrie trie, JsonSerializerOptions? options = null)
    {
        try
        {
            var stream = new MemoryStream();

            using var writer = new StreamWriter(stream);
            await writer.WriteAsync(JsonSerializer.Serialize(trie, options ?? mSerializerOptions)).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
            writer.Close();
            stream.Position = 0;
            return stream;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(string.Format(null, errImpEx, _Export, nameof(trie)), ex);
        }
    }

    /// <summary>
    /// Creates a new <see cref="CharTrie"/> and tries to import all words in the given text file asynchronously.
    /// One word per line is expected, whitespace is trimmed and empty lines will be ignored.
    /// The <see cref="CharTrie"/> will not be <see cref="CharTrie.Prune(bool)"/>d. Call this method to finalize the <see cref="CharTrie"/>.
    /// </summary>
    /// <param name="fi">The <see cref="FileInfo"/></param>
    /// <returns>A Task, resulting in a <see cref="CharTrie"/> if successful; else <see langword="null"/></returns>
    /// <exception cref="InvalidOperationException">The file could not be opened and read as a text file.</exception>
    public static async Task<CharTrie?> ImportAsync(FileInfo fi)
    {
        ArgumentNullException.ThrowIfNull(fi, nameof(fi));
        return await ImportAsync(fi.Open(FileMode.Open, FileAccess.Read, FileShare.Read)).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a new <see cref="CharTrie"/> and tries to import all words in the given <see cref="Stream"/> asynchronously.
    /// One word per line is expected, whitespace is trimmed and empty lines will be ignored.
    /// The <see cref="CharTrie"/> will not be <see cref="CharTrie.Prune(bool)"/>d. Call this method to finalize the <see cref="CharTrie"/>.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> containing the word list</param>
    /// <returns>A Task, resulting in a <see cref="CharTrie"/> if successful; else <see langword="null"/></returns>
    /// <exception cref="InvalidOperationException">The stream could not be opened and read as text</exception>
    public static async Task<CharTrie?> ImportAsync(Stream stream)
    {
        CharTrie? trie = null;

        var characters = new List<char>();
        var words = new List<string>();

        characters.Add('\0'); // root character
        try
        {
            using var reader = new StreamReader(stream);
            var curLine = await reader.ReadLineAsync().ConfigureAwait(false);
#if DEBUG
                var numLinesRead = 0;
                var numLinesAdded = 0;
#endif
            while (curLine != null)
            {
#if DEBUG
                    numLinesRead++;
#endif
                if (curLine.Length > 0)
                {
                    foreach (var c in curLine)
                    {
                        if (!characters.Contains(c))
                        {
                            characters.Add(c);
                            characters.Sort();
                        }
                    }
                    words.Add(curLine.Trim());
                }
                curLine = await reader.ReadLineAsync().ConfigureAwait(false);
            }
            characters.Sort();
            trie = new CharTrie([.. characters]);
            words.ForEach(w =>
            {
                trie.Add(w);
#if DEBUG
                    numLinesAdded++;
#endif
            });
#if DEBUG
                Debug.WriteLine("Lines read: {0} | Lines added: {1}.", numLinesRead, numLinesAdded);
#endif
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(string.Format(null, errImpEx, _Import, nameof(stream)), ex);
        }
        return trie;
    }

    /// <summary>
    /// Creates a <see cref="CharTrie"/> from the given json file asynchronously.
    /// </summary>
    /// <param name="fi">The <see cref="FileInfo"/></param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use</param>
    /// <returns>A Task, resulting in a <see cref="CharTrie"/> if successful; else <see langword="null"/></returns>
    /// <exception cref="InvalidOperationException">The file could not be opened and read as a text file or its contents could not be parsed into a <see cref="CharTrie"/>.</exception>
    public static async Task<CharTrie?> LoadAsync(FileInfo fi, JsonSerializerOptions? options = null)
    {
        if (fi != null && fi.Exists)
        {
            try
            {
                using var stream = fi.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
                return await JsonSerializer.DeserializeAsync<CharTrie>(stream, options ?? mSerializerOptions).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format(null, errImpEx, _Import, fi.FullName), ex);
            }
        }
        return null;
    }

    /// <summary>
    /// Creates a <see cref="CharTrie"/> from the given <see cref="Stream"/> asynchronously.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/>, containing the serialized <see cref="CharTrie"/></param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use</param>
    /// <returns>A Task, resulting in a <see cref="CharTrie"/> if successful; else <see langword="null"/></returns>
    /// <exception cref="InvalidOperationException">The file could not be opened and read as a text file or its contents could not be parsed into a <see cref="CharTrie"/>.</exception>
    public static async Task<CharTrie?> LoadAsync(Stream stream, JsonSerializerOptions? options = null)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<CharTrie>(stream, options ?? mSerializerOptions).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(string.Format(null, errImpEx, _Import, nameof(stream)), ex);
        }
    }

    #endregion
}