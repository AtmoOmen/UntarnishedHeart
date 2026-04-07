using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.ExecuteAction.Configuration;

public sealed class ExecuteActionJSONConverter : JsonConverter<ExecuteActionBase>
{
    private const string TypeIDPropertyName = "TypeId";

    public override void WriteJson(JsonWriter writer, ExecuteActionBase? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        SerializeToJObject(value, serializer).WriteTo(writer);
    }

    public override ExecuteActionBase? ReadJson
    (
        JsonReader         reader,
        Type               objectType,
        ExecuteActionBase? existingValue,
        bool               hasExistingValue,
        JsonSerializer     serializer
    )
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var token = JToken.Load(reader);
        if (token.Type != JTokenType.Object)
            return null;

        var jsonObject = ExecuteActionJSONMigrator.Instance.MigrateToLatest((JObject)token);
        return DeserializeCurrent(jsonObject);
    }

    internal static JObject SerializeToJObject(ExecuteActionBase value, JsonSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(value);

        var concreteSerializer = PolymorphicJsonSerializerFactory.CreateConcreteTypeSerializer<ExecuteActionBase>();
        var obj                = JObject.FromObject(value, concreteSerializer);
        obj["Version"]          = ExecuteActionJSONMigrator.CurrentJSONVersion;
        obj[TypeIDPropertyName] = ExecuteActionJsonTypeRegistry.Instance.GetTypeID(value);
        return obj;
    }

    internal static ExecuteActionBase DeserializeCurrent(JObject obj)
    {
        var typeID = obj[TypeIDPropertyName]?.Value<string>();
        if (string.IsNullOrWhiteSpace(typeID))
            throw new InvalidOperationException("执行动作缺少 TypeId");

        var runtimeType        = ExecuteActionJsonTypeRegistry.Instance.GetRuntimeType(typeID);
        var concreteSerializer = PolymorphicJsonSerializerFactory.CreateConcreteTypeSerializer<ExecuteActionBase>();

        return obj.ToObject(runtimeType, concreteSerializer) as ExecuteActionBase ??
               throw new InvalidOperationException($"无法反序列化执行动作 TypeId: {typeID}");
    }
}
