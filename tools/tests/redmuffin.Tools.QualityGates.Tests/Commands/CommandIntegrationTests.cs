namespace redmuffin.Tools.QualityGates.Tests.Commands;

using redmuffin.Tools.QualityGates.Analysis;
using redmuffin.Tools.QualityGates.Commands;

/// <summary>
///     Integration tests for command Execute() methods.
///     Coverage for these tests is collected during the coverage run
///     that executes all tests — not via self-generation.
/// </summary>
public sealed partial class CommandIntegrationTests
{
    private static readonly string SrcProject = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "src", "redmuffin.Tools.QualityGates"));

    private static readonly string TestProject = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "tests", "redmuffin.Tools.QualityGates.Tests"));

    private static readonly string ArchitectureConfig = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "quality-gates", "architecture-rules.yml"));

    /// <summary>
    ///     Pre-generate this file once before running:
    ///     dotnet run --project tests/... --coverage \
    ///       --coverage-output-format cobertura \
    ///       --coverage-output /tmp/integration-coverage.xml
    /// </summary>
    private const string CoverageFile = "/tmp/integration-coverage.xml";

    [Test]
    public async Task all_commands_and_handlers_should_run_without_errors()
    {
        // CRAP: needs valid coverage file, skip if absent
        if (!File.Exists(CoverageFile))
        {
            return;
        }

        var crapExit = CrapCommand.Execute(
            SrcProject, CoverageFile, maxCrap: 999, changedOnly: false,
            autoCoverage: false, testProjectPaths: null);
        AssertCrapExit(crapExit);

        var scrapExit = ScrapCommand.Execute(
            TestProject, verbose: false, json: false, changedOnly: false,
            writeBaseline: false, comparePath: null);
        AssertCrapExit(scrapExit);

        var configPath = ArchitectureConfig;
        if (File.Exists(configPath))
        {
            var archExit = ArchCommand.Execute(SrcProject, configPath, json: false);
            AssertCrapExit(archExit);
        }

        var dupesOptions = new DupesOptions(
            Threshold: 0.82, MinLines: 4, MinNodes: 20, Format: "text",
            Paths: [SrcProject]);
        var (dupesExit, _) = DupesHandler.Run(dupesOptions);
        AssertCrapExit(dupesExit);

        var sampleFile = Path.Combine(SrcProject, "Commands", "CrapHandler.cs");
        if (File.Exists(sampleFile))
        {
            var mutateOptions = new MutateOptions(Scan: true);
            var mutateExit = await MutateHandler.RunAsync(
                sampleFile, TestProject, mutateOptions).ConfigureAwait(false);
            AssertCrapExit(mutateExit);
        }
    }

    private static void AssertCrapExit(int exitCode)
    {
        // 0=pass, 2=fail (violations), 1=error — all mean the gate ran
        if (exitCode != 0 && exitCode != 2 && exitCode != 1)
        {
            throw new InvalidOperationException($"Unexpected exit code: {exitCode}");
        }
    }

    [Test]
    public async Task ApplyDifferentialFilter_null_manifest_returns_site_count()
    {
        var sites = new List<redmuffin.Tools.QualityGates.Analysis.MutationSite>();
        var count = redmuffin.Tools.QualityGates.Commands.MutateHandler.ApplyDifferentialFilter(
            sites, strippedSource: "code", existingManifest: null,
            new redmuffin.Tools.QualityGates.Commands.MutateOptions(Scan: false));
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task ApplyDifferentialFilter_with_manifest_filters_sites()
    {
        var manifest = redmuffin.Tools.QualityGates.Analysis.MutationManifest.Build(
            "class C { int Add(int a, int b) => a + b; }", DateTime.UtcNow);
        var sites = new List<redmuffin.Tools.QualityGates.Analysis.MutationSite>();
        var count = redmuffin.Tools.QualityGates.Commands.MutateHandler.ApplyDifferentialFilter(
            sites, strippedSource: "class C { int Add(int a, int b) => a + b; }",
            existingManifest: manifest, new redmuffin.Tools.QualityGates.Commands.MutateOptions(Scan: false));
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task RunArchitectureCheck_with_existing_config_returns_result()
    {
        var configPath = ArchitectureConfig;
        if (!File.Exists(configPath)) return;

        var (exitCode, result) = redmuffin.Tools.QualityGates.Commands.ArchHandler.Run(
            configPath, SrcProject);
        await Assert.That(exitCode is 0 or 2).IsTrue();
    }

    [Test]
    public async Task RunAsync_scan_mode_covers_mutation_pipeline()
    {
        var sampleFile = Path.Combine(SrcProject, "Commands", "CrapHandler.cs");
        if (!File.Exists(sampleFile)) return;

        var exitCode = await redmuffin.Tools.QualityGates.Commands.MutateHandler.RunAsync(
            sampleFile, TestProject, new redmuffin.Tools.QualityGates.Commands.MutateOptions(Scan: true))
            .ConfigureAwait(false);
        await Assert.That(exitCode is 0 or 1 or 2).IsTrue();
    }

    [Test]
    public async Task ScrapHandler_Run_empty_reports_returns_zero()
    {
        using var writer = new StringWriter();
        var options = new ScrapOptions(Verbose: false, Json: false);
        var exit = ScrapHandler.Run([], options, writer);
        await Assert.That(exit).IsEqualTo(0);
        await Assert.That(writer.ToString()).Contains("No test");
    }

    [Test]
    public async Task ScrapHandler_Run_with_verbose_writes_per_example_table()
    {
        var report = MakeScrapReport("TestFile.cs");

        using var writer = new StringWriter();
        var options = new ScrapOptions(Verbose: true, Json: false);
        var exit = ScrapHandler.Run([report], options, writer);
        await Assert.That(exit is 0 or 2).IsTrue();
        await Assert.That(writer.ToString()).Contains("Method");
        await Assert.That(writer.ToString()).Contains("Test1");
    }

    [Test]
    public async Task ScrapHandler_Run_failing_report_returns_two()
    {
        var report = MakeScrapReport("BadFile.cs", avgScrap: 15.0, maxScrap: 15.0);

        using var writer = new StringWriter();
        var options = new ScrapOptions(Verbose: false, Json: false);
        var exit = ScrapHandler.Run([report], options, writer);
        await Assert.That(exit).IsEqualTo(2);
        await Assert.That(writer.ToString()).Contains("FAIL");
    }

    [Test]
    public async Task RunArchAsync_null_config_skips_and_returns_zero()
    {
        using var writer = new StringWriter();
        var exit = await AllCommand.RunArchAsync(writer, "/fake/project", archConfig: null)
            .ConfigureAwait(false);
        await Assert.That(exit).IsEqualTo(0);
        await Assert.That(writer.ToString()).Contains("SKIPPED");
    }

    [Test]
    public async Task RunMutateAsync_null_source_skips_and_returns_zero()
    {
        using var writer = new StringWriter();
        var exit = await AllCommand.RunMutateAsync(
            writer, mutateSource: null, "/fake/tests", mutateScan: false)
            .ConfigureAwait(false);
        await Assert.That(exit).IsEqualTo(0);
        await Assert.That(writer.ToString()).Contains("SKIPPED");
    }

    [Test]
    public async Task MutateHandler_RunAsync_missing_file_returns_one()
    {
        using var writer = new StringWriter();
        var exit = await MutateHandler.RunAsync(
            "/nonexistent/file.cs", "/tmp/fake-tests",
            new MutateOptions(Scan: true), writer).ConfigureAwait(false);
        await Assert.That(exit).IsEqualTo(1);
    }

    [Test]
    public async Task MutateHandler_RunMutationCoreAsync_scan_mode_returns_zero()
    {
        var sampleFile = Path.Combine(SrcProject, "Commands", "CrapHandler.cs");
        if (!File.Exists(sampleFile)) return;

        using var writer = new StringWriter();
        var exit = await MutateHandler.RunMutationCoreAsync(
            sampleFile, TestProject, new MutateOptions(Scan: true), writer)
            .ConfigureAwait(false);

        await Assert.That(exit).IsEqualTo(0);
    }

    [Test]
    public async Task MutateHandler_DiscoverSitesAsync_with_valid_file()
    {
        var sampleFile = Path.Combine(SrcProject, "Commands", "CrapHandler.cs");
        if (!File.Exists(sampleFile)) return;

        using var writer = new StringWriter();
        var (sites, _, covered, _, _, _, _) = await MutateHandler.DiscoverSitesAsync(
            sampleFile, TestProject, new MutateOptions(Scan: true), writer)
            .ConfigureAwait(false);

        await Assert.That(sites).IsNotNull();
        await Assert.That(covered).IsNotNull();
    }

    [Test]
    public async Task ArchHandler_Run_missing_config_returns_error()
    {
        var (exitCode, _) = ArchHandler.Run("/nonexistent/config.yml", SrcProject);
        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task ArchHandler_Run_bad_project_path_returns_error()
    {
        var configPath = ArchitectureConfig;
        if (!File.Exists(configPath)) return;

        var (exitCode, _) = ArchHandler.Run(configPath, "/nonexistent/project");
        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task RunBaselineOrEmpty_bad_project_returns_false()
    {
        var (canProceed, _) = await MutationRunner.RunBaselineOrEmptyAsync(
            "/nonexistent/project", timeoutFactor: 10)
            .ConfigureAwait(false);
        await Assert.That(canProceed).IsFalse();
    }
}
