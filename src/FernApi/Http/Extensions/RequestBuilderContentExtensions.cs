using FernApi.Http;
using global::System.Net.Http.Headers;
using global::System.Text;
using global::System.Text.Json;

namespace FernApi.Http.Extensions;

/// <summary>
/// fernMode-only (FSDK-1881): a null-skipping counterpart for each RequestBuilder content setter, so
/// an omitted optional request body sends no content rather than a `null`/empty literal. Mirrors the
/// RequestBuilder <c>SetOptional*</c> query/header/cookie family; the request builder selects one of
/// these only for a body the spec marked <c>required: false</c>.
/// </summary>
internal static class RequestBuilderContentExtensions
{
    internal static RequestBuilder SetOptionalContentAsJson(
        this RequestBuilder builder,
        object? content,
        JsonSerializerOptions? options = null,
        MediaTypeHeaderValue? mediaType = null
    ) => content is null ? builder : builder.SetContentAsJson(content, options, mediaType);

    internal static RequestBuilder SetOptionalContentAsText(
        this RequestBuilder builder,
        string? content,
        string mediaType,
        Encoding? encoding = null
    ) => content is null ? builder : builder.SetContentAsText(content, mediaType, encoding);

    internal static RequestBuilder SetOptionalContentAsBinary(
        this RequestBuilder builder,
        byte[]? content,
        MediaTypeHeaderValue mediaType
    ) => content is null ? builder : builder.SetContentAsBinary(content, mediaType);

    internal static RequestBuilder SetOptionalUrlEncodedContent(
        this RequestBuilder builder,
        object? content,
        JsonSerializerOptions? options = null
    ) => content is null ? builder : builder.SetUrlEncodedContent(content, options);

    internal static RequestBuilder SetOptionalContentAsMultipartFormData(
        this RequestBuilder builder,
        object? content,
        JsonSerializerOptions? options = null
    ) => content is null ? builder : builder.SetContentAsMultipartFormData(content, options);
}
