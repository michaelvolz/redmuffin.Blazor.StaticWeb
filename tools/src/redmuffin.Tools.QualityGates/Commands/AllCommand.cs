namespace redmuffin.Tools.QualityGates.Commands;

using System.CommandLine;

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
        };

        command.SetAction(parseResult =>
        {
            var projectPath = parseResult.GetValue(projectOption)!.FullName;
            var testProjectPath = parseResult.GetValue(testProjectOption)!.FullName;
            var coveragePath = parseResult.GetValue(coverageOption)!.FullName;
            var archConfig = parseResult.GetValue(archConfigOption);
            var changedOnly = parseResult.GetValue(changedOption);
            var verbose = parseResult.GetValue(verboseOption);
            var mutateSource = parseResult.GetValue(mutateSourceOption);
            var mutateScan = parseResult.GetValue(mutateScanOption);

            return Execute(
                projectPath, testProjectPath, coveragePath,
                archConfig, changedOnly, verbose, mutateSource, mutateScan);
        });

        return command;
    }

    internal static int Execute(
        string projectPath,
        string testProjectPath,
        string coveragePath,
        string? archConfig,
        bool changedOnly,
        bool verbose,
        string? mutateSource,
        bool mutateScan)
    {
        Console.Out.WriteLine("=== CRAP (Complexity Risk Analysis) ===");
        var crapExit = CrapCommand.Execute(projectPath, coveragePath, maxCrap: 8, changedOnly);

        Console.Out.WriteLine();
        Console.Out.WriteLine("=== SCRAP (Structural Analyzer) ===");
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
            Console.Out.WriteLine();
            Console.Out.WriteLine("=== Architecture (Dependency Checker) ===");
            archExit = ArchCommand.Execute(projectPath, archConfig, json: false);
        }
        else
        {
            Console.Out.WriteLine();
            Console.Out.WriteLine("=== Architecture: SKIPPED (no --arch-config) ===");
        }

        var mutateExit = 0;
        if (mutateSource is not null)
        {
            Console.Out.WriteLine();
            Console.Out.WriteLine("=== Mutation Testing ===");
            var options = new MutateOptions(
                Scan: mutateScan);
            mutateExit = MutateHandler.RunAsync(
                mutateSource, testProjectPath, options).GetAwaiter().GetResult();
        }
        else
        {
            Console.Out.WriteLine();
            Console.Out.WriteLine("=== Mutation: SKIPPED (no --mutate-source) ===");
        }

        var overallExit = CombineExitCodes(crapExit, scrapExit, archExit, mutateExit);
        var overallStatus = overallExit == 0 ? "PASS" : "FAIL";

        Console.Out.WriteLine();
        var crapStatus = crapExit == 0 ? "PASS" : (crapExit == 1 ? "ERROR" : "FAIL");
        var scrapStatus = scrapExit == 0 ? "PASS" : (scrapExit == 1 ? "ERROR" : "FAIL");
        var archStatus = archConfig is null ? "N/A"
            : (archExit == 0 ? "PASS" : (archExit == 1 ? "ERROR" : "FAIL"));
        var mutateStatus = mutateSource is null ? "N/A"
            : (mutateExit == 0 ? "PASS" : (mutateExit == 1 ? "ERROR" : "FAIL"));
        Console.Out.WriteLine(
            $"CRAP: {crapStatus} | SCRAP: {scrapStatus} | ARCH: {archStatus} | MUTATE: {mutateStatus} | Overall: {overallStatus}");

        return overallExit;
    }

    public static int CombineExitCodes(int crapExit, int scrapExit, int archExit, int mutateExit = 0)
    {
        if (crapExit == 2 || scrapExit == 2 || archExit == 2 || mutateExit == 2)
        {
            return 2;
        }

        if (crapExit == 1 || scrapExit == 1 || archExit == 1 || mutateExit == 1)
        {
            return 1;
        }

        return 0;
    }
}
