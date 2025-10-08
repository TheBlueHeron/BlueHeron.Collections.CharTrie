using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueHeron.Collections.Trie.Serialization;

/// <summary>
/// A <see cref="JsonConverter{CharNode}"/> that minimizes output.
/// </summary>
internal sealed class CharNodeConverter : JsonConverter<CharNode>
{
    #region Fields

    private const string _F = "f"; // FirstChildIndex
    private const string _I = "i"; // CharIndex
    private const string _C = "c"; // ChildCount
    private const string _W = "w"; // IsWordEnd
    private const string _R = "r"; // RemainingDepth

    #endregion

    #region Overrides

    /// <inheritdoc/>
    public override CharNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var node = new CharNode();

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    var propertyName = reader.GetString();
                    switch (propertyName)
                    {
                        case _F:
                            reader.Read();
                            node.FirstChildIndex = reader.GetInt32();
                            break;
                        case _I:
                            reader.Read();
                            node.CharIndex = reader.GetByte();
                            break;
                        case _C:
                            reader.Read();
                            node.ChildCount = reader.GetByte();
                            break;
                        case _W:
                            reader.Read();
                            node.IsWordEnd = true;  // no need to read value, because the value is always 1, meaning 'true'. When node.IsWord = false, it is not written during serialization.
                            break;
                        case _R:
                            reader.Read();
                            node.RemainingDepth = (ushort)reader.GetInt16();
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
    public override void Write(Utf8JsonWriter writer, CharNode value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber(_F, value.FirstChildIndex);
        writer.WriteNumber(_I, value.CharIndex);
        writer.WriteNumber(_C, value.ChildCount);
        if (value.IsWordEnd)
        {
            writer.WriteNumber(_W, 1);
        }
        writer.WriteNumber(_R, value.RemainingDepth);
        writer.WriteEndObject();
    }

    #endregion
}