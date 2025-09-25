using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueHeron.Collections.Trie.Serialization;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes
/// <summary>
/// A <see cref="JsonConverter{CharTrie}"/> that handles <see cref="CharTrie"/> (de-)serialization.
/// </summary>
internal sealed class CharTrieConverter : JsonConverter<CharTrie>
{
    #region Fields

    private const string CHARACTER = "c";
    private const string CHILDREN = "c";
    private const string NODES = "n";
    private const string NUMNODES = "nn";
    private const string REMAININGDEPTH = "d";
    private const string WORDENDS = "w";

    #endregion

    #region Overrides

    /// <inheritdoc/>
    public override CharTrie? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var trie = new CharTrie();

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    if (reader.GetString() is string key)
                    {
                        if (key == NUMNODES)
                        {
                            reader.Read();
                            trie.mNodeCount = reader.GetInt32();
                            trie.mNodes = new CharNode[trie.mNodeCount];
                        }
                        else if (key == NODES)
                        {

                        }
                    }
                    break;
                default:
                    break;
            }
        }
        return trie;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, CharTrie value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber(NUMNODES, value.mNodeCount);
        writer.WritePropertyName(NODES);
        writer.WriteStartArray();
        foreach (var node in value.mNodes)
        {
            writer.WriteStartObject();
            writer.WriteString(CHARACTER, node.Character.ToString());
            if (node.RemainingDepth > 0)
            {
                writer.WriteNumber(REMAININGDEPTH, node.RemainingDepth);
            }
            writer.WriteEndObject();
        };
        writer.WriteEndArray();
        writer.WritePropertyName(CHILDREN);
        writer.WriteRawValue(JsonSerializer.Serialize(value.mChildren));
        writer.WritePropertyName(WORDENDS);
        writer.WriteStartArray();
        foreach (var isEnd in value.mIsWordEnd)
        {
            writer.WriteNumberValue(isEnd ? 1 : 0);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    #endregion
}
#pragma warning restore CA1812