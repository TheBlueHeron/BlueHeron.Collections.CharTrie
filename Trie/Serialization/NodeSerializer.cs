using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueHeron.Collections.Trie.Serialization;

/// <summary>
/// A <see cref="JsonConverter{Node}"/> that minimizes output.
/// The node will be serialized with an extra field, containing the number of children.
/// </summary>
internal sealed class NodeSerializer : JsonConverter<Node>
{
    #region Fields

    private const string _C = "c"; // NumChildren
    private const string _R = "r"; // RemainingDepth
    private const string _V = "v"; // Value
    private const string _W = "w"; // IsWord

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
        if (value.Value != null)
        {
            writer.WriteNumber(_V, value.Value.Value);
        }
        writer.WriteEndObject();
    }

    #endregion
}