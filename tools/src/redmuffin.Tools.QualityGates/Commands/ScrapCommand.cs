namespace redmuffin.Tools.QualityGates.Commands;

using System.CommandLine;
using redmuffin.Tools.QualityGates.Analysis;

public static class ScrapCommand
{
    private static readonly Option<DirectoryInfo> TestProjectOption = new("--test-project")
    {
        Description = "Path to the test project directory to analyze",
        Required = true,
    };

    private static readonly Option<bool> VerboseOption = new("--verbose")
    {
        Description = "Show per-example metrics in output",
    };

    private static readonly Option<bool> JsonOption = new("--json")
    {
        Description = "Output results as JSON",
    };

    private static readonly Option<bool> ChangedOption = new("--changed")
    {
        Description = "Only analyze test files modified since HEAD (requires git)",
    };

    private static readonly Option<bool> WriteBaselineOption = new("--write-baseline")
    {
        Description = "Write baseline to target/scrap/ for future comparison",
    };

    private static readonly Option<string?> ComparePathOption = new("--compare")
    {
        Description = "Path to baseline JSON file for comparison",
    };

    public static Command Create()
    {
        var command = new Command("scrap", "Analyze test structural quality")
        {
            TestProjectOption, VerboseOption, JsonOption,
            ChangedOption, WriteBaselineOption, ComparePathOption,
        };

        command.SetAction(parseResult =>
        {
            var projectPath = parseResult.GetValue(TestProjectOption)!.FullName;
            var verbose = parseResult.GetValue(VerboseOption);
            var json = parseResult.GetValue(JsonOption);
            var changedOnly = parseResult.GetValue(ChangedOption);
            var writeBaseline = parseResult.GetValue(WriteBaselineOption);
            var comparePath = parseResult.GetValue(ComparePathOption);

            return Execute(projectPath, verbose, json, changedOnly, writeBaseline, comparePath);
        });

        return command;
    }

    public static int Execute(
        string projectPath, bool verbose, bool json,
        bool changedOnly, bool writeBaseline, string? comparePath)
    {
        if (!Directory.Exists(projectPath))
        {
            Console.Error.WriteLine($"Test project directory not found: {projectPath}");
            return 1;
        }

        if (comparePath is not null && !File.Exists(comparePath))
        {
            Console.Error.WriteLine($"Baseline file not found: {comparePath}. Run with --write-baseline first.");
            return 1;
        }

        var testMethods = DiscoverTestMethods(projectPath, changedOnly);
        if (testMethods.Count == 0)
        {
            Console.Out.WriteLine("No test methods found in the project.");
            return 0;
        }

        return RunScrapAnalysis(testMethods, verbose, json, writeBaseline, comparePath);
    }

    public static IReadOnlyList<TestMethod> DiscoverTestMethods(string projectPath, bool changedOnly)
    {
        var testMethods = TestMethodParser.FindTests(projectPath);
        if (changedOnly)
        {
            testMethods = GitFileFilter.FilterChanged(testMethods, projectPath, m => m.FilePath);
        }

        return testMethods;
    }

    public static int RunScrapAnalysis(
        IReadOnlyList<TestMethod> testMethods, bool verbose, bool json,
        bool writeBaseline, string? comparePath)
    {
        try
        {
            var reports = AnalyzeTestFiles(testMethods);
            var options = new ScrapOptions(
                Verbose: verbose, Json: json,
                WriteBaseline: writeBaseline, ComparePath: comparePath);
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
