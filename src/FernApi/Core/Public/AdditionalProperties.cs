using FernApi.Core;
using global::System.Collections;
using global::System.Collections.ObjectModel;
using global::System.Text.Json;
using global::System.Text.Json.Nodes;

namespace FernApi;

public record ReadOnlyAdditionalProperties : ReadOnlyAdditionalProperties<JsonElement>
{
    internal ReadOnlyAdditionalProperties() { }

    internal ReadOnlyAdditionalProperties(IDictionary<string, JsonElement> properties)
        : base(properties) { }
}

public record ReadOnlyAdditionalProperties<T> : IReadOnlyDictionary<string, T>
{
    private readonly Dictionary<string, JsonElement> _extensionData = new();
    private readonly Dictionary<string, T> _convertedCache = new();

    /// <inheritdoc />
    /// <remarks>
    /// Written out rather than synthesized. A record's generated Equals compares every instance
    /// field, and both backing dictionaries are fresh instances per object compared by REFERENCE —
    /// so two of these were never equal, not even when both were empty. Because every generated
    /// model holds one, that silently defeated value equality for every model in the SDK.
    /// Compared on the raw JSON so equality does not depend on which keys happen to be converted.
    /// </remarks>
    public virtual bool Equals(ReadOnlyAdditionalProperties<T>? other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        // Restated because hand-writing Equals drops the synthesized EqualityContract check, and
        // ReadOnlyAdditionalProperties : ReadOnlyAdditionalProperties<JsonElement> really does derive.
        if (EqualityContract != other.EqualityContract)
        {
            return false;
        }
        if (_extensionData.Count != other._extensionData.Count)
        {
            return false;
        }
        foreach (var pair in _extensionData)
        {
            if (!other._extensionData.TryGetValue(pair.Key, out var candidate))
            {
                return false;
            }
            if (pair.Value.GetRawText() != candidate.GetRawText())
            {
                return false;
            }
        }
        return true;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // Key-set only: JsonElement has no value-based hash, and a hash that ignores the values is
        // still correct — equal objects hash equally, which is all GetHashCode has to guarantee.
        // Summed rather than folded so the result does not depend on dictionary iteration order,
        // and without LINQ so no using has to be added to every generated SDK.
        unchecked
        {
            var hash = 17 + _extensionData.Count;
            foreach (var key in _extensionData.Keys)
            {
                hash += key.GetHashCode();
            }
            return hash;
        }
    }

    internal ReadOnlyAdditionalProperties()
    {
        _extensionData = new Dictionary<string, JsonElement>();
        _convertedCache = new Dictionary<string, T>();
    }

    internal ReadOnlyAdditionalProperties(IDictionary<string, T> properties)
    {
        _extensionData = new Dictionary<string, JsonElement>(properties.Count);
        _convertedCache = new Dictionary<string, T>(properties.Count);
        foreach (var kvp in properties)
        {
            if (kvp.Value is JsonElement element)
            {
                _extensionData.Add(kvp.Key, element);
            }
            else
            {
                _extensionData[kvp.Key] = JsonUtils.SerializeToElement(kvp.Value);
            }

            _convertedCache[kvp.Key] = kvp.Value;
        }
    }

    private static T ConvertToT(JsonElement value)
    {
        if (typeof(T) == typeof(JsonElement))
        {
            return (T)(object)value;
        }

        return value.Deserialize<T>(JsonOptions.JsonSerializerOptions)!;
    }

    internal void CopyFromExtensionData(IDictionary<string, JsonElement> extensionData)
    {
        _extensionData.Clear();
        _convertedCache.Clear();
        foreach (var kvp in extensionData)
        {
            _extensionData[kvp.Key] = kvp.Value;
            if (kvp.Value is T value)
            {
                _convertedCache[kvp.Key] = value;
            }
        }
    }

    private T GetCached(string key)
    {
        if (_convertedCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var value = ConvertToT(_extensionData[key]);
        _convertedCache[key] = value;
        return value;
    }

    public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
    {
        return _extensionData
            .Select(kvp => new KeyValuePair<string, T>(kvp.Key, GetCached(kvp.Key)))
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _extensionData.Count;

    public bool ContainsKey(string key) => _extensionData.ContainsKey(key);

    public bool TryGetValue(string key, out T value)
    {
        if (_convertedCache.TryGetValue(key, out value!))
        {
            return true;
        }

        if (_extensionData.TryGetValue(key, out var element))
        {
            value = ConvertToT(element);
            _convertedCache[key] = value;
            return true;
        }

        return false;
    }

    public T this[string key] => GetCached(key);

    public IEnumerable<string> Keys => _extensionData.Keys;

    public IEnumerable<T> Values => Keys.Select(GetCached);
}

public record AdditionalProperties : AdditionalProperties<object?>
{
    public AdditionalProperties() { }

    public AdditionalProperties(IDictionary<string, object?> properties)
        : base(properties) { }
}

public record AdditionalProperties<T> : IDictionary<string, T>
{
    private readonly Dictionary<string, object?> _extensionData;
    private readonly Dictionary<string, T> _convertedCache;

    /// <inheritdoc />
    /// <remarks>
    /// Written out for the same reason as on <see cref="ReadOnlyAdditionalProperties{T}"/>: the
    /// synthesized record Equals compares both backing dictionaries by REFERENCE, so two of these
    /// were never equal and every model holding one lost value equality. Compared on the JSON form
    /// so a bag filled by deserialization (values are JsonElement) still equals an identical one
    /// filled by hand (values are CLR objects).
    /// </remarks>
    public virtual bool Equals(AdditionalProperties<T>? other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        // Restated because hand-writing Equals drops the synthesized EqualityContract check, and
        // AdditionalProperties : AdditionalProperties<object?> really does derive.
        if (EqualityContract != other.EqualityContract)
        {
            return false;
        }
        if (_extensionData.Count != other._extensionData.Count)
        {
            return false;
        }
        foreach (var pair in _extensionData)
        {
            if (!other._extensionData.TryGetValue(pair.Key, out var candidate))
            {
                return false;
            }
            if (!ValuesEqual(pair.Value, candidate))
            {
                return false;
            }
        }
        return true;
    }

    /// <remarks>
    /// This bag takes any <c>object?</c>, so unlike the read-only one it can be handed a value
    /// System.Text.Json cannot write — a cyclic graph, or a type whose converter throws. Letting
    /// that escape would make Equals itself throw, and an Equals that throws propagates into
    /// <c>==</c>, <c>Distinct()</c>, <c>HashSet</c> membership and every Dictionary lookup. Fall
    /// back to the structural comparison rather than poison all of those.
    /// </remarks>
    private static bool ValuesEqual(object? left, object? right)
    {
        try
        {
            return RawTextOf(left) == RawTextOf(right);
        }
        catch (JsonException)
        {
            return ValueEquality.AreEqual(left, right);
        }
        catch (global::System.NotSupportedException)
        {
            return ValueEquality.AreEqual(left, right);
        }
    }

    private static string RawTextOf(object? value) =>
        value is JsonElement element
            ? element.GetRawText()
            : JsonUtils.SerializeToElement(value).GetRawText();

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // Key-set only, as on ReadOnlyAdditionalProperties: a hash that ignores the values is still
        // correct (equal objects hash equally) and costs no serialization. Summed so the result does
        // not depend on dictionary iteration order.
        unchecked
        {
            var hash = 17 + _extensionData.Count;
            foreach (var key in _extensionData.Keys)
            {
                hash += key.GetHashCode();
            }
            return hash;
        }
    }

    public AdditionalProperties()
    {
        _extensionData = new Dictionary<string, object?>();
        _convertedCache = new Dictionary<string, T>();
    }

    public AdditionalProperties(IDictionary<string, T> properties)
    {
        _extensionData = new Dictionary<string, object?>(properties.Count);
        _convertedCache = new Dictionary<string, T>(properties.Count);
        foreach (var kvp in properties)
        {
            _extensionData[kvp.Key] = kvp.Value;
            _convertedCache[kvp.Key] = kvp.Value;
        }
    }

    private static T ConvertToT(object? extensionDataValue)
    {
        return extensionDataValue switch
        {
            T value => value,
            JsonElement jsonElement => jsonElement.Deserialize<T>(
                JsonOptions.JsonSerializerOptions
            )!,
            JsonNode jsonNode => jsonNode.Deserialize<T>(JsonOptions.JsonSerializerOptions)!,
            _ => JsonUtils
                .SerializeToElement(extensionDataValue)
                .Deserialize<T>(JsonOptions.JsonSerializerOptions)!,
        };
    }

    internal void CopyFromExtensionData(IDictionary<string, object?> extensionData)
    {
        _extensionData.Clear();
        _convertedCache.Clear();
        foreach (var kvp in extensionData)
        {
            _extensionData[kvp.Key] = kvp.Value;
            if (kvp.Value is T value)
            {
                _convertedCache[kvp.Key] = value;
            }
        }
    }

    internal void CopyToExtensionData(IDictionary<string, object?> extensionData)
    {
        extensionData.Clear();
        foreach (var kvp in _extensionData)
        {
            extensionData[kvp.Key] = kvp.Value;
        }
    }

    public JsonObject ToJsonObject() =>
        (
            JsonUtils.SerializeToNode(_extensionData)
            ?? throw new InvalidOperationException(
                "Failed to serialize AdditionalProperties to JSON Node."
            )
        ).AsObject();

    public JsonNode ToJsonNode() =>
        JsonUtils.SerializeToNode(_extensionData)
        ?? throw new InvalidOperationException(
            "Failed to serialize AdditionalProperties to JSON Node."
        );

    public JsonElement ToJsonElement() => JsonUtils.SerializeToElement(_extensionData);

    public JsonDocument ToJsonDocument() => JsonUtils.SerializeToDocument(_extensionData);

    public IReadOnlyDictionary<string, JsonElement> ToJsonElementDictionary()
    {
        return new ReadOnlyDictionary<string, JsonElement>(
            _extensionData.ToDictionary(
                kvp => kvp.Key,
                kvp =>
                {
                    if (kvp.Value is JsonElement jsonElement)
                    {
                        return jsonElement;
                    }

                    return JsonUtils.SerializeToElement(kvp.Value);
                }
            )
        );
    }

    public ICollection<string> Keys => _extensionData.Keys;

    public ICollection<T> Values
    {
        get
        {
            var values = new T[_extensionData.Count];
            var i = 0;
            foreach (var key in Keys)
            {
                values[i++] = GetCached(key);
            }

            return values;
        }
    }

    private T GetCached(string key)
    {
        if (_convertedCache.TryGetValue(key, out var value))
        {
            return value;
        }

        value = ConvertToT(_extensionData[key]);
        _convertedCache.Add(key, value);
        return value;
    }

    private void SetCached(string key, T value)
    {
        _extensionData[key] = value;
        _convertedCache[key] = value;
    }

    private void AddCached(string key, T value)
    {
        _extensionData.Add(key, value);
        _convertedCache.Add(key, value);
    }

    private bool RemoveCached(string key)
    {
        var isRemoved = _extensionData.Remove(key);
        _convertedCache.Remove(key);
        return isRemoved;
    }

    public int Count => _extensionData.Count;
    public bool IsReadOnly => false;

    public T this[string key]
    {
        get => GetCached(key);
        set => SetCached(key, value);
    }

    public void Add(string key, T value) => AddCached(key, value);

    public void Add(KeyValuePair<string, T> item) => AddCached(item.Key, item.Value);

    public bool Remove(string key) => RemoveCached(key);

    public bool Remove(KeyValuePair<string, T> item) => RemoveCached(item.Key);

    public bool ContainsKey(string key) => _extensionData.ContainsKey(key);

    public bool Contains(KeyValuePair<string, T> item)
    {
        return _extensionData.ContainsKey(item.Key)
            && EqualityComparer<T>.Default.Equals(GetCached(item.Key), item.Value);
    }

    public bool TryGetValue(string key, out T value)
    {
        if (_convertedCache.TryGetValue(key, out value!))
        {
            return true;
        }

        if (_extensionData.TryGetValue(key, out var extensionDataValue))
        {
            value = ConvertToT(extensionDataValue);
            _convertedCache[key] = value;
            return true;
        }

        return false;
    }

    public void Clear()
    {
        _extensionData.Clear();
        _convertedCache.Clear();
    }

    public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
    {
        if (array is null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (arrayIndex < 0 || arrayIndex > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        if (array.Length - arrayIndex < _extensionData.Count)
        {
            throw new ArgumentException(
                "The array does not have enough space to copy the elements."
            );
        }

        foreach (var kvp in _extensionData)
        {
            array[arrayIndex++] = new KeyValuePair<string, T>(kvp.Key, GetCached(kvp.Key));
        }
    }

    public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
    {
        return _extensionData
            .Select(kvp => new KeyValuePair<string, T>(kvp.Key, GetCached(kvp.Key)))
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
