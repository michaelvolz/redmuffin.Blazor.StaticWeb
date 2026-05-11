namespace redmuffin.Tools.QualityGates.Commands;

using System.CommandLine;
using redmuffin.Tools.QualityGates.Analysis;

public static class AllCommand
{
    private const string DefaultProject = "src/redmuffin.Tools.QualityGates";
    private const string DefaultTestProject = "tests/redmuffin.Tools.QualityGates.Tests";
    private const string DefaultCoverageFile = "/tmp/quality-gates-coverage.xml";

    private static readonly Option<DirectoryInfo?> ProjectOption = new("--project")
    {
        Description = $"Path to the source project directory for CRAP analysis. Defaults to '{DefaultProject}'.",
    };

    private static readonly Option<DirectoryInfo?> TestProjectOption = new("--test-project")
    {
        Description = $"Path to the test project directory (used by SCRAP, coverage, and mutation). Defaults to '{DefaultTestProject}'.",
    };

    private static readonly Option<FileInfo?> CoverageOption = new("--coverage-file")
    {
        Description = $"Path to the Cobertura XML coverage file for CRAP. Defaults to '{DefaultCoverageFile}'.",
    };

    private static readonly Option<string?> ArchConfigOption = new("--arch-config")
    {
        Description = "Path to the YAML architecture config file. Defaults to '<project>/arch-rules.yml'.",
    };

    private static readonly Option<bool> ChangedOption = new("--changed")
    {
        Description = "Only analyze files modified since HEAD (requires git).",
    };

    private static readonly Option<bool> VerboseOption = new("--verbose")
    {
        Description = "Show detailed per-gate output.",
    };

    private static readonly Option<string?> MutateSourceOption = new("--mutate-source")
    {
        Description = "Path to source file for mutation testing (requires explicit path).",
    };

    private static readonly Option<bool> MutateScanOption = new("--mutate-scan")
    {
        Description = "Run mutation in scan-only mode (no test execution).",
    };

    private static readonly Option<bool?> DupesOption = new("--dupes")
    {
        Description = "Run the duplicate code detection gate. Enabled by default. Use --no-dupes to disable.",
    };

    private static readonly Option<bool?> AutoCoverageOption = new("--auto-coverage")
    {
        Description = "Auto-generate coverage before CRAP analysis. Enabled by default. Use --no-auto-coverage to disable.",
    };

    private static readonly Func<ParseResult, Task<int>> AllAction = async parseResult =>
    {
        var project = parseResult.GetValue(ProjectOption);
        var testProject = parseResult.GetValue(TestProjectOption);
        var coverageFile = parseResult.GetValue(CoverageOption);
        var archConfig = parseResult.GetValue(ArchConfigOption);
        var changedOnly = parseResult.GetValue(ChangedOption);
        var verbose = parseResult.GetValue(VerboseOption);
        var mutateSource = parseResult.GetValue(MutateSourceOption);
        var mutateScan = parseResult.GetValue(MutateScanOption);
        var runDupes = parseResult.GetValue(DupesOption) ?? true;
        var autoCoverage = parseResult.GetValue(AutoCoverageOption) ?? true;

        // Resolve defaults
        var projectPath = ResolveProjectPath(project);
        var testProjectPath = ResolveTestProjectPath(testProject);
        var coveragePath = coverageFile?.FullName ?? DefaultCoverageFile;
        var resolvedArchConfig = archConfig ?? Path.Combine(projectPath, "arch-rules.yml");

        return await ExecuteAsync(
            projectPath, testProjectPath, coveragePath,
            resolvedArchConfig, changedOnly, verbose, mutateSource, mutateScan, runDupes,
            autoCoverage).ConfigureAwait(false);
    };

    public static Command Create()
    {
        var command = new Command("all", "Run all quality gates with smart defaults. Use --help to see all options.")
        {
            ProjectOption, TestProjectOption, CoverageOption, ArchConfigOption,
            ChangedOption, VerboseOption, MutateSourceOption, MutateScanOption, DupesOption,
            AutoCoverageOption,
        };

        command.SetAction(AllAction);
        return command;
    }

    internal static async Task<int> ExecuteAsync(
        string projectPath, string testProjectPath, string? coveragePath,
        string? archConfig, bool changedOnly, bool verbose,
        string? mutateSource, bool mutateScan, bool runDupes,
        bool autoCoverage)
    {
        var o = Console.Out;

        var crapExit = await RunCrapAsync(o, projectPath, coveragePath, changedOnly,
            autoCoverage, testProjectPath).ConfigureAwait(false);
        var scrapExit = await RunScrapAsync(o, testProjectPath, verbose, changedOnly).ConfigureAwait(false);
        var archExit = await RunArchAsync(o, projectPath, archConfig).ConfigureAwait(false);
        var mutateExit = await RunMutateAsync(o, mutateSource, testProjectPath, mutateScan).ConfigureAwait(false);
        var dupesExit = await RunDupesAsync(o, projectPath, runDupes).ConfigureAwait(false);

        var overallExit = CombineExitCodes(crapExit, scrapExit, archExit, mutateExit, dupesExit);
        await WriteSummaryAsync(o, overallExit, crapExit, scrapExit,
            archConfig, archExit, mutateSource, mutateExit, runDupes, dupesExit).ConfigureAwait(false);
        return overallExit;
    }

    private static string ResolveProjectPath(DirectoryInfo? project)
    {
        if (project is not null)
        {
            if (!project.Exists) throw new DirectoryNotFoundException($"Project path not found: {project.FullName}");
            return project.FullName;
        }

        var defaultPath = Path.GetFullPath(DefaultProject);
        if (!Directory.Exists(defaultPath))
            throw new DirectoryNotFoundException(
                $"Default project path not found: '{defaultPath}'. Specify --project to override.");
        return defaultPath;
    }

    private static string ResolveTestProjectPath(DirectoryInfo? testProject)
    {
        if (testProject is not null)
        {
            if (!testProject.Exists) throw new DirectoryNotFoundException($"Test project path not found: {testProject.FullName}");
            return testProject.FullName;
        }

        var defaultPath = Path.GetFullPath(DefaultTestProject);
        if (!Directory.Exists(defaultPath))
            throw new DirectoryNotFoundException(
                $"Default test project path not found: '{defaultPath}'. Specify --test-project to override.");
        return defaultPath;
    }

    private static async Task<int> RunCrapAsync(TextWriter o, string projectPath, string? coveragePath,
        bool changedOnly, bool autoCoverage, string? testProjectPath)
    {
        await o.WriteLineAsync("=== CRAP (Complexity Risk Analysis) ===").ConfigureAwait(false);
        return CrapCommand.Execute(projectPath, coveragePath, 8, changedOnly, autoCoverage, testProjectPath);
    }

    private static async Task<int> RunScrapAsync(TextWriter o, string testProjectPath, bool verbose, bool changedOnly)
    {
        await o.WriteLineAsync().ConfigureAwait(false);
        await o.WriteLineAsync("=== SCRAP (Structural Analyzer) ===").ConfigureAwait(false);
        return ScrapCommand.Execute(
            testProjectPath, verbose, json: false, changedOnly, writeBaseline: false, comparePath: null);
    }

    public static async Task<int> RunArchAsync(TextWriter o, string projectPath, string? archConfig)
    {
        return await WriteGateHeaderAsync(o, archConfig,
                "Architecture (Dependency Checker)", "--arch-config").ConfigureAwait(false)
            ? ArchCommand.Execute(projectPath, archConfig!, json: false)
            : 0;
    }

    public static async Task<int> RunMutateAsync(TextWriter o, string? mutateSource,
        string testProjectPath, bool mutateScan)
    {
        return await WriteGateHeaderAsync(o, mutateSource,
                "Mutation Testing", "--mutate-source").ConfigureAwait(false)
            ? await MutateHandler.RunAsync(
                mutateSource!, testProjectPath, new MutateOptions(Scan: mutateScan)).ConfigureAwait(false)
            : 0;
    }

    private static async Task<int> RunDupesAsync(TextWriter o, string projectPath, bool runDupes)
    {
        if (!runDupes) return 0;

        await o.WriteLineAsync().ConfigureAwait(false);
        await o.WriteLineAsync("=== Dupes (Duplicate Code Detection) ===").ConfigureAwait(false);
        var dupesOptions = new DupesOptions(Paths: [projectPath]);
        var (exitCode, candidates) = DupesHandler.Run(dupesOptions);
        await o.WriteLineAsync(DupesOutputFormatter.Format(candidates, "text")).ConfigureAwait(false);
        return exitCode;
    }

    public static async Task<bool> WriteGateHeaderAsync(TextWriter o, string? config, string gateName, string missingFlag)
    {
        await o.WriteLineAsync().ConfigureAwait(false);
        if (config is not null)
        {
            await o.WriteLineAsync($"=== {gateName} ===").ConfigureAwait(false);
            return true;
        }

        await o.WriteLineAsync($"=== {gateName}: SKIPPED (no {missingFlag}) ===").ConfigureAwait(false);
        return false;
    }

    private static async Task WriteSummaryAsync(
        TextWriter o, int overallExit, int crapExit, int scrapExit,
        string? archConfig, int archExit, string? mutateSource, int mutateExit,
        bool runDupes, int dupesExit)
    {
        await o.WriteLineAsync().ConfigureAwait(false);
        var line = BuildSummaryLine(overallExit, crapExit, scrapExit,
            archConfig, archExit, mutateSource, mutateExit, runDupes, dupesExit);
        await o.WriteLineAsync(line).ConfigureAwait(false);
    }

    public static string BuildSummaryLine(int overallExit, int crapExit, int scrapExit,
        string? archConfig, int archExit, string? mutateSource, int mutateExit,
        bool runDupes, int dupesExit)
    {
        var overallStatus = overallExit == 0 ? "PASS" : "FAIL";
        var crapStatus = StatusText(crapExit);
        var scrapStatus = StatusText(scrapExit);
        var archStatus = GateStatus(archConfig, archExit);
        var mutateStatus = GateStatus(mutateSource, mutateExit);
        var dupesStatus = runDupes ? StatusText(dupesExit) : "N/A";
        return $"CRAP: {crapStatus} | SCRAP: {scrapStatus} | ARCH: {archStatus} | MUTATE: {mutateStatus} | DUPES: {dupesStatus} | Overall: {overallStatus}";
    }

    private static string GateStatus(string? config, int exitCode) =>
        config is null ? "N/A" : StatusText(exitCode);

    private static string StatusText(int exitCode) =>
        exitCode == 0 ? "PASS" : (exitCode == 1 ? "ERROR" : "FAIL");

    public static int CombineExitCodes(int crapExit, int scrapExit, int archExit, int mutateExit = 0, int dupesExit = 0) =>
        Math.Max(crapExit, Math.Max(scrapExit, Math.Max(archExit, Math.Max(mutateExit, dupesExit))));
}
