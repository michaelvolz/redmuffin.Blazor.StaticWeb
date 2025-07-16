namespace redmuffin.Blazor.StaticWeb.Core;

public static class StringExtensions
{
    /// <summary>
    ///     Returns a new string with the characters in reverse order.
    /// </summary>
    /// <param name="input">The string to reverse.</param>
    /// <returns>The reversed string, or null if input is null.</returns>
    public static string? ReverseString(this string? input)
    {
        return input is null ? null : new string(input.Reverse().ToArray());
    }
}