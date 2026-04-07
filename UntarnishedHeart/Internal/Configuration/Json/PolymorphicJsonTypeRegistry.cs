using System.Collections.Frozen;

namespace UntarnishedHeart.Internal.Configuration.Json;

internal abstract class PolymorphicJsonTypeRegistry<TBase, TKind, TAttribute>
    where TBase : class
    where TKind : struct, Enum
    where TAttribute : Attribute
{
    private readonly FrozenDictionary<string, Entry> entriesByTypeID;
    private readonly FrozenDictionary<Type, Entry>   entriesByRuntimeType;
    private readonly FrozenDictionary<TKind, Entry>  entriesByKind;

    protected PolymorphicJsonTypeRegistry()
    {
        var entries = typeof(TBase).Assembly
                                   .GetTypes()
                                   .Where(type => !type.IsAbstract && typeof(TBase).IsAssignableFrom(type) && ShouldRegisterType(type))
                                   .Select(CreateEntry)
                                   .ToArray();

        entriesByTypeID      = CreateFrozenDictionary(entries, entry => entry.TypeID,      StringComparer.Ordinal, "TypeId");
        entriesByRuntimeType = CreateFrozenDictionary(entries, entry => entry.RuntimeType, null,                   "运行时类型");
        entriesByKind        = CreateFrozenDictionary(entries, entry => entry.Kind,        null,                   "Kind");
    }

    protected abstract string DisplayName { get; }

    protected abstract TAttribute? GetMetadata(Type type);

    protected virtual bool ShouldRegisterType(Type type) => true;

    protected abstract string GetTypeID(TAttribute metadata);

    protected abstract TKind GetKind(TAttribute metadata);

    protected virtual void InitializeDefaultInstance(TBase instance)
    {
    }

    public TBase CreateDefault(TKind kind)
    {
        var instance = GetEntry(kind).CreateInstance();
        InitializeDefaultInstance(instance);
        return instance;
    }

    public string GetTypeID(TKind kind) => GetEntry(kind).TypeID;

    public string GetTypeID(TBase value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return GetEntry(value.GetType()).TypeID;
    }

    public Type GetRuntimeType(string typeID) => GetEntry(typeID).RuntimeType;

    private Entry CreateEntry(Type runtimeType)
    {
        var metadata = GetMetadata(runtimeType) ??
                       throw new InvalidOperationException($"{DisplayName} 类型 {runtimeType.FullName} 缺少 {typeof(TAttribute).Name}");

        var typeID = GetTypeID(metadata);
        if (string.IsNullOrWhiteSpace(typeID))
            throw new InvalidOperationException($"{DisplayName} 类型 {runtimeType.FullName} 的 TypeId 不能为空");

        var constructor = runtimeType.GetConstructor(Type.EmptyTypes) ??
                          throw new InvalidOperationException($"{DisplayName} 类型 {runtimeType.FullName} 缺少公有无参构造函数");

        return new Entry
        (
            typeID,
            GetKind(metadata),
            runtimeType,
            () => (TBase)constructor.Invoke(null)
        );
    }

    private Entry GetEntry(string typeID) =>
        entriesByTypeID.TryGetValue(typeID, out var entry)
            ? entry
            : throw new InvalidOperationException($"未知的 {DisplayName} TypeId: {typeID}");

    private Entry GetEntry(Type runtimeType) =>
        entriesByRuntimeType.TryGetValue(runtimeType, out var entry)
            ? entry
            : throw new InvalidOperationException($"未注册的 {DisplayName} 运行时类型: {runtimeType.FullName}");

    private Entry GetEntry(TKind kind) =>
        entriesByKind.TryGetValue(kind, out var entry)
            ? entry
            : throw new InvalidOperationException($"未注册的 {DisplayName} Kind: {kind}");

    private FrozenDictionary<TKey, Entry> CreateFrozenDictionary<TKey>
    (
        Entry[]                  entries,
        Func<Entry, TKey>        keySelector,
        IEqualityComparer<TKey>? comparer,
        string                   keyDisplayName
    )
        where TKey : notnull
    {
        var dictionary = comparer is null ? new Dictionary<TKey, Entry>() : new Dictionary<TKey, Entry>(comparer);

        foreach (var entry in entries)
        {
            var key = keySelector(entry);
            if (!dictionary.TryAdd(key, entry))
                throw new InvalidOperationException($"{DisplayName} 的 {keyDisplayName} 重复: {key}");
        }

        return comparer is null
                   ? dictionary.ToFrozenDictionary()
                   : dictionary.ToFrozenDictionary(comparer);
    }

    private sealed record Entry
    (
        string      TypeID,
        TKind       Kind,
        Type        RuntimeType,
        Func<TBase> CreateInstance
    );
}
