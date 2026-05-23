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
        var primaryTestProject = testProjectPaths.Count > 0 ? testProjectPaths[0] : projectPath;

        var gates = new GateDescriptor[]
        {
            new(
                "Architecture (Dependency Checker)",
                () => Task.FromResult(ArchCommand.Execute(projectPath, archConfig!, json: false)),
                archConfig is null),

            new(
                "Depth (Structural Quality)",
                () => Task.FromResult(DepthCommand.Execute(projectPath)),
                !runDepth),

            new(
                "CRAP (Complexity Risk Analysis)",
                () => Task.FromResult(CrapCommand.Execute(projectPath, coveragePath, 8, changedOnly, autoCoverage, testProjectPaths)),
                false),

            new(
                "SCRAP (Structural Analyzer)",
                () => Task.FromResult(ScrapCommand.Execute(
                    primaryTestProject, verbose, json: false, changedOnly, writeBaseline: false, comparePath: null)),
                false),

            new(
                "Mutation Testing",
                async () => await MutateHandler.RunAsync(
                    mutateSource!, primaryTestProject,
                    new MutateOptions(Scan: mutateScan, AutoCoverage: true)).ConfigureAwait(false),
                mutateSource is null),

            new(
                "Duplicates (Duplicate Code Detection)",
                async () =>
                {
                    var dupesOptions = new DupesOptions(Paths: [projectPath]);
                    int dupesExit;
                    IReadOnlyList<DupesCandidate> dupeCandidates;
                    try
                    {
                        dupeCandidates = DupesDetector.FindDuplicates(dupesOptions);
                        dupesExit = dupeCandidates.Count > 0 ? 2 : 0;
                    }
                    catch (Exception ex)
                    {
                        await Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
                        return 1;
                    }

                    await o.WriteLineAsync(DupesOutputFormatter.Format(dupeCandidates, "text")).ConfigureAwait(false);
                    return dupesExit;
                },
                !runDupes),
        };

        var results = await RunGatesAsync(o, gates).ConfigureAwait(false);
        return results.Where(static r => !r.Skipped).Select(static r => r.ExitCode).DefaultIfEmpty(0).Max();
    }

    public static async Task<IReadOnlyList<GateResult>> RunGatesAsync(TextWriter output, IReadOnlyList<GateDescriptor> gates)
    {
        var results = new List<GateResult>(gates.Count);
        foreach (var gate in gates)
        {
            await output.WriteLineAsync().ConfigureAwait(false);
            if (gate.Skip)
            {
                await output.WriteLineAsync($"=== {gate.Name}: SKIPPED ===").ConfigureAwait(false);
                results.Add(new GateResult(gate.Name, 0, Skipped: true));
            }
            else
            {
                await output.WriteLineAsync($"=== {gate.Name} ===").ConfigureAwait(false);
                var exitCode = await gate.Execute().ConfigureAwait(false);
                results.Add(new GateResult(gate.Name, exitCode, Skipped: false));
            }
        }

        var overallExit = results.Where(static r => !r.Skipped).Select(static r => r.ExitCode).DefaultIfEmpty(0).Max();
        await WriteSummaryAsync(output, results, overallExit).ConfigureAwait(false);
        return results;
    }

    private static async Task WriteSummaryAsync(TextWriter o, IReadOnlyList<GateResult> results, int overallExit)
    {
        await o.WriteLineAsync().ConfigureAwait(false);
        var overall = overallExit == 0 ? "PASS" : "FAIL";
        var parts = results.Select(r => $"{r.Name}: {(r.Skipped ? "N/A" : StatusText(r.ExitCode))}");
        await o.WriteLineAsync($"{string.Join(" | ", parts)} | Overall: {overall}").ConfigureAwait(false);
    }

    private static string StatusText(int exitCode) =>
        exitCode == 0 ? "PASS" : exitCode == 1 ? "ERROR" : "FAIL";

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

        var projectConfig = Path.Combine(
            projectPath,
            "quality-gates", "architecture-rules.yml");
        if (File.Exists(projectConfig)) return projectConfig;

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
}
