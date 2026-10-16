using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FernApi.Http.Extensions;

/// <summary>
/// Extension methods for MultipartFormDataContent to serialize objects into multipart/form-data format.
/// Handles primitives, file parameters (FileParameter), file arrays, and nested objects.
/// </summary>
public static class MultipartFormDataContentExtensions
{
    /// <summary>
    /// Recursively serializes an object into multipart/form-data format.
    /// Maps object properties to form fields, handling files (FileParameter), file arrays,
    /// nested objects, and primitives. Respects JSON property name attributes.
    /// </summary>
    /// <param name="formData">The multipart form data content to populate.</param>
    /// <param name="content">The object to serialize.</param>
    /// <param name="options">JSON serializer options for property name mapping.</param>
    /// <returns>The populated multipart form data content for method chaining.</returns>
    public static MultipartFormDataContent AddObject(
        this MultipartFormDataContent formData,
        object content,
        JsonSerializerOptions? options
    )
    {
        foreach (var property in content.GetType().GetProperties())
        {
            // Skip the [JsonExtensionData] overflow bag: it holds unknown JSON
            // properties and has no meaning as multipart form fields. Without this
            // the dictionary itself would be walked and its CLR members (Count,
            // Keys, ...) emitted as bogus fields.
            if (property.GetCustomAttribute<JsonExtensionDataAttribute>() != null)
            {
                continue;
            }

            // [JsonIgnore] members (e.g. the AdditionalProperties bag) are not wire fields.
            if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null)
            {
                continue;
            }

            var value = property.GetValue(content);
            var mappedKey = GetPropertyName(property);

            formData.AddValue(mappedKey, value, options);
        }

        return formData;
    }

    private static void AddValue(
        this MultipartFormDataContent formData,
        string mappedKey,
        object? value,
        JsonSerializerOptions? options
    )
    {
        if (value is FernApi.FileParameter fileParameter)
        {
            formData.AddFilePart(mappedKey, fileParameter);
        }
        else if (value is IEnumerable<FernApi.FileParameter> fileParameters)
        {
            // Each element carries its own FileName/ContentType, so distinct filenames come from
            // the caller rather than an index suffix.
            foreach (var file in fileParameters)
            {
                formData.AddFilePart(mappedKey, file);
            }
        }
        // byte[] fallback: multipart binary fields are marked and typed FileParameter under fernMode,
        // so these branches only fire for a binary the retype did not reach (e.g. inside a oneOf body).
        // They stream the bytes but cannot carry a real filename/content-type.
        else if (value is byte[] fileBytes)
        {
            formData.AddFilePart(mappedKey, fileBytes);
        }
        else if (value is IEnumerable<byte[]> files)
        {
            foreach (var file in files)
            {
                formData.AddFilePart(mappedKey, file);
            }
        }
        else if (
            value is System.Collections.IEnumerable items
            && value is not string
            && IsPrimitiveEnumerable(value)
        )
        {
            // An array of primitives (e.g. List<string>) is sent as one repeated field
            // per element rather than reflected into the List's CLR members (Count, ...).
            // Object/dictionary collections fall through to the nested-object branch.
            foreach (var item in items)
            {
                formData.Add(new StringContent(item?.ToString() ?? string.Empty), mappedKey);
            }
        }
        else if (value != null && !IsPrimitive(value.GetType()))
        {
            var nestedContent = new MultipartFormDataContent().AddObject(value, options);
            formData.Add(nestedContent, mappedKey);
        }
        else
        {
            formData.Add(new StringContent(value?.ToString() ?? string.Empty), mappedKey);
        }
    }

    private static void AddFilePart(
        this MultipartFormDataContent formData,
        string mappedKey,
        FernApi.FileParameter file
    )
    {
        if (file.Stream == null)
        {
            return;
        }

        var fileContent = new StreamContent(file.Stream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(
            file.ContentType ?? "application/octet-stream"
        );

        // The real filename supplied on the FileParameter, falling back to omitting the filename
        // (rather than reusing the field name) when the caller did not set one.
        if (file.FileName != null)
        {
            formData.Add(fileContent, mappedKey, file.FileName);
        }
        else
        {
            formData.Add(fileContent, mappedKey);
        }
    }

    // Fallback for a binary not retyped to FileParameter (see the byte[] branches in AddValue). The
    // live FileParameter path above uses .NET's default `Add(content, name, fileName)`; this path
    // has no caller filename, so it keeps FSM-180's field-name-as-filename with explicit RFC 7578
    // quoting for strict parsers.
    private static void AddFilePart(
        this MultipartFormDataContent formData,
        string mappedKey,
        byte[] fileBytes
    )
    {
        var fileContent = new StreamContent(new MemoryStream(fileBytes));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        // .NET's default is unquoted (name=x; filename=x), which stricter multipart parsers reject.
        fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "\"" + mappedKey + "\"",
            FileName = "\"" + mappedKey + "\"",
        };

        formData.Add(fileContent);
    }

    private static bool IsPrimitive(global::System.Type type)
    {
        return type.IsPrimitive || type.IsValueType || type == typeof(string);
    }

    private static bool IsPrimitiveEnumerable(object value)
    {
        var elementType = GetEnumerableElementType(value.GetType());
        // KeyValuePair<,> is a struct, so IsPrimitive would otherwise treat a dictionary's
        // entries as primitives; exclude it so dictionaries fall through to the
        // nested-object branch instead of stringifying each "[key, value]".
        return elementType != null
            && IsPrimitive(elementType)
            && !(
                elementType.IsGenericType
                && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)
            );
    }

    private static global::System.Type? GetEnumerableElementType(global::System.Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return iface.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private static string GetPropertyName(PropertyInfo property)
    {
        var jsonPropertyAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
        if (jsonPropertyAttribute != null)
        {
            return jsonPropertyAttribute.Name;
        }
        return property.Name;
    }
}
