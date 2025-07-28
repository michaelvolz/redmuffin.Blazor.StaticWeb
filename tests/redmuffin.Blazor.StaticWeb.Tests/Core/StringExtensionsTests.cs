using redmuffin.Blazor.StaticWeb.Core;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

/// <summary>
///     Tests for string extension methods with behavior-focused validation.
///     Validates string manipulation functionality using TUnit framework with fluent assertions.
/// </summary>
public sealed class StringExtensionsTests
{
    [Test]
    public async Task Should_Handle_Empty_String_When_Reversing()
    {
        // Arrange
        var emptyString = string.Empty;

        // Act
        var result = emptyString.ReverseString();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result).IsEqualTo(string.Empty);
            await Assert.That(result).IsNotNull();
        }
    }

    [Test]
    public async Task Should_Handle_Null_Input_When_Reversing()
    {
        // Arrange
        string? nullString = null;

        // Act
        var result = nullString.ReverseString();

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Should_Handle_Special_Characters_When_Reversing()
    {
        // Arrange
        var specialString = "Hello, World! 123";
        var expectedReversed = "321 !dlroW ,olleH";

        // Act
        var result = specialString.ReverseString();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result).IsEqualTo(expectedReversed);
            await Assert.That(result).StartsWith("321");
            await Assert.That(result).EndsWith("H");
        }
    }

    [Test]
    public async Task Should_Preserve_Character_Case_When_Reversing()
    {
        // Arrange
        var mixedCaseString = "AbCdEf";
        var expectedReversed = "fEdCbA";

        // Act
        var result = mixedCaseString.ReverseString();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result).IsEqualTo(expectedReversed);
            await Assert.That(result).Contains("f");
            await Assert.That(result).Contains("A");
        }
    }

    [Test]
    [Arguments("Blazor", "rozalB")]
    [Arguments("racecar", "racecar")]
    [Arguments("", "")]
    [Arguments(null, null)]
    public async Task Should_Reverse_String_Correctly_When_Valid_Input_Provided(string? input, string? expected)
    {
        // Act
        var result = input.ReverseString();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result).IsEqualTo(expected);

            if (input is not null) await Assert.That(result?.Length).IsEqualTo(input.Length);
        }
    }
}