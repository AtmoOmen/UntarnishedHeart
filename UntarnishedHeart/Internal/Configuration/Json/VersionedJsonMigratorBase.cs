using Newtonsoft.Json.Linq;

namespace UntarnishedHeart.Internal.Configuration.Json;

internal abstract class VersionedJsonMigratorBase<T>
{
    protected virtual string DisplayName => typeof(T).Name;

    protected virtual string VersionPropertyName => "Version";

    protected abstract int CurrentVersion { get; }

    protected abstract int LegacyVersion { get; }

    protected abstract IReadOnlyDictionary<int, JsonObjectMigratorBase> Migrators { get; }

    public JObject MigrateToLatest(JObject jsonObject)
    {
        ArgumentNullException.ThrowIfNull(jsonObject);

        var current = (JObject)jsonObject.DeepClone();
        var version = ReadVersion(current);
        if (version > CurrentVersion)
            throw new InvalidOperationException($"不支持的 {DisplayName} 版本: {version}");

        while (version < CurrentVersion)
        {
            if (!Migrators.TryGetValue(version, out var migrator))
                throw new InvalidOperationException($"不支持的 {DisplayName} 版本: {version}");

            current = migrator.Migrate(current);
            current[VersionPropertyName] = migrator.ToVersion;
            version                      = migrator.ToVersion;
        }

        current[VersionPropertyName] = CurrentVersion;
        return current;
    }

    private int ReadVersion(JObject jsonObject)
    {
        var token = jsonObject[VersionPropertyName];
        if (token is null)
            return ResolveMissingVersion(jsonObject);

        var version = token.Type switch
        {
            JTokenType.Integer => token.Value<int>(),
            JTokenType.String when int.TryParse(token.Value<string>(), out var value) => value,
            _ => throw new InvalidOperationException($"{DisplayName} 的版本字段无效")
        };

        if (version < LegacyVersion)
            throw new InvalidOperationException($"不支持的 {DisplayName} 版本: {version}");

        return version;
    }

    protected virtual int ResolveMissingVersion(JObject jsonObject) => LegacyVersion;
}
