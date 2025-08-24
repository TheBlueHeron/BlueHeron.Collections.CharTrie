using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueHeron.Collections.Trie.Serialization;

/// <summary>
/// A <see cref="JsonConverter{DeserializedNode}"/>.
/// </summary>
internal sealed class NodeDeserializer : JsonConverter<DeserializedNode>
{
    #region Fields

    private const string _C = "c"; // NumChildren
    private const string _K = "k"; // Character
    private const string _R = "r"; // RemainingDepth
    private const string _V = "v"; // Value
    private const string _W = "w"; // IsWord

    #endregion

    #region Overrides

    /// <inheritdoc/>
    public override DeserializedNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var numChildren = 0;
        var node  = new TrieNode();

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    var propertyName = reader.GetString();
                    switch (propertyName)
                    {

                        case _K:
                            reader.Read();
                            var sc = reader.GetString();
                            if (!string.IsNullOrEmpty(sc))
                            {
                                node.Character = sc[0];
                            }
                            break;
                        case _C:
                            reader.Read();
                            numChildren = reader.GetInt32();
                            break;
                        case _R:
                            reader.Read();
                            node.RemainingDepth = reader.GetInt32();
                            break;
                        case _W:
                            reader.Read();
                            node.IsWord = true;  // no need to read value, because the value is always 1, meaning 'true'. When node.IsWord = false, it is not written during serialization.
                            break;
                        case _V:
                            reader.Read();
                            node.Value = reader.GetString();
                            break;
                    }
                    break;
                case JsonTokenType.EndObject: // EndObject of node
                    return new DeserializedNode(node) { NumChildren = numChildren };
            }
        }
        throw new InvalidCastException(); // this should not happen
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DeserializedNode value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    #endregion
}