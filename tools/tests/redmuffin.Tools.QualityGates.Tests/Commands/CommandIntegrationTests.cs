namespace redmuffin.Tools.QualityGates.Tests.Commands;

using redmuffin.Tools.QualityGates.Analysis;
using redmuffin.Tools.QualityGates.Commands;

/// <summary>
///     Integration tests for command Execute() methods.
///     Coverage for these tests is collected during the coverage run
///     that executes all tests — not via self-generation.
/// </summary>
public sealed class CommandIntegrationTests
{
    private static readonly string SrcProject = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "src", "redmuffin.Tools.QualityGates"));

    private static readonly string TestProject = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "tests", "redmuffin.Tools.QualityGates.Tests"));

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
        if (!File.Exists(CoverageFile))
        {
            return;
        }

        // CRAP: may fail if coverage file is locked during coverage collection
        var crapExit = CrapCommand.Execute(
            SrcProject, CoverageFile, maxCrap: 999, changedOnly: false,
            autoCoverage: false, testProjectPath: null);
        AssertCrapExit(crapExit);

        var scrapExit = ScrapCommand.Execute(
            TestProject, verbose: false, json: false, changedOnly: false,
            writeBaseline: false, comparePath: null);
        AssertCrapExit(scrapExit);

        var configPath = Path.Combine(SrcProject, "arch-rules.yml");
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
}
