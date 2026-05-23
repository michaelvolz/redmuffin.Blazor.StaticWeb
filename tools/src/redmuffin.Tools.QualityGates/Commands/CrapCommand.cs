namespace redmuffin.Tools.QualityGates.Commands;

using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using redmuffin.Tools.QualityGates.Analysis;

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

        var testProjectPaths = testProject is not null
            ? new[] { testProject.FullName }
            : null;

        return Execute(
            projectPath,
            coverageFile?.FullName,
            maxCrap,
            changedOnly,
            autoCoverage,
            testProjectPaths);
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
        IReadOnlyList<string>? testProjectPaths = null)
    {
        var resolvedPath = ResolveCoverage(coveragePath, testProjectPaths, autoCoverage);
        if (resolvedPath is null) return 1;
        if (ValidatePaths(projectPath, resolvedPath)) return 1;

        return RunAnalysis(projectPath, resolvedPath, maxCrap, changedOnly);
    }

    public static bool ValidatePaths(string projectPath, string coveragePath)
    {
        if (!Directory.Exists(projectPath))
        {
            Console.Error.WriteLine($"Project directory not found: {projectPath}");
            return true;
        }

        if (!File.Exists(coveragePath))
        {
            Console.Error.WriteLine($"Coverage file not found: {coveragePath}");
            return true;
        }

        return false;
    }

    public static string? ResolveCoverage(
        string? coveragePath, IReadOnlyList<string>? testProjectPaths, bool autoCoverage)
    {
        if (autoCoverage && string.IsNullOrEmpty(coveragePath))
        {
            return ResolveAutoCoverage(testProjectPaths);
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

    public static string? ValidateTestProjectList(IReadOnlyList<string>? testProjectPaths)
    {
        if (testProjectPaths is null || testProjectPaths.Count == 0)
        {
            return "--test-project is required with --auto-coverage";
        }

        return null;
    }

    private static string? ResolveAutoCoverage(IReadOnlyList<string>? testProjectPaths)
    {
        var error = ValidateTestProjectList(testProjectPaths);
        if (error is not null)
        {
            Console.Error.WriteLine(error);
            return null;
        }

        return GenerateCoverageForAllProjects(testProjectPaths!);
    }

    public static string? GenerateCoverageForAllProjects(
        IReadOnlyList<string> testProjectPaths,
        Func<string, string?>? generateCoverage = null)
    {
        generateCoverage ??= GenerateCoverage;

        var tempFiles = new List<string>();
        try
        {
            foreach (var projectPath in testProjectPaths)
            {
                var path = generateCoverage(projectPath);
                if (path is null)
                {
                    CleanupTempFiles(tempFiles);
                    return null;
                }

                tempFiles.Add(path);
            }

            return MergeTempCoverageFiles(tempFiles);
        }
        catch
        {
            CleanupTempFiles(tempFiles);
            throw;
        }
    }

    public static string? MergeTempCoverageFiles(IReadOnlyList<string> tempFiles)
    {
        if (tempFiles.Count == 1) return tempFiles[0];

        var mergedPath = Path.Combine(
            Path.GetTempPath(),
            Path.GetRandomFileName() + ".merged.cobertura.xml");
        Analysis.CoberturaMerger.Merge(tempFiles, mergedPath);
        CleanupTempFiles(tempFiles);
        return mergedPath;
    }

    private static void CleanupTempFiles(IReadOnlyList<string> files)
    {
        foreach (var path in files)
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    private static string? GenerateCoverage(string testProjectPath)
        => CoverageRunner.Generate(testProjectPath);
}
