using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueHeron.Collections.Trie.Serialization;

/// <summary>
/// A <see cref="JsonConverter"/> that serializes and deserializes a <see cref="Trie"/>.
/// </summary>
internal sealed class TrieConverter : JsonConverter<Trie>
{
    #region Fields

    private const string _N = "n"; // Nodes

    #endregion

    #region Overrides

    /// <inheritdoc/>
    public override Trie? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var trie = new Trie();
        List<DeserializedNode> nodes = [];
        var curIndex = 0;
        
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    if (reader.GetString() is string key)
                    {
                        if (key == _N)
                        {
                            while (reader.Read())
                            {
                                switch (reader.TokenType)
                                {
                                    case JsonTokenType.StartObject:
                                        using (var doc = JsonDocument.ParseValue(ref reader))
                                        {
                                            var node = JsonSerializer.Deserialize<DeserializedNode>(doc.RootElement, options);
                                            if (node != null)
                                            {
                                                nodes.Add(node);
                                            }
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        if (nodes.Count > 0)
        {
            var rootWrapper = nodes[0];
            var root = rootWrapper.Node;
            if (rootWrapper.NumChildren > 0) // reconstitute the hierarchy
            {
                AddChildren(ref root, ref nodes, ref curIndex, nodes[0].NumChildren);
            }
            trie.RootNode = root;
        }
        return trie;
    }

    /// <summary>
    /// Adds the appropriate (<paramref name="numChildren"/>) number of children from the given <paramref name="nodes"/> to <paramref name="parent"/>'s children array.
    /// </summary>
    /// <param name="parent">The current <see cref="TrieNode"/></param>
    /// <param name="nodes">The deserialized <see cref="DeserializedNode"/>s</param>
    /// <param name="curIndex">The current index in the <paramref name="nodes"/> list</param>
    /// <param name="numChildren">The number of nodes to add to <paramref name="parent"/></param>
    private static void AddChildren(ref TrieNode parent, ref List<DeserializedNode> nodes, ref int curIndex, int numChildren)
    {
        parent.Children = new TrieNode[numChildren];
        for (var i = 0; i < numChildren; i++)
        {
            curIndex++;
            var nodeWrapper = nodes[curIndex];
            parent.Children[i] = nodeWrapper.Node;
            if (nodeWrapper.NumChildren > 0)
            {
                AddChildren(ref parent.Children[i], ref nodes, ref curIndex, nodeWrapper.NumChildren);
            }
        }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Trie value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(_N);
        writer.WriteStartArray();
        foreach (var node in value.AsEnumerable()) // serialize Trie nodes as a flat array of nodes (large hierarchies cause memory problems in the JsonSerializer when deserializing)
        {
            writer.WriteRawValue(JsonSerializer.Serialize(node));
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    #endregion
}