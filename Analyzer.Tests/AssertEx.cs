namespace Analyzer.Tests;

internal static class AssertEx
{
    public static void Equal<T>(T expected, T actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(message ?? $"Expected '{expected}', got '{actual}'.");
        }
    }

    public static void True(bool condition, string? message = null)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message ?? "Expected condition to be true.");
        }
    }

    public static void Contains(string expectedSubstring, string actual, string? message = null)
    {
        if (!actual.Contains(expectedSubstring, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(message ?? $"Expected '{actual}' to contain '{expectedSubstring}'.");
        }
    }

    public static T Throws<T>(Action action, string? expectedMessageSubstring = null)
        where T : Exception
    {
        try
        {
            action();
        }
        catch (T ex)
        {
            if (!string.IsNullOrWhiteSpace(expectedMessageSubstring))
            {
                Contains(expectedMessageSubstring, ex.Message);
            }

            return ex;
        }

        throw new InvalidOperationException($"Expected exception of type {typeof(T).Name}.");
    }
}
