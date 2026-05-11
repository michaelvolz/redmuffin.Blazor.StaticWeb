namespace redmuffin.Tools.QualityGates.Commands;

using System.CommandLine;
using System.Diagnostics;
using System.Globalization;

public static class CrapCommand
{
    private static readonly Option<DirectoryInfo> ProjectOption = new("--project")
    {
        Description = "Path to the project directory to analyze",
        Required = true,
    };

    private static readonly Option<FileInfo?> CoverageOption = new("--coverage-file")
    {
        Description = "Path to the Cobertura XML coverage file. Optional when --auto-coverage is used.",
    };

    private static readonly Option<int> MaxCrapOption = new("--max-crap")
    {
        Description = "Maximum CRAP score allowed per method",
        DefaultValueFactory = _ => 8,
    };

    private static readonly Option<bool> ChangedOption = new("--changed")
    {
        Description = "Only analyze files modified since HEAD (requires git)",
    };

    private static readonly Option<bool> AutoCoverageOption = new("--auto-coverage")
    {
        Description = "Auto-generate coverage from --test-project before analysis",
    };

    private static readonly Option<DirectoryInfo?> TestProjectOption = new("--test-project")
    {
        Description = "Path to the test project (required with --auto-coverage)",
    };

    private static readonly Func<ParseResult, int> CrapAction = parseResult =>
    {
        var projectPath = parseResult.GetValue(ProjectOption)!.FullName;
        var coverageFile = parseResult.GetValue(CoverageOption);
        var maxCrap = parseResult.GetValue(MaxCrapOption);
        var changedOnly = parseResult.GetValue(ChangedOption);
        var autoCoverage = parseResult.GetValue(AutoCoverageOption);
        var testProject = parseResult.GetValue(TestProjectOption);

        return Execute(
            projectPath,
            coverageFile?.FullName,
            maxCrap,
            changedOnly,
            autoCoverage,
            testProject?.FullName);
    };

    public static Command Create()
    {
        var command = new Command("crap", "Analyze CRAP score per method")
        {
            ProjectOption, CoverageOption, MaxCrapOption, ChangedOption,
            AutoCoverageOption, TestProjectOption,
        };

        command.SetAction(CrapAction);
        return command;
    }

    public static int Execute(
        string projectPath,
        string? coveragePath,
        int maxCrap,
        bool changedOnly,
        bool autoCoverage = false,
        string? testProjectPath = null)
    {
        var resolvedPath = ResolveCoverage(coveragePath, testProjectPath, autoCoverage);
        if (resolvedPath is null) return 1;

        var error = ValidatePaths(projectPath, resolvedPath);
        return error ?? RunAnalysis(projectPath, resolvedPath, maxCrap, changedOnly);
    }

    public static int? ValidatePaths(string projectPath, string coveragePath)
    {
        if (!Directory.Exists(projectPath))
        {
            Console.Error.WriteLine($"Project directory not found: {projectPath}");
            return 1;
        }

        if (!File.Exists(coveragePath))
        {
            Console.Error.WriteLine($"Coverage file not found: {coveragePath}");
            return 1;
        }

        return null;
    }

    public static string? ResolveCoverage(string? coveragePath, string? testProjectPath, bool autoCoverage)
    {
        if (autoCoverage && string.IsNullOrEmpty(coveragePath))
        {
            return ResolveAutoCoverage(testProjectPath!);
        }

        if (string.IsNullOrEmpty(coveragePath))
        {
            return MissingCoverageError();
        }

        return coveragePath;
    }

    public static string? MissingCoverageError()
    {
        Console.Error.WriteLine(
            "Coverage file is required. Provide --coverage-file or use --auto-coverage.");
        return null;
    }

    public static int RunAnalysis(string projectPath, string coveragePath, int maxCrap, bool changedOnly)
    {
        try
        {
            var methods = Analysis.CyclomaticComplexity.Analyze(projectPath);

            if (changedOnly)
            {
                methods = GitFileFilter.FilterChanged(methods, projectPath, m => m.FilePath);
            }

            var coverage = Analysis.CoverageParser.Parse(coveragePath);
            var results = Analysis.MethodMapper.Map(methods, coverage);
            results = Analysis.CoverageGapDetector.ClassifyCoverageGaps(results, projectPath);

            return CrapHandler.Run(results, maxCrap);
        }
        catch (System.Xml.XmlException)
        {
            Console.Error.WriteLine($"Failed to parse coverage file: {coveragePath}");
            return 1;
        }
    }

    private static string? ResolveAutoCoverage(string testProjectPath)
    {
        if (string.IsNullOrEmpty(testProjectPath))
        {
            Console.Error.WriteLine("--test-project is required with --auto-coverage");
            return null;
        }

        return GenerateCoverage(testProjectPath);
    }

    private static string? GenerateCoverage(string testProjectPath)
    {
        var newPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".cobertura.xml");

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{testProjectPath}\" --coverage --coverage-output-format cobertura --coverage-output \"{newPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0 || !File.Exists(newPath))
        {
            ReportCoverageError(process);
            return null;
        }

        return newPath;
    }

    private static void ReportCoverageError(Process process)
    {
        var exitString = process.ExitCode.ToString(CultureInfo.InvariantCulture);
        Console.Error.WriteLine($"Failed to generate coverage. dotnet run exit code: {exitString}");
        var stderr = process.StandardError.ReadToEnd();
        if (!string.IsNullOrWhiteSpace(stderr))
        {
            Console.Error.WriteLine(stderr);
        }
    }
}
