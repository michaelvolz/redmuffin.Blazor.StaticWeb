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

        var changedOption = new Option<bool>("--changed")
        {
            Description = "Only analyze files modified since HEAD (requires git)",
        };

        var verboseOption = new Option<bool>("--verbose")
        {
            Description = "Show detailed per-gate output",
        };

        var command = new Command("all", "Run all quality gates")
        {
            projectOption,
            testProjectOption,
            coverageOption,
            changedOption,
            verboseOption,
        };

        command.SetAction(parseResult =>
        {
            var projectPath = parseResult.GetValue(projectOption)!.FullName;
            var testProjectPath = parseResult.GetValue(testProjectOption)!.FullName;
            var coveragePath = parseResult.GetValue(coverageOption)!.FullName;
            var changedOnly = parseResult.GetValue(changedOption);
            var verbose = parseResult.GetValue(verboseOption);

            return Execute(projectPath, testProjectPath, coveragePath, changedOnly, verbose);
        });

        return command;
    }

    internal static int Execute(
        string projectPath,
        string testProjectPath,
        string coveragePath,
        bool changedOnly,
        bool verbose)
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

        Console.Out.WriteLine();
        Console.Out.WriteLine("=== Duplication: NOT YET IMPLEMENTED ===");
        Console.Out.WriteLine("=== Architecture: NOT YET IMPLEMENTED ===");

        var overallExit = CombineExitCodes(crapExit, scrapExit);
        var overallStatus = overallExit == 0 ? "PASS" : "FAIL";

        Console.Out.WriteLine();
        var crapStatus = crapExit == 0 ? "PASS" : (crapExit == 1 ? "ERROR" : "FAIL");
        var scrapStatus = scrapExit == 0 ? "PASS" : (scrapExit == 1 ? "ERROR" : "FAIL");
        Console.Out.WriteLine($"CRAP: {crapStatus} | SCRAP: {scrapStatus} | Overall: {overallStatus}");

        return overallExit;
    }

    /// <summary>
    /// Combines exit codes from individual gates. Returns the worst result:
    /// 2 (threshold breach) overrides 1 (error) overrides 0 (pass).
    /// </summary>
    public static int CombineExitCodes(int crapExit, int scrapExit)
    {
        if (crapExit == 2 || scrapExit == 2)
        {
            return 2;
        }

        if (crapExit == 1 || scrapExit == 1)
        {
            return 1;
        }

        return 0;
    }
}
