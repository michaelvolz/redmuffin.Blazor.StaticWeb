namespace redmuffin.Tools.QualityGates.Commands;

using System.CommandLine;
using redmuffin.Tools.QualityGates.Analysis;

public static class ScrapCommand
{
    public static Command Create()
    {
        var testProjectOption = new Option<DirectoryInfo>("--test-project")
        {
            Description = "Path to the test project directory to analyze",
            Required = true,
        };

        var verboseOption = new Option<bool>("--verbose")
        {
            Description = "Show per-example metrics in output",
        };

        var jsonOption = new Option<bool>("--json")
        {
            Description = "Output results as JSON",
        };

        var changedOption = new Option<bool>("--changed")
        {
            Description = "Only analyze test files modified since HEAD (requires git)",
        };

        var writeBaselineOption = new Option<bool>("--write-baseline")
        {
            Description = "Write baseline to target/scrap/ for future comparison",
        };

        var compareOption = new Option<string?>("--compare")
        {
            Description = "Path to baseline JSON file for comparison",
        };

        var command = new Command("scrap", "Analyze test structural quality")
        {
            testProjectOption,
            verboseOption,
            jsonOption,
            changedOption,
            writeBaselineOption,
            compareOption,
        };

        command.SetAction(parseResult =>
        {
            var projectPath = parseResult.GetValue(testProjectOption)!.FullName;
            var verbose = parseResult.GetValue(verboseOption);
            var json = parseResult.GetValue(jsonOption);
            var changedOnly = parseResult.GetValue(changedOption);
            var writeBaseline = parseResult.GetValue(writeBaselineOption);
            var comparePath = parseResult.GetValue(compareOption);

            return Execute(projectPath, verbose, json, changedOnly, writeBaseline, comparePath);
        });

        return command;
    }

    internal static int Execute(
        string projectPath,
        bool verbose,
        bool json,
        bool changedOnly,
        bool writeBaseline,
        string? comparePath)
    {
        try
        {
            if (!Directory.Exists(projectPath))
            {
                Console.Error.WriteLine($"Test project directory not found: {projectPath}");
                return 1;
            }

            // Validate compare mode
            if (comparePath is not null && !File.Exists(comparePath))
            {
                Console.Error.WriteLine($"Baseline file not found: {comparePath}. Run with --write-baseline first.");
                return 1;
            }

            var testMethods = TestMethodParser.FindTests(projectPath);

            if (changedOnly)
            {
                testMethods = GitFileFilter.FilterChanged(testMethods, projectPath, m => m.FilePath);
            }

            if (testMethods.Count == 0)
            {
                Console.Out.WriteLine("No test methods found in the project.");
                return 0;
            }

            var reports = AnalyzeTestFiles(testMethods);
            var options = new ScrapOptions(
                Verbose: verbose,
                Json: json,
                WriteBaseline: writeBaseline,
                ComparePath: comparePath);

            return ScrapHandler.Run(reports, options);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error analyzing test project: {ex.Message}");
            return 1;
        }
    }

    private static List<FileScrapReport> AnalyzeTestFiles(IReadOnlyList<TestMethod> testMethods)
    {
        var reports = new List<FileScrapReport>();
        var groups = testMethods.GroupBy(m => m.FilePath, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var methods = group.ToList();
            var dupResults = ScrapDuplication.Analyze(methods);
            var filePressure = ExtractionPressure.ComputeFilePressure(dupResults);
            var report = ScrapScorer.ScoreFile(methods, dupResults, filePressure);
            reports.Add(report);
        }

        return reports;
    }
}
