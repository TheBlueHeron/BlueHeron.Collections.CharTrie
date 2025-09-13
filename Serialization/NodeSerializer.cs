using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueHeron.Collections.Trie.Serialization;

/// <summary>
/// A <see cref="JsonConverter{TrieNode}"/> that minimizes output.
/// The node will be serialized with an extra field, containing the number of children.
/// </summary>
internal sealed class NodeSerializer : JsonConverter<TrieNode>
{
    #region Fields

    private const string _C = "c"; // NumChildren
    private const string _K = "k"; // Character
    private const string _R = "r"; // RemainingDepth
    private const string _W = "w"; // IsWord

    #endregion

    #region Overrides

    /// <inheritdoc/>
    public override TrieNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, TrieNode value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(_K, value.Character.ToString());
        if (value.IsWord)
        {
            writer.WriteNumber(_W, 1);
        }
        if (value.Children.Length > 0)
        {
            writer.WriteNumber(_C, value.Children.Length);
        }
        if (value.RemainingDepth > 0)
        {
            writer.WriteNumber(_R, value.RemainingDepth);
        }
        writer.WriteEndObject();
    }

    #endregion
}