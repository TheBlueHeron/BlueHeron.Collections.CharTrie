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

    private const string _C = "c"; // NumChildren
    private const string _N = "n"; // NumWords
    private const string _R = "r"; // RemainingDepth
    private const string _T = "t"; // TypeIndex
    private const string _V = "v"; // Value
    private const string _W = "w"; // IsWord

    #endregion

    #region Private methods and functions

    /// <summary>
    /// Reads the value of and sets it on the node.
    /// </summary>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> containing the data</param>
    /// <param name="node">The <see cref="Node"/>, whose value to deserialize</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use</param>
    protected static void ReadValue(ref Utf8JsonReader reader, Node node, JsonSerializerOptions options)
    {
        if (node.TypeIndex >= 0)
        {
            var type = Type.GetType(Trie.Types[node.TypeIndex]);

            if (type != null)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Number:
                        switch (type)
                        {
                            case Type _ when type == typeof(short):
                                node.Value = reader.GetInt16();
                                break;
                            case Type _ when type == typeof(int):
                                node.Value = reader.GetInt32();
                                break;
                            case Type _ when type == typeof(float):
                                node.Value = reader.GetSingle();
                                break;
                            case Type _ when type == typeof(double):
                                node.Value = reader.GetDouble();
                                break;
                            case Type _ when type == typeof(long):
                                node.Value = reader.GetInt64();
                                break;
                            case Type _ when type == typeof(decimal):
                                node.Value = reader.GetDecimal();
                                break;
                        }
                        break;
                    case JsonTokenType.String:
                        switch (type)
                        {
                            case Type _ when type == typeof(DateOnly):
                                if (DateOnly.TryParse(reader.GetString(), out var d))
                                {
                                    node.Value = d;
                                }
                                break;
                            case Type _ when type == typeof(DateTime):
                                if (DateTime.TryParse(reader.GetString(), out var dt))
                                {
                                    node.Value = dt;
                                }
                                break;
                            case Type _ when type == typeof(DateTimeOffset):
                                if (DateTimeOffset.TryParse(reader.GetString(), out var o))
                                {
                                    node.Value = o;
                                }
                                break;
                            default:
                                node.Value = reader.GetString();
                                break;
                        }
                        break;
                    case JsonTokenType.True:
                    case JsonTokenType.False:
                        node.Value = reader.GetBoolean();
                        break;
                    case JsonTokenType.StartObject: // try to deserialize object to its registered type
                        {
                            try
                            {
                                using var doc = JsonDocument.ParseValue(ref reader);

                                node.Value = JsonSerializer.Deserialize(doc.RootElement, type, options);
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidOperationException($"Unable to parse object of type '{Trie.Types[node.TypeIndex]}'.", ex);
                            }
                        }
                        break;
                    default:
                        node.Value = null;
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Writes the value of the node.
    /// </summary>
    /// <param name="writer">The <see cref="Utf8JsonWriter"/> to write to</param>
    /// <param name="node">The <see cref="Node"/> whose value to serialize</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use</param>
    protected static void WriteValue(Utf8JsonWriter writer, Node node, JsonSerializerOptions options)
    {
        if (node.Value != null)
        {
            writer.WriteNumber(_T, node.TypeIndex);
            writer.WritePropertyName(_V);
            writer.WriteRawValue(JsonSerializer.Serialize(node.Value, options));
        }
    }

    #endregion

    #region Overrides

    /// <inheritdoc/>
    public override Node? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var node = new Node(true);

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    var propertyName = reader.GetString();
                    switch (propertyName)
                    {
                        case _C:
                            reader.Read();
                            node.NumChildren = reader.GetInt32();
                            break;
                        case _N:
                            reader.Read();
                            node.NumWords = reader.GetInt32();
                            break;
                        case _R:
                            reader.Read();
                            node.RemainingDepth = reader.GetInt32();
                            break;
                        case _W:
                            reader.Read();
                            node.IsWord = true;  // no need to read value, because the value is always 1, meaning 'true'. When node.IsWord = false, it is not written during serialization.
                            break;
                        case _T:
                            reader.Read();
                            node.TypeIndex = reader.GetInt32();
                            break;
                        case _V:
                            reader.Read();
                            ReadValue(ref reader, node, options);
                            break;
                    }
                    break;
                case JsonTokenType.EndObject: // EndObject of node
                    return node;
            }
        }
        throw new InvalidCastException(); // this should not happen
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Node value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        if (value.IsWord)
        {
            writer.WriteNumber(_W, 1);
        }
        if (value.NumChildren > 0)
        {
            writer.WriteNumber(_C, value.NumChildren);
        }
        if (value.NumWords > 0)
        {
            writer.WriteNumber(_N, value.NumWords);
        }
        if (value.RemainingDepth > 0)
        {
            writer.WriteNumber(_R, value.RemainingDepth);
        }
        WriteValue(writer, value, options);
        writer.WriteEndObject();
    }

    #endregion
}