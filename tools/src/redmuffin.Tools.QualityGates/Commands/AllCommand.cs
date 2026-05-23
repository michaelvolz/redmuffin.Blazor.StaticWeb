namespace redmuffin.Tools.QualityGates.Commands;

using System.CommandLine;
using redmuffin.Tools.QualityGates.Analysis;

public static class AllCommand
{
    private static readonly string DefaultCoverageFile = Path.Combine(Path.GetTempPath(), "quality-gates-coverage.xml");

    private static readonly Option<DirectoryInfo?> ProjectOption = new("--project")
    {
        Description = "Path to the source project to analyze. Auto-discovered from the nearest .slnx when omitted.",
    };

    private static readonly Option<DirectoryInfo?> TestProjectOption = new("--test-project")
    {
        Description = "Path to the test project (used by SCRAP, coverage, and mutation). Auto-discovered from the nearest .slnx when omitted.",
    };

    private static readonly Option<FileInfo?> SolutionOption = new("--solution")
    {
        Description = "Path to a .slnx solution file to discover projects from. Overrides auto-discovery.",
    };

    private static readonly Option<FileInfo?> CoverageOption = new("--coverage-file")
    {
        Description = $"Path to the Cobertura XML coverage file for CRAP. Defaults to '{DefaultCoverageFile}'.",
    };

    private static readonly Option<string?> ArchConfigOption = new("--architecture-config")
    {
        Description = "Path to the YAML architecture config file. Defaults to '<project>/quality-gates/architecture-rules.yml'.",
    };

    private static readonly Option<bool> ChangedOption = new("--changed")
    {
        Description = "Only analyze files modified since HEAD (requires git).",
    };

    private static readonly Option<bool> VerboseOption = new("--verbose")
    {
        Description = "Show detailed per-gate output.",
    };

    private static readonly Option<string?> MutateSourceOption = new("--mutation-source")
    {
        Description = "Path to source file for mutation testing (requires explicit path).",
    };

    private static readonly Option<bool> MutateScanOption = new("--mutation-scan")
    {
        Description = "Run mutation in scan-only mode (no test execution).",
    };

    private static readonly Option<bool?> DupesOption = new("--duplicates")
    {
        Description = "Run the duplicate code detection gate. Enabled by default.",
    };

    private static readonly Option<bool?> DepthOption = new("--depth")
    {
        Description = "Run the structural depth analysis gate. Enabled by default.",
    };

    private static readonly Option<bool?> AutoCoverageOption = new("--auto-coverage")
    {
        Description = "Auto-generate coverage before CRAP analysis. Enabled by default.",
    };

    private static readonly Func<ParseResult, Task<int>> AllAction = async parseResult =>
    {
        var project = parseResult.GetValue(ProjectOption);
        var testProject = parseResult.GetValue(TestProjectOption);
        var solution = parseResult.GetValue(SolutionOption);
        var coverageFile = parseResult.GetValue(CoverageOption);
        var archConfig = parseResult.GetValue(ArchConfigOption);
        var changedOnly = parseResult.GetValue(ChangedOption);
        var verbose = parseResult.GetValue(VerboseOption);
        var mutateSource = parseResult.GetValue(MutateSourceOption);
        var mutateScan = parseResult.GetValue(MutateScanOption);
        var runDupes = parseResult.GetValue(DupesOption) ?? true;
        var runDepth = parseResult.GetValue(DepthOption) ?? true;
        var autoCoverage = parseResult.GetValue(AutoCoverageOption) ?? true;

        // Resolve defaults
        var projectPath = ResolveProjectPath(project, solution);
        var testProjectPaths = ResolveTestProjectPaths(testProject, solution);
        var coveragePath = coverageFile?.FullName ?? (autoCoverage ? null : DefaultCoverageFile);
        var resolvedArchConfig = ResolveArchConfig(archConfig, projectPath);

        return await ExecuteAsync(
            projectPath, testProjectPaths, coveragePath,
            resolvedArchConfig, changedOnly, verbose, mutateSource, mutateScan, runDupes,
            runDepth, autoCoverage).ConfigureAwait(false);
    };

    public static Command Create()
    {
        var command = new Command("all", "Run all quality gates with smart defaults. Use --help to see all options.")
        {
            ProjectOption, TestProjectOption, SolutionOption, CoverageOption, ArchConfigOption,
            ChangedOption, VerboseOption, MutateSourceOption, MutateScanOption, DupesOption,
            DepthOption, AutoCoverageOption,
        };

        command.SetAction(AllAction);
        return command;
    }

    internal static async Task<int> ExecuteAsync(
        string projectPath, IReadOnlyList<string> testProjectPaths, string? coveragePath,
        string? archConfig, bool changedOnly, bool verbose,
        string? mutateSource, bool mutateScan, bool runDupes,
        bool runDepth, bool autoCoverage)
    {
        var o = Console.Out;

        // Order: Architecture → Depth → CRAP → SCRAP → Mutation → Duplicates
        var archExit = await RunArchAsync(o, projectPath, archConfig).ConfigureAwait(false);
        var depthExit = runDepth ? RunDepth(o, projectPath) : 0;
        var crapExit = await RunCrapAsync(o, projectPath, coveragePath, changedOnly,
            autoCoverage, testProjectPaths).ConfigureAwait(false);
        var primaryTestProject = testProjectPaths.Count > 0 ? testProjectPaths[0] : projectPath;
        var scrapExit = await RunScrapAsync(o, primaryTestProject, verbose, changedOnly).ConfigureAwait(false);
        var mutateExit = await RunMutateAsync(o, mutateSource, primaryTestProject, mutateScan).ConfigureAwait(false);
        var dupesExit = runDupes ? await RunDupesAsync(o, projectPath).ConfigureAwait(false) : 0;

        var overallExit = CombineExitCodes(crapExit, scrapExit, archExit, mutateExit, dupesExit, depthExit);
        var results = new GateRunResults(overallExit, crapExit, scrapExit,
            archConfig, archExit, mutateSource, mutateExit, runDupes, dupesExit, runDepth, depthExit);
        await WriteSummaryAsync(o, results).ConfigureAwait(false);
        return overallExit;
    }

    private static string ResolveProjectPath(DirectoryInfo? project, FileInfo? solution)
    {
        if (project is not null)
        {
            if (!project.Exists)
                throw new DirectoryNotFoundException(
                    $"Project path not found: {project.FullName}");
            return project.FullName;
        }

        if (solution is not null)
            return ResolveSourceRootFromSolution(solution);

        return ResolveSourceRootFromDiscovery();
    }

    private static string ResolveSourceRootFromSolution(FileInfo solution)
    {
        return Path.Combine(
            Path.GetDirectoryName(solution.FullName)!, "src");
    }

    private static string ResolveSourceRootFromDiscovery()
    {
        var discovered = SlnxProjectDiscovery.Discover(null);
        if (discovered.SourceProjects.Count == 0)
            throw new InvalidOperationException(
                "No source projects found. Specify --project to override.");

        var firstProject = ProjectDir(discovered.SourceProjects[0]);
        return Path.GetDirectoryName(firstProject)!;
    }

    public static IReadOnlyList<string> ResolveTestProjectPaths(DirectoryInfo? testProject, FileInfo? solution)
    {
        if (testProject is not null)
        {
            if (!testProject.Exists)
                throw new DirectoryNotFoundException(
                    $"Test project path not found: {testProject.FullName}");
            return [testProject.FullName];
        }

        var discovered = DiscoverFromSourceOrSolution(solution);
        if (discovered.TestProjects.Count == 0)
            throw new InvalidOperationException(
                "No test projects found. Specify --test-project to override.");

        return discovered.TestProjects.Select(ProjectDir).ToArray();
    }

    public static string ProjectDir(string csprojOrDirPath) =>
        csprojOrDirPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            ? Path.GetDirectoryName(csprojOrDirPath)!
            : csprojOrDirPath;

    public static SlnxDiscoveredProjects DiscoverFromSourceOrSolution(FileInfo? solution)
    {
        if (solution is not null)
        {
            if (!solution.Exists)
                throw new FileNotFoundException(
                    $"Solution file not found: {solution.FullName}");
            return SlnxProjectDiscovery.DiscoverFromSlnx(solution.FullName);
        }

        var discovered = SlnxProjectDiscovery.Discover(null);
        if (discovered.SourceProjects.Count == 0)
            throw new InvalidOperationException(
                "No source projects found. Specify --project to override.");

        return discovered;
    }

    public static string? ResolveArchConfig(string? archConfig, string projectPath)
    {
        if (archConfig is not null) return archConfig;

        // Try the project directory first.
        var projectConfig = Path.Combine(
            projectPath,
            "quality-gates", "architecture-rules.yml");
        if (File.Exists(projectConfig)) return projectConfig;

        // Walk up from the project to find a quality-gates/ directory.
        var current = projectPath;
        while (current is not null)
        {
            var candidate = Path.Combine(
                current,
                "quality-gates", "architecture-rules.yml");
            if (File.Exists(candidate)) return candidate;
            current = Path.GetDirectoryName(current);
        }

        return null;
    }

    private static int RunDepth(TextWriter o, string projectPath)
    {
        o.WriteLine();
        o.WriteLine("=== Depth (Structural Quality) ===");
        return DepthCommand.Execute(projectPath);
    }

    private static async Task<int> RunCrapAsync(TextWriter o, string projectPath, string? coveragePath,
        bool changedOnly, bool autoCoverage, IReadOnlyList<string> testProjectPaths)
    {
        await o.WriteLineAsync("=== CRAP (Complexity Risk Analysis) ===").ConfigureAwait(false);
        return CrapCommand.Execute(projectPath, coveragePath, 8, changedOnly, autoCoverage, testProjectPaths);
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
                "Architecture (Dependency Checker)", "--architecture-config").ConfigureAwait(false)
            ? ArchCommand.Execute(projectPath, archConfig!, json: false)
            : 0;
    }

    public static async Task<int> RunMutateAsync(TextWriter o, string? mutateSource,
        string testProjectPath, bool mutateScan)
    {
        return await WriteGateHeaderAsync(o, mutateSource,
                "Mutation Testing", "--mutation-source").ConfigureAwait(false)
            ? await MutateHandler.RunAsync(
                mutateSource!, testProjectPath,
                new MutateOptions(Scan: mutateScan, AutoCoverage: true)).ConfigureAwait(false)
            : 0;
    }

    private static async Task<int> RunDupesAsync(TextWriter o, string projectPath)
    {
        await o.WriteLineAsync().ConfigureAwait(false);
        await o.WriteLineAsync("=== Duplicates (Duplicate Code Detection) ===").ConfigureAwait(false);
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

    private static async Task WriteSummaryAsync(TextWriter o, GateRunResults r)
    {
        await o.WriteLineAsync().ConfigureAwait(false);
        var line = BuildSummaryLine(r);
        await o.WriteLineAsync(line).ConfigureAwait(false);
    }

    public static string BuildSummaryLine(GateRunResults r)
    {
        var overallStatus = r.OverallExit == 0 ? "PASS" : "FAIL";
        var archStatus = GateStatus(r.ArchConfig, r.ArchExit);
        var depthStatus = r.RunDepth ? StatusText(r.DepthExit) : "N/A";
        var crapStatus = StatusText(r.CrapExit);
        var scrapStatus = StatusText(r.ScrapExit);
        var mutateStatus = GateStatus(r.MutateSource, r.MutateExit);
        var dupesStatus = r.RunDupes ? StatusText(r.DupesExit) : "N/A";
        return $"Architecture: {archStatus} | Depth: {depthStatus} | CRAP: {crapStatus} | SCRAP: {scrapStatus} | Mutation: {mutateStatus} | Duplicates: {dupesStatus} | Overall: {overallStatus}";
    }

    private static string GateStatus(string? config, int exitCode) =>
        config is null ? "N/A" : StatusText(exitCode);

    private static string StatusText(int exitCode) =>
        exitCode == 0 ? "PASS" : (exitCode == 1 ? "ERROR" : "FAIL");

    public static int CombineExitCodes(int crapExit, int scrapExit, int archExit, int mutateExit = 0, int dupesExit = 0, int depthExit = 0) =>
        Math.Max(crapExit, Math.Max(scrapExit, Math.Max(archExit, Math.Max(mutateExit, Math.Max(dupesExit, depthExit)))));
}
