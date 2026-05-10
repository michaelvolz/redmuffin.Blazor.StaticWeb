namespace redmuffin.Tools.QualityGates.Commands;

using System.CommandLine;
using redmuffin.Tools.QualityGates.Analysis;

public static class AllCommand
{
    public static Command Create()
    {
        var projectOption = new Option<DirectoryInfo>("--project")
        {
            Description = "Path to the source project directory for CRAP analysis",
            Required = true,
        };

        var testProjectOption = new Option<DirectoryInfo>("--test-project")
        {
            Description = "Path to the test project directory for SCRAP analysis",
            Required = true,
        };

        var coverageOption = new Option<FileInfo>("--coverage-file")
        {
            Description = "Path to the Cobertura XML coverage file for CRAP",
            Required = true,
        };

        var archConfigOption = new Option<string>("--arch-config")
        {
            Description = "Path to the YAML architecture config file",
        };

        var changedOption = new Option<bool>("--changed")
        {
            Description = "Only analyze files modified since HEAD (requires git)",
        };

        var verboseOption = new Option<bool>("--verbose")
        {
            Description = "Show detailed per-gate output",
        };

        var mutateSourceOption = new Option<string?>("--mutate-source")
        {
            Description = "Path to source file for mutation testing",
        };

        var mutateScanOption = new Option<bool>("--mutate-scan")
        {
            Description = "Run mutation in scan-only mode (no test execution)",
        };

        var dupesOption = new Option<bool>("--dupes")
        {
            Description = "Run the duplicate code detection gate",
        };

        var command = new Command("all", "Run all quality gates")
        {
            projectOption,
            testProjectOption,
            coverageOption,
            archConfigOption,
            changedOption,
            verboseOption,
            mutateSourceOption,
            mutateScanOption,
            dupesOption,
        };

        command.SetAction(async parseResult =>
        {
            var projectPath = parseResult.GetValue(projectOption)!.FullName;
            var testProjectPath = parseResult.GetValue(testProjectOption)!.FullName;
            var coveragePath = parseResult.GetValue(coverageOption)!.FullName;
            var archConfig = parseResult.GetValue(archConfigOption);
            var changedOnly = parseResult.GetValue(changedOption);
            var verbose = parseResult.GetValue(verboseOption);
            var mutateSource = parseResult.GetValue(mutateSourceOption);
            var mutateScan = parseResult.GetValue(mutateScanOption);
            var runDupes = parseResult.GetValue(dupesOption);

            return await Execute(
                projectPath, testProjectPath, coveragePath,
                archConfig, changedOnly, verbose, mutateSource, mutateScan, runDupes).ConfigureAwait(false);
        });

        return command;
    }

    internal static async Task<int> Execute(
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
            testProjectPath,
            verbose: verbose,
            json: false,
            changedOnly: changedOnly,
            writeBaseline: false,
            comparePath: null);

        var archExit = 0;
        if (archConfig is not null)
        {
            await o.WriteLineAsync().ConfigureAwait(false);
            await o.WriteLineAsync("=== Architecture (Dependency Checker) ===").ConfigureAwait(false);
            archExit = ArchCommand.Execute(projectPath, archConfig, json: false);
        }
        else
        {
            await o.WriteLineAsync().ConfigureAwait(false);
            await o.WriteLineAsync("=== Architecture: SKIPPED (no --arch-config) ===").ConfigureAwait(false);
        }

        var mutateExit = 0;
        if (mutateSource is not null)
        {
            await o.WriteLineAsync().ConfigureAwait(false);
            await o.WriteLineAsync("=== Mutation Testing ===").ConfigureAwait(false);
            var options = new MutateOptions(Scan: mutateScan);
            mutateExit = await MutateHandler.RunAsync(
                mutateSource, testProjectPath, options).ConfigureAwait(false);
        }
        else
        {
            await o.WriteLineAsync().ConfigureAwait(false);
            await o.WriteLineAsync("=== Mutation: SKIPPED (no --mutate-source) ===").ConfigureAwait(false);
        }

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
        var overallStatus = overallExit == 0 ? "PASS" : "FAIL";

        await o.WriteLineAsync().ConfigureAwait(false);
        var crapStatus = crapExit == 0 ? "PASS" : (crapExit == 1 ? "ERROR" : "FAIL");
        var scrapStatus = scrapExit == 0 ? "PASS" : (scrapExit == 1 ? "ERROR" : "FAIL");
        var archStatus = archConfig is null ? "N/A"
            : (archExit == 0 ? "PASS" : (archExit == 1 ? "ERROR" : "FAIL"));
        var mutateStatus = mutateSource is null ? "N/A"
            : (mutateExit == 0 ? "PASS" : (mutateExit == 1 ? "ERROR" : "FAIL"));
        var dupesStatus = runDupes
            ? (dupesExit == 0 ? "PASS" : (dupesExit == 1 ? "ERROR" : "FAIL"))
            : "N/A";
        await o.WriteLineAsync(
            $"CRAP: {crapStatus} | SCRAP: {scrapStatus} | ARCH: {archStatus} | MUTATE: {mutateStatus} | DUPES: {dupesStatus} | Overall: {overallStatus}")
            .ConfigureAwait(false);

        return overallExit;
    }

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
