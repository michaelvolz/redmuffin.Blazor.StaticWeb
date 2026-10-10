using redmuffin.Blazor.StaticWeb.Core;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

/// <summary>
///     Tests for string extension methods with behavior-focused validation.
///     Validates string manipulation functionality using TUnit framework with fluent assertions.
/// </summary>
[Category("Feature:Core")]
[Category("Unit")]
public sealed partial class StringExtensionsTests
{
    [Test]
    public async Task Should_Preserve_Character_Case_When_Reversing()
    {
        // Arrange
        var mixedCaseString = "AbCdEf";
        var expectedReversed = "fEdCbA";

        // Act
        var result = mixedCaseString.ReverseString();

        // Assert
        await Assert.That(result).IsEqualTo(expectedReversed);
    }

    [Test]
    [Arguments("Blazor", "rozalB")]
    [Arguments("racecar", "racecar")]
    [Arguments("", "")]
    public async Task Should_Reverse_String_Correctly_When_Valid_Input_Provided(
        string input,
        string expected
    )
    {
        // Act
        var result = input.ReverseString();

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result).IsEqualTo(expected);
            await Assert.That(result?.Length).IsEqualTo(input.Length);
        }
    }
}
