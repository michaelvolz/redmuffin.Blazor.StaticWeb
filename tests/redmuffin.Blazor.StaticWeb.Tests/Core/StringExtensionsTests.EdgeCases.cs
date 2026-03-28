using redmuffin.Blazor.StaticWeb.Core;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

[Category("Feature:Core")]
public sealed partial class StringExtensionsTests
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
}