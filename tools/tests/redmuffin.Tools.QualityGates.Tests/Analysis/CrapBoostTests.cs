namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using Microsoft.CodeAnalysis.CSharp;
using redmuffin.Tools.QualityGates.Analysis;

/// <summary>
///     Additional test cases for methods hovering just above CRAP threshold.
/// </summary>
public sealed class CrapBoostTests
{
    [Test]
    public async Task TestNormalizer_VisitLiteralExpression_should_handle_all_literal_kinds()
    {
        var source = "class C { void M() { _ = 'c'; _ = \"s\"; _ = 42; _ = 3.14; _ = true; _ = false; _ = null; _ = default; } }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();
        var method = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First();

        var features = redmuffin.Tools.QualityGates.Analysis.TestNormalizer.Normalize(method);
        await Assert.That(features).IsNotEmpty();
    }

    [Test]
    public async Task DupesDetector_ScanFiles_should_scan_directory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(tempDir, "Test.cs"), "class C { void M() { if (true) return; } }").ConfigureAwait(false);

            var options = new redmuffin.Tools.QualityGates.Commands.DupesOptions(
                Threshold: 0.82, MinLines: 1, MinNodes: 1,
                Format: "text", Paths: [tempDir]);

            var results = redmuffin.Tools.QualityGates.Analysis.DupesDetector.FindDuplicates(options);
            await Assert.That(results).IsNotNull();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task MutationApplicator_Mutate_should_handle_arithmetic_variants()
    {
        var source = "class C { int M(int a, int b) { return a + b; } }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();
        var node = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax>()
            .First();
        var site = new redmuffin.Tools.QualityGates.Analysis.MutationSite(
            0, redmuffin.Tools.QualityGates.Analysis.MutationCategory.Arithmetic, 1, 20, "test",
            SyntaxKind.AddExpression, SyntaxKind.SubtractExpression, node);

        var mutatedSource = redmuffin.Tools.QualityGates.Analysis.MutationApplicator.Apply(source, 0, site);
        await Assert.That(mutatedSource).IsNotNull();
        await Assert.That(mutatedSource).IsNotEqualTo(source);
    }

    [Test]
    public async Task MutationApplicator_Mutate_should_handle_comparison_variants()
    {
        var source = "class C { bool M(int a, int b) { return a > b; } }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();
        var node = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax>()
            .First();
        var site = new redmuffin.Tools.QualityGates.Analysis.MutationSite(
            0, redmuffin.Tools.QualityGates.Analysis.MutationCategory.Comparison,
            1, 20, "test",
            SyntaxKind.GreaterThanExpression, SyntaxKind.LessThanExpression, node);

        var mutatedSource = redmuffin.Tools.QualityGates.Analysis.MutationApplicator.Apply(source, 0, site);
        await Assert.That(mutatedSource).IsNotNull();
        await Assert.That(mutatedSource).Contains("<");
    }

    [Test]
    public async Task BuildHeaderLines_with_manifest_and_changes()
    {
        var manifest = new redmuffin.Tools.QualityGates.Analysis.Manifest(
            Version: 1, TestedAt: DateTime.UtcNow, ModuleHash: "abc", Forms: []);

        var lines = redmuffin.Tools.QualityGates.Commands.MutateHandler.BuildHeaderLines(
            "/src/Bar.cs", totalSites: 5, coveredSites: 3, uncoveredSites: 2,
            changedCount: 1, existingManifest: manifest);

        await Assert.That(lines[1]).DoesNotContain("none");
        await Assert.That(lines[6]).Contains("yes");
        await Assert.That(lines[7]).Contains("yes");
    }

    [Test]
    public async Task AllLowComplexity_empty_returns_true()
    {
        var actual = redmuffin.Tools.QualityGates.Analysis.ScrapDuplication.AllLowComplexity([]);
        await Assert.That(actual).IsTrue();
    }

    [Test]
    public async Task AllLowComplexity_high_lines_returns_false()
    {
        var m = new redmuffin.Tools.QualityGates.Analysis.ScrapDuplication.SimpleMethodMetrics(
            LineCount: 20, AssertionCount: 0, BranchCount: 0, SetupDepth: 0);
        var actual = redmuffin.Tools.QualityGates.Analysis.ScrapDuplication.AllLowComplexity([m]);
        await Assert.That(actual).IsFalse();
    }

    [Test]
    public async Task AllLowComplexity_valid_returns_true()
    {
        var m = new redmuffin.Tools.QualityGates.Analysis.ScrapDuplication.SimpleMethodMetrics(
            LineCount: 5, AssertionCount: 1, BranchCount: 0, SetupDepth: 1);
        var actual = redmuffin.Tools.QualityGates.Analysis.ScrapDuplication.AllLowComplexity([m]);
        await Assert.That(actual).IsTrue();
    }

    [Test]
    public async Task IsFailingReport_with_data_returns_true()
    {
        var code = "class X { void M() { int a = 1; } }";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();
        var method = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();
        var testMethod = new redmuffin.Tools.QualityGates.Analysis.TestMethod("M", "/x.cs", 1, 2, method, "X");

        var report = redmuffin.Tools.QualityGates.Analysis.ScrapScorer.ScoreFile(
            methods: [testMethod],
            duplicationResults: new([], [], [], 0),
            extractionPressure: new(0, [], 0, 0));
        var actual = redmuffin.Tools.QualityGates.Commands.ScrapHandler.IsFailingReport(report);
        await Assert.That(actual).IsTrue();
    }

    [Test]
    public async Task Format_json_returns_non_empty_string()
    {
        var result = redmuffin.Tools.QualityGates.Analysis.DupesOutputFormatter.Format([], "json");
        await Assert.That(string.IsNullOrWhiteSpace(result)).IsFalse();
    }

    [Test]
    public async Task Format_text_returns_non_empty_string()
    {
        var result = redmuffin.Tools.QualityGates.Analysis.DupesOutputFormatter.Format([], "text");
        await Assert.That(string.IsNullOrWhiteSpace(result)).IsFalse();
    }

    [Test]
    public async Task MissingCoverageError_returns_null()
    {
        var result = redmuffin.Tools.QualityGates.Commands.CrapCommand.MissingCoverageError();
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ResolveCoverage_null_without_auto_returns_null()
    {
        var result = redmuffin.Tools.QualityGates.Commands.CrapCommand.ResolveCoverage(
            coveragePath: null, testProjectPath: null, autoCoverage: false);
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ResolveCoverage_with_path_returns_path()
    {
        var result = redmuffin.Tools.QualityGates.Commands.CrapCommand.ResolveCoverage(
            coveragePath: "/tmp/cov.xml", testProjectPath: null, autoCoverage: false);
        await Assert.That(result).IsEqualTo("/tmp/cov.xml");
    }

    [Test]
    public async Task BuildSummaryLine_all_pass_returns_summary()
    {
        var line = redmuffin.Tools.QualityGates.Commands.AllCommand.BuildSummaryLine(
            overallExit: 0, crapExit: 0, scrapExit: 0,
            archConfig: "/cfg.yml", archExit: 0,
            mutateSource: "/src.cs", mutateExit: 0,
            runDupes: true, dupesExit: 0);
        await Assert.That(line).Contains("PASS");
        await Assert.That(line).Contains("Overall: PASS");
    }

    [Test]
    public async Task HasAnyFailure_empty_list_returns_false()
    {
        var actual = redmuffin.Tools.QualityGates.Commands.ScrapHandler.HasAnyFailure([]);
        await Assert.That(actual).IsFalse();
    }

    [Test]
    public async Task ClassifyChannel_harmful_with_many_shared_forms()
    {
        var result = redmuffin.Tools.QualityGates.Analysis.ScrapDuplication.ClassifyChannel(
            methods: [],
            sharedForms: 3,
            variablePoints: 1,
            metrics: []);
        await Assert.That(result).IsEqualTo(
            redmuffin.Tools.QualityGates.Analysis.ChannelType.Harmful);
    }

    [Test]
    public async Task ClassifyChannel_case_matrix_with_low_complexity()
    {
        var m = new redmuffin.Tools.QualityGates.Analysis.ScrapDuplication.SimpleMethodMetrics(
            LineCount: 5, AssertionCount: 1, BranchCount: 0, SetupDepth: 1);
        var result = redmuffin.Tools.QualityGates.Analysis.ScrapDuplication.ClassifyChannel(
            methods: [],
            sharedForms: 1,
            variablePoints: 10,
            metrics: [m]);
        await Assert.That(result).IsEqualTo(
            redmuffin.Tools.QualityGates.Analysis.ChannelType.CaseMatrix);
    }

    [Test]
    public async Task NormalizeCreation_with_arguments()
    {
        var code = "new Foo(1, 2)";
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();
        var creation = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ObjectCreationExpressionSyntax>().First();
        var result = redmuffin.Tools.QualityGates.Analysis.DupesNormalizer.Normalize(creation);
        await Assert.That(result.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task ResolveCoverage_with_auto_coverage()
    {
        var result = redmuffin.Tools.QualityGates.Commands.CrapCommand.ResolveCoverage(
            coveragePath: null, testProjectPath: "/tmp/proj", autoCoverage: true);
        // Returns null because test project doesn't exist
        await Assert.That(result).IsNull();
    }
}
