using TUnit.Core;
using redmuffin.Tools.QualityGates.Analysis;

namespace redmuffin.Tools.QualityGates.Tests.Analysis;

public sealed class DepthDetectorTests
{
    private static string DepthFixturesDir => Path.Combine(
        AppContext.BaseDirectory,
        "Fixtures",
        "depth-fixtures");

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_detect_shallow_private_method_with_no_branching()
    {
        var results = DepthDetector.Analyze(DepthFixturesDir);

        var shallow = results.FirstOrDefault(r => r.MethodName == "ShallowHelper");

        await Assert.That(shallow).IsNotNull();
        await Assert.That(shallow!.IsShallow).IsTrue();
        await Assert.That(shallow.CompositeScore).IsEqualTo(3);
        await Assert.That(shallow.Signals).Contains("shallow(3)");
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_detect_parameter_bloat_over_four_params()
    {
        var results = DepthDetector.Analyze(DepthFixturesDir);

        var bloat = results.FirstOrDefault(r => r.MethodName == "Configure");

        await Assert.That(bloat).IsNotNull();
        await Assert.That(bloat!.ParameterCount).IsEqualTo(5);
        await Assert.That(bloat.CompositeScore).IsEqualTo(1);
        await Assert.That(bloat.Signals).Contains("params(1)");
        await Assert.That(bloat.IsShallow).IsFalse(); // public — not shallow
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_detect_wrong_abstraction_branching_on_parameter()
    {
        var results = DepthDetector.Analyze(DepthFixturesDir);

        var wrong = results.FirstOrDefault(r => r.MethodName == "ApplyMode");

        await Assert.That(wrong).IsNotNull();
        await Assert.That(wrong!.IsWrongAbstraction).IsTrue();
        await Assert.That(wrong.CompositeScore).IsEqualTo(2);
        await Assert.That(wrong.Signals).Contains("wrong-abstraction(2)");
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_detect_combined_shallow_and_param_bloat()
    {
        var results = DepthDetector.Analyze(DepthFixturesDir);

        var combined = results.FirstOrDefault(r => r.MethodName == "Combine");

        await Assert.That(combined).IsNotNull();
        await Assert.That(combined!.IsShallow).IsTrue();
        await Assert.That(combined.ParameterCount).IsEqualTo(5);
        await Assert.That(combined.CompositeScore).IsEqualTo(4);
        await Assert.That(combined.Signals).Contains("shallow(3)");
        await Assert.That(combined.Signals).Contains("params(1)");
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_not_flag_deep_methods_with_branching_and_loc_over_four()
    {
        var results = DepthDetector.Analyze(DepthFixturesDir);

        var compute = results.FirstOrDefault(r => r.MethodName == "Compute");
        var process = results.FirstOrDefault(r => r.MethodName == "Process");

        await Assert.That(compute).IsNull(); // public, LOC>4, branching → composite=0, excluded
        await Assert.That(process).IsNull(); // private but LOC>4, loops+branching → composite=0, excluded
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_not_flag_public_constructor()
    {
        var results = DepthDetector.Analyze(DepthFixturesDir);

        var ctor = results.FirstOrDefault(r => r.MethodName == ".ctor");

        await Assert.That(ctor).IsNull(); // constructors are ConstructorDeclarationSyntax, not MethodDeclarationSyntax
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_return_empty_list_for_non_existent_directory()
    {
        var nonExistent = Path.Combine(DepthFixturesDir, "does-not-exist");
        var results = DepthDetector.Analyze(nonExistent);

        await Assert.That(results).IsNotNull();
        await Assert.That(results).IsEmpty();
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_verify_line_numbers_in_results()
    {
        var results = DepthDetector.Analyze(DepthFixturesDir);

        var shallow = results.First(r => r.MethodName == "ShallowHelper");
        await Assert.That(shallow.LineNumber).IsEqualTo(9);
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_suppress_shallow_signal_for_methods_called_from_three_or_more_places()
    {
        // SharedHelper is private, LOC=1, no branching → Phase 1 says shallow(3)
        // but it's called from FirstCaller, SecondCaller, ThirdCaller → Phase 2 suppresses
        var results = DepthDetector.Analyze(DepthFixturesDir);

        var shared = results.FirstOrDefault(r => r.MethodName == "SharedHelper");
        await Assert.That(shared).IsNull(); // composite would be 0 after suppression → excluded
    }

    [Test]
    [Category("Feature:Depth")]
    public async Task Should_not_suppress_shallow_signal_for_single_caller_methods()
    {
        // ShallowHelper is called from one method (Caller) → still flagged
        var results = DepthDetector.Analyze(DepthFixturesDir);

        var shallow = results.FirstOrDefault(r => r.MethodName == "ShallowHelper");
        await Assert.That(shallow).IsNotNull();
        await Assert.That(shallow!.IsShallow).IsTrue();
        await Assert.That(shallow.CompositeScore).IsEqualTo(3);
    }
}
