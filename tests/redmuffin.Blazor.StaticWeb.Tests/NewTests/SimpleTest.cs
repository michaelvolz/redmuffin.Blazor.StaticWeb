using TUnit.Core;
using TUnit.Assertions;

namespace redmuffin.Blazor.StaticWeb.Tests.NewTests
{
    public class SimpleTest
    {
        [Test]
        public async Task PassingTest()
        {
            int x = 1;
            await Assert.That(x).IsEqualTo(1);
        }
    }
}