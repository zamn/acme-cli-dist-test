using System.Text.Json;
using NUnit.Framework;

namespace FernApi.Tests;

/// <summary>
/// Structural JSON comparison for the generated suites.
/// </summary>
/// <remarks>
/// Not a string comparison: two serializations of the same document legitimately differ in property
/// order and in number formatting (<c>1</c> vs <c>1.0</c>), so comparing text would fail on output
/// that is correct. Comparing parsed values instead keeps the assertion exact about what is on the
/// wire — a missing, extra, or changed property still fails.
/// </remarks>
internal static class JsonAssert
{
    internal static void AreEquivalent(string expected, string? actual, string because)
    {
        Assert.That(actual, Is.Not.Null, because);

        using var expectedDocument = JsonDocument.Parse(expected);
        using var actualDocument = JsonDocument.Parse(actual!);

        var difference = FindDifference(
            expectedDocument.RootElement,
            actualDocument.RootElement,
            "$"
        );
        if (difference != null)
        {
            Assert.Fail(
                $"{because}{System.Environment.NewLine}{difference}{System.Environment.NewLine}Expected: {expected}{System.Environment.NewLine}Actual:   {actual}"
            );
        }
    }

    /// <summary>
    /// Every property of <paramref name="expectedSubset"/> is present in <paramref name="actual"/>
    /// with the same value. Extra properties in <paramref name="actual"/> are allowed.
    /// </summary>
    /// <remarks>
    /// Used for request bodies, where an exact comparison would be asserting the SDK's
    /// optional-property policy (is an unset optional omitted, or written as null?) rather than the
    /// payload. The subset is built from the properties the call site definitely supplied, so a
    /// serializer that drops or mangles one of them still fails.
    /// </remarks>
    internal static void ContainsSubset(string expectedSubset, string? actual, string because)
    {
        Assert.That(actual, Is.Not.Null.And.Not.Empty, because);

        using var expectedDocument = JsonDocument.Parse(expectedSubset);
        using var actualDocument = JsonDocument.Parse(actual!);

        var difference = FindMissing(expectedDocument.RootElement, actualDocument.RootElement, "$");
        if (difference != null)
        {
            Assert.Fail(
                $"{because}{System.Environment.NewLine}{difference}{System.Environment.NewLine}Expected to contain: {expectedSubset}{System.Environment.NewLine}Actual:              {actual}"
            );
        }
    }

    private static string? FindMissing(JsonElement expected, JsonElement actual, string path)
    {
        if (expected.ValueKind != JsonValueKind.Object || actual.ValueKind != JsonValueKind.Object)
        {
            return FindDifference(expected, actual, path);
        }

        foreach (var property in expected.EnumerateObject())
        {
            if (!actual.TryGetProperty(property.Name, out var actualValue))
            {
                if (property.Value.ValueKind == JsonValueKind.Null)
                {
                    continue;
                }
                return $"{path}: property \"{property.Name}\" is missing.";
            }

            var difference = FindMissing(property.Value, actualValue, $"{path}.{property.Name}");
            if (difference != null)
            {
                return difference;
            }
        }

        return null;
    }

    private static string? FindDifference(JsonElement expected, JsonElement actual, string path)
    {
        if (expected.ValueKind != actual.ValueKind)
        {
            return $"{path}: expected {expected.ValueKind}, got {actual.ValueKind}.";
        }

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                return FindObjectDifference(expected, actual, path);
            case JsonValueKind.Array:
                return FindArrayDifference(expected, actual, path);
            case JsonValueKind.String:
                return expected.GetString() == actual.GetString()
                    ? null
                    : $"{path}: expected \"{expected.GetString()}\", got \"{actual.GetString()}\".";
            case JsonValueKind.Number:
                return expected.GetDouble().Equals(actual.GetDouble())
                    ? null
                    : $"{path}: expected {expected.GetRawText()}, got {actual.GetRawText()}.";
            default:
                return null;
        }
    }

    private static string? FindObjectDifference(
        JsonElement expected,
        JsonElement actual,
        string path
    )
    {
        var actualNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in actual.EnumerateObject())
        {
            actualNames.Add(property.Name);
        }

        foreach (var property in expected.EnumerateObject())
        {
            if (!actual.TryGetProperty(property.Name, out var actualValue))
            {
                // An absent property and one written as `null` are the same document here: the
                // serializer's ignore-null policy decides which one it emits, and asserting that
                // choice would be asserting the policy rather than the payload.
                if (property.Value.ValueKind == JsonValueKind.Null)
                {
                    continue;
                }
                return $"{path}: property \"{property.Name}\" is missing.";
            }

            actualNames.Remove(property.Name);
            var difference = FindDifference(property.Value, actualValue, $"{path}.{property.Name}");
            if (difference != null)
            {
                return difference;
            }
        }

        foreach (var extra in actualNames)
        {
            if (actual.GetProperty(extra).ValueKind == JsonValueKind.Null)
            {
                continue;
            }
            return $"{path}: unexpected property \"{extra}\".";
        }

        return null;
    }

    private static string? FindArrayDifference(
        JsonElement expected,
        JsonElement actual,
        string path
    )
    {
        var expectedItems = expected.EnumerateArray().ToList();
        var actualItems = actual.EnumerateArray().ToList();
        if (expectedItems.Count != actualItems.Count)
        {
            return $"{path}: expected {expectedItems.Count} item(s), got {actualItems.Count}.";
        }

        for (var index = 0; index < expectedItems.Count; index++)
        {
            var difference = FindDifference(
                expectedItems[index],
                actualItems[index],
                $"{path}[{index}]"
            );
            if (difference != null)
            {
                return difference;
            }
        }

        return null;
    }
}
