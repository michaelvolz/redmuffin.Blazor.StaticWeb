namespace redmuffin.Blazor.StaticWeb.Tests;

public class SimpleTest
{
    [Test]
    public async Task PassingTest()
    {
        var x = 1;
        await Assert.That(x).IsEqualTo(1);
    }
}