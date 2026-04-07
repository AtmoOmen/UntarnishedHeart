using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace UntarnishedHeart.Internal.Configuration.Json;

internal static class PolymorphicJsonSerializerFactory
{
    public static JsonSerializer CreateConcreteTypeSerializer<TBase>()
        where TBase : class =>
        SerializerCache<TBase>.Instance.Value!;

    private static class SerializerCache<TBase>
        where TBase : class
    {
        public static ThreadLocal<JsonSerializer> Instance { get; } = new
        (
            () => JsonSerializer.CreateDefault
            (
                new JsonSerializerSettings
                {
                    ContractResolver = new SuppressPolymorphicJsonConverterContractResolver(typeof(TBase))
                }
            )
        );
    }

    private sealed class SuppressPolymorphicJsonConverterContractResolver
    (
        Type suppressedBaseType
    ) : DefaultContractResolver
    {
        protected override JsonConverter? ResolveContractConverter(Type objectType) =>
            suppressedBaseType.IsAssignableFrom(objectType)
                ? null
                : base.ResolveContractConverter(objectType);
    }
}
