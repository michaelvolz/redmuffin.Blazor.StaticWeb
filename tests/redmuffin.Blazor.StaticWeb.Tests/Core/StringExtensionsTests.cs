using redmuffin.Blazor.StaticWeb.Core;

#pragma warning disable VSTHRD200

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

public class StringExtensionsTests
{
    [Test]
    [Arguments("Blazor", "rozalB")]
    [Arguments("racecar", "racecar")]
    [Arguments("", "")]
    [Arguments(null, null)]
    public async Task ReverseString_ReturnsExpectedResult(string? input, string? expected)
    {
        var result = input.ReverseString();
        await Assert.That(result).IsEqualTo(expected);
    }
}