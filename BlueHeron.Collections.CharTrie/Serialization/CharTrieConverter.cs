using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueHeron.Collections.Trie.Serialization;

/// <summary>
/// A <see cref="JsonConverter"/> that serializes and deserializes a <see cref="CharTrie"/>.
/// </summary>
internal sealed class CharTrieConverter : JsonConverter<CharTrie>
{
    #region Fields

    private const string _C = "c"; // Characters
    private const string _I = "i"; // Child indices
    private const string _N = "n"; // CharNodes
    private const string _W = "w"; // NumWords

    #endregion

    #region Overrides

    /// <inheritdoc/>
    public override CharTrie Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var count = 0;
        List<char> chars = [];
        List<int> indices = [];
        List<CharNode> nodes = [];

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    if (reader.GetString() is string key)
                    {
                        if (key == _C)
                        {
                            while (reader.Read())
                            {
                                switch (reader.TokenType)
                                {
                                    case JsonTokenType.Number:
                                        if (reader.TryGetInt32(out var c))
                                        {
                                            chars.Add((char)c);
                                        }
                                        break;
                                    default:
                                        break;
                                }
                                if (reader.TokenType == JsonTokenType.EndArray)
                                {
                                    break;
                                }
                            }
                        }
                        else if (key == _I)
                        {
                            while (reader.Read())
                            {
                                switch (reader.TokenType)
                                {
                                    case JsonTokenType.Number:
                                        if (reader.TryGetInt32(out var idx))
                                        {
                                            indices.Add(idx);
                                        }
                                        break;
                                    default:
                                        break;
                                }
                                if (reader.TokenType == JsonTokenType.EndArray)
                                {
                                    break;
                                }
                            }
                        }
                        else if (key == _N)
                        {
                            while (reader.Read())
                            {
                                switch (reader.TokenType)
                                {
                                    case JsonTokenType.StartObject:
                                        using (var doc = JsonDocument.ParseValue(ref reader))
                                        {
                                            var node = JsonSerializer.Deserialize<CharNode>(doc.RootElement, options);
                                            nodes.Add(node);
                                        }
                                        break;
                                    default:
                                        break;
                                }
                                if (reader.TokenType == JsonTokenType.EndArray)
                                {
                                    break;
                                }
                            }
                        }
                        else if (key == _W)
                        {
                            if (reader.Read() && reader.TokenType == JsonTokenType.Number)
                            {
                                if (reader.TryGetInt32(out var cnt))
                                {
                                    count = cnt;
                                }
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        return new CharTrie([.. chars], nodes, indices, count);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, CharTrie value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(_C);
        writer.WriteStartArray();
        foreach (var c in value.mCharacters)
        {
            writer.WriteNumberValue(c);
        }
        writer.WriteEndArray();
        writer.WritePropertyName(_I);
        writer.WriteStartArray();
        foreach (var idx in value.mChildIndices)
        {
            writer.WriteNumberValue(idx);
        }
        writer.WriteEndArray();
        writer.WritePropertyName(_N);
        writer.WriteStartArray();
        foreach (var node in value.mNodes)
        {
            writer.WriteRawValue(JsonSerializer.Serialize(node));
        }
        writer.WriteEndArray();
        writer.WriteNumber(_W, value.Count);
        writer.WriteEndObject();
    }

    #endregion
}