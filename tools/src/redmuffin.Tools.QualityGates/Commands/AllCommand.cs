namespace redmuffin.Tools.QualityGates.Commands;

using System.CommandLine;
using redmuffin.Tools.QualityGates.Analysis;

public static class AllCommand
{
    private static readonly Option<DirectoryInfo> ProjectOption = new("--project")
    {
        Description = "Path to the source project directory for CRAP analysis",
        Required = true,
    };

    private static readonly Option<DirectoryInfo> TestProjectOption = new("--test-project")
    {
        Description = "Path to the test project directory for SCRAP analysis",
        Required = true,
    };

    private static readonly Option<FileInfo> CoverageOption = new("--coverage-file")
    {
        Description = "Path to the Cobertura XML coverage file for CRAP",
        Required = true,
    };

    private static readonly Option<string> ArchConfigOption = new("--arch-config")
    {
        Description = "Path to the YAML architecture config file",
    };

    private static readonly Option<bool> ChangedOption = new("--changed")
    {
        Description = "Only analyze files modified since HEAD (requires git)",
    };

    private static readonly Option<bool> VerboseOption = new("--verbose")
    {
        Description = "Show detailed per-gate output",
    };

    private static readonly Option<string?> MutateSourceOption = new("--mutate-source")
    {
        Description = "Path to source file for mutation testing",
    };

    private static readonly Option<bool> MutateScanOption = new("--mutate-scan")
    {
        Description = "Run mutation in scan-only mode (no test execution)",
    };

    private static readonly Option<bool> DupesOption = new("--dupes")
    {
        Description = "Run the duplicate code detection gate",
    };

    public static Command Create()
    {
        var command = new Command("all", "Run all quality gates")
        {
            ProjectOption, TestProjectOption, CoverageOption, ArchConfigOption,
            ChangedOption, VerboseOption, MutateSourceOption, MutateScanOption, DupesOption,
        };

        command.SetAction(async parseResult =>
        {
            var projectPath = parseResult.GetValue(ProjectOption)!.FullName;
            var testProjectPath = parseResult.GetValue(TestProjectOption)!.FullName;
            var coveragePath = parseResult.GetValue(CoverageOption)!.FullName;
            var archConfig = parseResult.GetValue(ArchConfigOption);
            var changedOnly = parseResult.GetValue(ChangedOption);
            var verbose = parseResult.GetValue(VerboseOption);
            var mutateSource = parseResult.GetValue(MutateSourceOption);
            var mutateScan = parseResult.GetValue(MutateScanOption);
            var runDupes = parseResult.GetValue(DupesOption);

            return await ExecuteAsync(
                projectPath, testProjectPath, coveragePath,
                archConfig, changedOnly, verbose, mutateSource, mutateScan, runDupes).ConfigureAwait(false);
        });

        return command;
    }

    internal static async Task<int> ExecuteAsync(
        string projectPath,
        string testProjectPath,
        string coveragePath,
        string? archConfig,
        bool changedOnly,
        bool verbose,
        string? mutateSource,
        bool mutateScan,
        bool runDupes = false)
    {
        var o = Console.Out;

        await o.WriteLineAsync("=== CRAP (Complexity Risk Analysis) ===").ConfigureAwait(false);
        var crapExit = CrapCommand.Execute(projectPath, coveragePath, maxCrap: 8, changedOnly);

        await o.WriteLineAsync().ConfigureAwait(false);
        await o.WriteLineAsync("=== SCRAP (Structural Analyzer) ===").ConfigureAwait(false);
        var scrapExit = ScrapCommand.Execute(
            testProjectPath, verbose: verbose, json: false,
            changedOnly: changedOnly, writeBaseline: false, comparePath: null);

        var archExit = await WriteArchHeaderAsync(o, archConfig).ConfigureAwait(false)
            ? ArchCommand.Execute(projectPath, archConfig!, json: false)
            : 0;

        var mutateExit = await WriteMutateHeaderAsync(o, mutateSource).ConfigureAwait(false)
            ? await MutateHandler.RunAsync(
                mutateSource!, testProjectPath, new MutateOptions(Scan: mutateScan)).ConfigureAwait(false)
            : 0;

        var dupesExit = 0;
        if (runDupes)
        {
            await o.WriteLineAsync().ConfigureAwait(false);
            await o.WriteLineAsync("=== Dupes (Duplicate Code Detection) ===").ConfigureAwait(false);
            var dupesOptions = new DupesOptions(Paths: [projectPath]);
            var (exitCode, candidates) = DupesHandler.Run(dupesOptions);
            await o.WriteLineAsync(DupesOutputFormatter.Format(candidates, "text")).ConfigureAwait(false);
            dupesExit = exitCode;
        }

        var overallExit = CombineExitCodes(crapExit, scrapExit, archExit, mutateExit, dupesExit);
        await WriteSummaryAsync(o, overallExit, crapExit, scrapExit,
            archConfig, archExit, mutateSource, mutateExit, runDupes, dupesExit).ConfigureAwait(false);
        return overallExit;
    }

    private static async Task<bool> WriteArchHeaderAsync(TextWriter o, string? archConfig)
    {
        await o.WriteLineAsync().ConfigureAwait(false);
        if (archConfig is not null)
        {
            await o.WriteLineAsync("=== Architecture (Dependency Checker) ===").ConfigureAwait(false);
            return true;
        }

        await o.WriteLineAsync("=== Architecture: SKIPPED (no --arch-config) ===").ConfigureAwait(false);
        return false;
    }

    private static async Task<bool> WriteMutateHeaderAsync(TextWriter o, string? mutateSource)
    {
        await o.WriteLineAsync().ConfigureAwait(false);
        if (mutateSource is not null)
        {
            await o.WriteLineAsync("=== Mutation Testing ===").ConfigureAwait(false);
            return true;
        }

        await o.WriteLineAsync("=== Mutation: SKIPPED (no --mutate-source) ===").ConfigureAwait(false);
        return false;
    }

    private static async Task WriteSummaryAsync(
        TextWriter o, int overallExit, int crapExit, int scrapExit,
        string? archConfig, int archExit, string? mutateSource, int mutateExit,
        bool runDupes, int dupesExit)
    {
        var overallStatus = overallExit == 0 ? "PASS" : "FAIL";
        await o.WriteLineAsync().ConfigureAwait(false);
        var crapStatus = StatusText(crapExit);
        var scrapStatus = StatusText(scrapExit);
        var archStatus = archConfig is null ? "N/A" : StatusText(archExit);
        var mutateStatus = mutateSource is null ? "N/A" : StatusText(mutateExit);
        var dupesStatus = runDupes ? StatusText(dupesExit) : "N/A";
        await o.WriteLineAsync(
            $"CRAP: {crapStatus} | SCRAP: {scrapStatus} | ARCH: {archStatus} | MUTATE: {mutateStatus} | DUPES: {dupesStatus} | Overall: {overallStatus}")
            .ConfigureAwait(false);
    }

    private static string StatusText(int exitCode) =>
        exitCode == 0 ? "PASS" : (exitCode == 1 ? "ERROR" : "FAIL");

    public static int CombineExitCodes(int crapExit, int scrapExit, int archExit, int mutateExit = 0, int dupesExit = 0)
    {
        if (crapExit == 2 || scrapExit == 2 || archExit == 2 || mutateExit == 2 || dupesExit == 2)
        {
            return 2;
        }

        if (crapExit == 1 || scrapExit == 1 || archExit == 1 || mutateExit == 1 || dupesExit == 1)
        {
            return 1;
        }

        return 0;
    }
}
