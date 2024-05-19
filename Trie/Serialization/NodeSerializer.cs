using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueHeron.Collections.Trie.Serialization;

/// <summary>
/// A <see cref="JsonConverter{Node}"/> that minimizes output.
/// </summary>
public class NodeSerializer : JsonConverter<Node>
{
    #region Fields

    private const string _C = "c"; // NumChildren
    private const string _R = "r"; // RemainingDepth
    private const string _T = "t"; // TypeIndex
    private const string _V = "v"; // Value
    private const string _W = "w"; // IsWord

    #endregion

    #region Private methods and functions

    /// <summary>
    /// Writes the <see cref="Node.Value"/>.
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
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Node value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        if (value.IsWord)
        {
            writer.WriteNumber(_W, 1);
        }
        if (value.Children.Count > 0)
        {
            writer.WriteNumber(_C, value.Children.Count);
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