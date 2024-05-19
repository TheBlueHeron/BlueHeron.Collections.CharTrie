using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueHeron.Collections.Trie.Serialization;

/// <summary>
/// A <see cref="JsonConverter"/> that serializes and deserializes a <see cref="Trie"/>.
/// </summary>
public sealed class TrieConverter : JsonConverter<Trie>
{
    #region Fields

    private const string _N = "n"; // Nodes

    #endregion

    #region Private methods and functions

    /// <summary>
    /// Reconstitutes the <see cref="Trie"/> recursively while reading successive nodes from the serialized array.
    /// </summary>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> that contains the data</param>
    /// <param name="parentNode">The current parent <see cref="Node"/>. If null: root node is being read</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use</param>
    /// <returns>A <see cref="Node"/></returns>
    /// <exception cref="InvalidCastException">Unexpected content in json</exception>
    private static Node? ParseNode(ref Utf8JsonReader reader, Node? parentNode, JsonSerializerOptions options)
    {
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    if (reader.GetString() is string key)
                    {
                        var c = key[0];
                        DeserializedNode? node;

                        reader.Read(); // -> StartObject ( = node)
                        using var doc = JsonDocument.ParseValue(ref reader);
                        node = JsonSerializer.Deserialize<DeserializedNode>(doc.RootElement, options);
                        if (node != null)
                        {
                            if (parentNode == null) // current node is root node
                            {
                                while (node.NumChildren > node.Children.Count)  // next node is child of current node
                                {
                                    ParseNode(ref reader, node, options);
                                }
                                var root = new Node() { IsWord = node.IsWord, RemainingDepth = node.RemainingDepth, Value = node.Value };  // loose NumChildren field

                                foreach (var item in node.Children)
                                {
                                    root.Children.Add(item);
                                }

                                return root; // end point of recursion
                            }
                            else // current node is child of parent node
                            {
                                var child = new Node { IsWord = node.IsWord, RemainingDepth = node.RemainingDepth, Value = node.Value };  // loose NumChildren field

                                parentNode.Children.Add((c, child));
                                while (node.NumChildren > node.Children.Count) // next node is child of current node
                                {
                                    ParseNode(ref reader, child, options);
                                    node.NumChildren--;
                                }
                                return null; // back up the tree
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        throw new InvalidCastException(); // should not happen
    }

    #endregion

    #region Overrides

    /// <inheritdoc/>
    public override Trie? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var trie = new Trie();

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
                                    case JsonTokenType.StartArray:
                                        var root = ParseNode(ref reader, null, options); // deserialize node array into a node hierarchy starting with a root node

                                        if (root != null)
                                        {
                                            trie.RootNode = root;
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
        return trie;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Trie value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(_N);
        writer.WriteStartArray();
        foreach (var item in value.AsEnumerable()) // serialize Trie nodes as an array of nodes
        {
            writer.WriteStartObject();
            writer.WritePropertyName($"{item.Item1}");
            writer.WriteRawValue(JsonSerializer.Serialize(item.Item2));
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    #endregion
}