using TUnit.Core;

namespace SurvivorTarget;

public sealed class SurvivorTests
{
    [Test]
    public async Task Add_should_return_sum()
    {
        var result = Survivor.Add(2, 3);
        await Assert.That(result).IsEqualTo(5);
    }

    // Intentionally NOT testing Multiply — survivor for MutationRunnerTests
}
