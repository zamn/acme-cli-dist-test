using global::System.Diagnostics.CodeAnalysis;
using global::System.Runtime.Serialization;

namespace FernApi.Core;

internal static class Extensions
{
    public static string Stringify(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field is not null)
        {
            var attribute = (EnumMemberAttribute?)
                global::System.Attribute.GetCustomAttribute(field, typeof(EnumMemberAttribute));
            return attribute?.Value ?? value.ToString();
        }
        return value.ToString();
    }

    /// <summary>
    /// Asserts that a condition is true, throwing an exception with the specified message if it is false.
    /// </summary>
    /// <param name="condition">The condition to assert.</param>
    /// <param name="message">The exception message if the assertion fails.</param>
    /// <exception cref="Exception">Thrown when the condition is false.</exception>
    internal static void Assert(this object value, bool condition, string message)
    {
        if (!condition)
        {
            throw new global::System.Exception(message);
        }
    }

    /// <summary>
    /// Asserts that a value is not null, throwing an exception with the specified message if it is null.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to assert.</typeparam>
    /// <param name="value">The value to assert is not null.</param>
    /// <param name="message">The exception message if the assertion fails.</param>
    /// <returns>The non-null value.</returns>
    /// <exception cref="Exception">Thrown when the value is null.</exception>
    internal static TValue Assert<TValue>(
        this object _unused,
        [NotNull] TValue? value,
        string message
    )
        where TValue : class
    {
        if (value is null)
        {
            throw new global::System.Exception(message);
        }
        return value;
    }
}

/// <summary>Structural equality for generated model members.</summary>
/// <remarks>
/// <c>EqualityComparer&lt;T&gt;.Default</c> is REFERENCE equality for a list, array or dictionary, so
/// two models built with equal-but-distinct collections would still compare unequal — which is most
/// of the point of giving models value equality at all. Dictionaries are compared by key lookup
/// rather than as sequences, because their enumeration order is unspecified.
/// </remarks>
internal static class ValueEquality
{
    internal static bool AreEqual<T>(T left, T right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }
        if (left is null || right is null)
        {
            return false;
        }
        // A string is IEnumerable<char>, so it has to be settled before the sequence branch or it
        // would be compared character by character. Tested on BOTH sides: when only one is a string
        // the two are not equal, and falling through would compare a string against a sequence.
        if (left is string || right is string)
        {
            return left is string leftText
                && right is string rightText
                && string.Equals(leftText, rightText, global::System.StringComparison.Ordinal);
        }
        // Likewise settled on both sides. A dictionary IS an IEnumerable, so a dictionary paired
        // with a plain sequence would otherwise fall into the sequence branch and be compared as a
        // list of KeyValuePairs against unrelated elements.
        var leftIsMap = left is global::System.Collections.IDictionary;
        var rightIsMap = right is global::System.Collections.IDictionary;
        if (leftIsMap || rightIsMap)
        {
            return leftIsMap
                && rightIsMap
                && DictionariesEqual(
                    (global::System.Collections.IDictionary)left,
                    (global::System.Collections.IDictionary)right
                );
        }
        if (
            left is global::System.Collections.IEnumerable leftSequence
            && right is global::System.Collections.IEnumerable rightSequence
        )
        {
            return SequencesEqual(leftSequence, rightSequence);
        }
        return global::System.Collections.Generic.EqualityComparer<T>.Default.Equals(left, right);
    }

    internal static int GetHashCodeOf<T>(T value)
    {
        // Null-safe by hand: EqualityComparer<T>.Default.GetHashCode(null) is not safe on every BCL
        // version this package targets.
        if (value is null)
        {
            return 0;
        }
        if (value is string)
        {
            return value.GetHashCode();
        }
        if (value is global::System.Collections.IEnumerable sequence)
        {
            // RECURSES. Calling item.GetHashCode() directly would hash a nested collection by
            // reference while AreEqual compares it structurally, so two equal `List<List<long>>`
            // would hash differently — breaking the GetHashCode contract and with it HashSet and
            // Distinct for any model holding a collection of collections.
            //
            // Summed, so the hash is order-independent. Weaker than the ordered AreEqual, still
            // correct: equal sequences are the same multiset and so hash the same.
            unchecked
            {
                var hash = 19;
                foreach (var item in sequence)
                {
                    hash += GetHashCodeOf(item);
                }
                return hash;
            }
        }
        return value.GetHashCode();
    }

    private static bool DictionariesEqual(
        global::System.Collections.IDictionary left,
        global::System.Collections.IDictionary right
    )
    {
        if (left.Count != right.Count)
        {
            return false;
        }
        foreach (global::System.Collections.DictionaryEntry entry in left)
        {
            if (!right.Contains(entry.Key))
            {
                return false;
            }
            if (!AreEqual(entry.Value, right[entry.Key]))
            {
                return false;
            }
        }
        return true;
    }

    private static bool SequencesEqual(
        global::System.Collections.IEnumerable left,
        global::System.Collections.IEnumerable right
    )
    {
        var leftCursor = left.GetEnumerator();
        var rightCursor = right.GetEnumerator();
        try
        {
            while (leftCursor.MoveNext())
            {
                if (!rightCursor.MoveNext() || !AreEqual(leftCursor.Current, rightCursor.Current))
                {
                    return false;
                }
            }
            return !rightCursor.MoveNext();
        }
        finally
        {
            (leftCursor as global::System.IDisposable)?.Dispose();
            (rightCursor as global::System.IDisposable)?.Dispose();
        }
    }
}
