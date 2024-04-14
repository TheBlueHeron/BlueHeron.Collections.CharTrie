using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueHeron.Collections.Trie.Serialization;

/// <summary>
/// A <see cref="JsonConverter{Node}"/> that minimizes output.
/// </summary>
/// <typeparam name="TNode">The type of the <see cref="Node"/></typeparam>
public class NodeConverter : JsonConverter<Node>
{
    #region Fields

    private const string _C = "c"; // Children
    private const string _N = "n"; // NumWords
    private const string _V = "v"; // Value
    private const string _W = "w"; // IsWord

    #endregion

    #region Public methods and functions

    /// <summary>
    /// Reads the value of the node.
    /// </summary>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> containing the data</param>
    /// <param name="propertyName">The name of the current property</param>
    /// <param name="value">The <see cref="Node"/> to deserialize</param>
    private static void ReadValue(ref Utf8JsonReader reader, string? propertyName, Node value)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                value.Value = reader.GetInt32();
                break;
        }
    }

    /// <summary>
    /// Writes the value of the node.
    /// </summary>
    /// <param name="writer">The <see cref="Utf8JsonWriter"/> to write to</param>
    /// <param name="value">The <see cref="Node.Value"/> to serialize</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use</param>
    private static void WriteValue(Utf8JsonWriter writer, object value)
    {
        if (value is int v)
        {
            writer.WriteNumber(_V, v);
        }
    }

    #endregion

    #region Overrides

    /// <inheritdoc/>
    public override Node? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var node = new Node();

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    var propertyName = reader.GetString();
                    switch (propertyName)
                    {
                        case _N:
                            reader.Read();
                            node.NumWords = reader.GetInt32();
                            break;
                        case _W:
                            reader.Read();
                            node.IsWord = true;  // no need to read value, because the value is always 1, meaning 'true'. When node.IsWord = false, it is not written during serialization.
                            break;
                        case _V:
                            reader.Read();
                            ReadValue(ref reader, propertyName, node);
                            break;
                        case _C: // must come last!
                            reader.Read(); // StartObject of dictionary
                            reader.Read(); // PropertyName -> is key of node in dictionary
                            while (reader.TokenType == JsonTokenType.PropertyName)
                            {
                                var p = reader.GetString();
                                reader.Read(); // StartObject of node
                                var child = Read(ref reader, typeToConvert, options);
                                if (!string.IsNullOrEmpty(p))
                                {
                                    if (child != null)
                                    {
                                        node.Children.Add(p[0], child);
                                    }
                                }
                                reader.Read(); // move to end or next node
                            }
                            break;
                    }
                    if (reader.TokenType == JsonTokenType.EndObject) // EndObject of dictionary
                    {
                        reader.Read(); // move to end or next node
                        return node;
                    }
                    break;
                case JsonTokenType.EndObject: // EndObject of node
                    return node;
            }
        }
        throw new InvalidOperationException(); // this should not happen
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Node value, JsonSerializerOptions options)
    {
        writer.WriteStartObject(); // node
        if (value.IsWord)
        {
            writer.WriteNumber(_W, 1);
        }
        writer.WriteNumber(_N,value.NumWords);
        if (value.Value != null)
        {
            WriteValue(writer, value.Value);
        }
        if (value.Children.Count > 0) // must come last!
        {
            writer.WriteStartObject(_C);
            foreach (var kv in value.Children)
            {
                writer.WritePropertyName(kv.Key.ToString());
                Write(writer, kv.Value, options);
            }
            writer.WriteEndObject(); // cc
        }
        writer.WriteEndObject(); // node
    }

    #endregion
}