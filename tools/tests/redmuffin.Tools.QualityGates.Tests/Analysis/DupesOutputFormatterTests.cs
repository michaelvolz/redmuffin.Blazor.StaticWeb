namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using redmuffin.Tools.QualityGates.Analysis;

public sealed class DupesOutputFormatterTests
{
    [Test]
    public async Task FormatJson_should_produce_indented_json()
    {
        var candidates = new List<DupesCandidate>
        {
            new(1.0, "a.cs", 1, 5, "b.cs", 1, 5, 10, 10),
        };

        var result = DupesOutputFormatter.Format(candidates, "json");
        await Assert.That(result).Contains("\n");
    }

    [Test]
    public async Task Format_should_return_empty_message_for_zero_candidates()
    {
        var result = DupesOutputFormatter.Format([], "text");
        await Assert.That(result).IsEqualTo("No duplicate candidates found.");
    }
}
