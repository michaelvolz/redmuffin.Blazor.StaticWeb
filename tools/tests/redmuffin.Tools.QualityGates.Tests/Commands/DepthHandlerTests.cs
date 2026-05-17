using TUnit.Core;
using redmuffin.Tools.QualityGates.Analysis;
using redmuffin.Tools.QualityGates.Commands;

namespace redmuffin.Tools.QualityGates.Tests.Commands;

public sealed class DepthHandlerTests
{
    private static DepthResult FailResult => new(
        "ShallowHelper", "ShallowMethod.cs", 10,
        true, 1, false, false, 3, ["shallow(3)"]);

    private static DepthResult WarnResult => new(
        "ApplyMode", "Abstraction.cs", 8,
        false, 2, true, false, 2, ["wrong-abstraction(2)"]);

    private static DepthResult InfoResult => new(
        "Configure", "Bloat.cs", 5,
        false, 5, false, false, 1, ["params(1)"]);

    private static DepthResult FailCombinedResult => new(
        "Combine", "MixedSignals.cs", 3,
        true, 5, false, false, 4, ["shallow(3)", "params(1)"]);

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_return_exit_zero_for_empty_results()
    {
        using var output = new StringWriter();
        var results = Array.Empty<DepthResult>();

        var exitCode = DepthHandler.Run(results, output: output);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_return_exit_two_when_fail_method_present()
    {
        using var output = new StringWriter();
        var results = new DepthResult[] { FailResult };

        var exitCode = DepthHandler.Run(results, output: output);

        await Assert.That(exitCode).IsEqualTo(2);
        await Assert.That(output.ToString()).Contains("FAIL");
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_return_exit_two_with_mixed_severities_when_fail_present()
    {
        using var output = new StringWriter();
        var results = new DepthResult[] { WarnResult, FailResult, InfoResult };

        var exitCode = DepthHandler.Run(results, output: output);

        await Assert.That(exitCode).IsEqualTo(2);
        await Assert.That(output.ToString()).Contains("FAIL");
        await Assert.That(output.ToString()).Contains("WARN");
        await Assert.That(output.ToString()).Contains("INFO");
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_return_exit_zero_when_only_warn_and_info_present()
    {
        using var output = new StringWriter();
        var results = new DepthResult[] { WarnResult, InfoResult };

        var exitCode = DepthHandler.Run(results, output: output);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output.ToString()).Contains("WARN");
        await Assert.That(output.ToString()).Contains("INFO");
        await Assert.That(output.ToString()).DoesNotContain("FAIL");
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_return_exit_zero_when_only_info_present()
    {
        using var output = new StringWriter();
        var results = new DepthResult[] { InfoResult };

        var exitCode = DepthHandler.Run(results, output: output);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output.ToString()).Contains("INFO");
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_classify_composite_exactly_three_as_fail()
    {
        using var output = new StringWriter();
        var results = new DepthResult[] { FailResult }; // composite=3

        var exitCode = DepthHandler.Run(results, output: output);

        await Assert.That(exitCode).IsEqualTo(2);
        await Assert.That(output.ToString()).Contains("FAIL");
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_classify_composite_exactly_two_as_warn()
    {
        using var output = new StringWriter();
        var results = new DepthResult[] { WarnResult }; // composite=2

        var exitCode = DepthHandler.Run(results, output: output);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output.ToString()).Contains("WARN");
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_support_custom_fail_threshold()
    {
        using var output = new StringWriter();
        var result = new DepthResult(
            "Test", "Test.cs", 1,
            false, 2, true, false, 2, ["wrong-abstraction(2)"]);

        // threshold=2 means composite≥2 FAILs
        var exitCode = DepthHandler.Run(new[] { result }, failThreshold: 2, output: output);

        await Assert.That(exitCode).IsEqualTo(2);
        await Assert.That(output.ToString()).Contains("FAIL");
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_write_output_format_with_file_line_method_composite_and_signals()
    {
        using var output = new StringWriter();
        var results = new DepthResult[] { FailCombinedResult };

        DepthHandler.Run(results, output: output);

        var text = output.ToString();
        await Assert.That(text).Contains("FAIL");
        await Assert.That(text).Contains("MixedSignals.cs:3");
        await Assert.That(text).Contains("Combine()");
        await Assert.That(text).Contains("composite=4");
        await Assert.That(text).Contains("shallow(3)");
        await Assert.That(text).Contains("params(1)");
    }
}
