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

    [Test]
    public async Task Format_text_should_list_score_and_both_spans()
    {
        var candidates = new List<DupesCandidate>
        {
            new(0.91, "left.cs", 10, 20, "right.cs", 30, 40, 12, 12),
        };

        var result = DupesOutputFormatter.Format(candidates, "text");

        await Assert.That(result).Contains("DUPLICATE score=0.91");
        await Assert.That(result).Contains("left.cs:10-20");
        await Assert.That(result).Contains("right.cs:30-40");
    }

    [Test]
    public async Task Format_text_should_separate_multiple_candidates_with_blank_line()
    {
        var candidates = new List<DupesCandidate>
        {
            new(1.0, "a.cs", 1, 5, "b.cs", 1, 5, 10, 10),
            new(0.85, "c.cs", 2, 6, "d.cs", 2, 6, 8, 8),
        };

        var result = DupesOutputFormatter.Format(candidates, "text");
        var blocks = result.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.None);

        await Assert.That(blocks.Length).IsEqualTo(2);
        await Assert.That(blocks[0]).Contains("a.cs:1-5");
        await Assert.That(blocks[1]).Contains("c.cs:2-6");
        await Assert.That(result).DoesNotContain("No duplicate candidates found.");
    }

    [Test]
    public async Task Format_unknown_format_should_use_text()
    {
        var candidates = new List<DupesCandidate>
        {
            new(0.5, "x.cs", 1, 2, "y.cs", 3, 4, 5, 5),
        };

        var result = DupesOutputFormatter.Format(candidates, "yaml");

        await Assert.That(result).Contains("DUPLICATE score=0.50");
        await Assert.That(result).DoesNotContain("{");
    }
}
