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

    private static string? GenerateCoverageForAllProjects(IReadOnlyList<string> testProjectPaths)
    {
        var tempFiles = new List<string>();
        try
        {
            foreach (var projectPath in testProjectPaths)
            {
                var path = GenerateCoverage(projectPath);
                if (path is null)
                {
                    CleanupTempFiles(tempFiles);
                    return null;
                }

                tempFiles.Add(path);
            }

            if (tempFiles.Count == 1) return tempFiles[0];

            var mergedPath = Path.Combine(
                Path.GetTempPath(),
                Path.GetRandomFileName() + ".merged.cobertura.xml");
            Analysis.CoberturaMerger.Merge(tempFiles, mergedPath);
            CleanupTempFiles(tempFiles);
            return mergedPath;
        }
        catch
        {
            CleanupTempFiles(tempFiles);
            throw;
        }
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

    public static ProcessStartInfo BuildCoverageProcessStartInfo(
        string testProjectPath, string outputPath)
    {
        return new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{testProjectPath}\" --coverage --coverage-output-format cobertura --coverage-output \"{outputPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
    }

    public static bool IsCoverageRunSuccessful(int exitCode, string filePath)
    {
        return exitCode == 0 && File.Exists(filePath);
    }

    public static string BuildCoverageOutputPath()
    {
        return Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".cobertura.xml");
    }

    private static string? GenerateCoverage(string testProjectPath)
    {
        var newPath = BuildCoverageOutputPath();

        using var process = new Process
        {
            StartInfo = BuildCoverageProcessStartInfo(testProjectPath, newPath),
        };

        process.Start();
        process.WaitForExit();

        if (!IsCoverageRunSuccessful(process.ExitCode, newPath))
        {
            ReportCoverageError(process);
            return null;
        }

        return newPath;
    }

    public static bool HasStderrContent(string? stderr)
    {
        return !string.IsNullOrWhiteSpace(stderr);
    }

    private static void ReportCoverageError(Process process)
    {
        var exitString = process.ExitCode.ToString(CultureInfo.InvariantCulture);
        Console.Error.WriteLine($"Failed to generate coverage. dotnet run exit code: {exitString}");
        var stderr = process.StandardError.ReadToEnd();
        if (HasStderrContent(stderr))
        {
            Console.Error.WriteLine(stderr);
        }
    }
}

// clj-mutate-manifest-begin
// {"version":1,"testedAt":"2026-05-16T09:05:24.7989223Z","moduleHash":"7c15b78dfa39ff5d13b5d034381432371b8090c9a79a089103b68c9bcc812591","forms":[{"id":"Create","line":62,"endLine":72,"hash":"07c9260d05fe729f1ddab014c862e2908a8763c1633fbfd76359dfd96fbc4d26"},{"id":"Execute","line":74,"endLine":87,"hash":"bbab70d513055e3c58d7c9dd60e02a89523642e4ff65df5b8737d74afd2eb090"},{"id":"ValidatePaths","line":89,"endLine":104,"hash":"a01e102f128472bbc1c361c647f3ec9026a24e835f88ec8e3e5ebc7832f554c4"},{"id":"ResolveCoverage","line":106,"endLine":120,"hash":"58119deef7174a2c80f7ae080866be05164b9411dad64f72a01634f8bfb20c86"},{"id":"MissingCoverageError","line":122,"endLine":127,"hash":"d6ecf41836e2ddf58d0f6fcecac6558f3e67e6e565ae49dc5aa016093a27cccf"},{"id":"RunAnalysis","line":129,"endLine":151,"hash":"92e91a93ac6e37edd8270bbbaeb7437fae8b8ac792dd740dd3d7e5704149b8d3"},{"id":"ValidateTestProjectList","line":153,"endLine":161,"hash":"297fae9afde1db9efc393431fab5cd4a2cf59e610ffd777aaf20f56547511c6c"},{"id":"ResolveAutoCoverage","line":163,"endLine":173,"hash":"0c8b9b88fa03add5cb3c3f4e289ce93c55eb0cb09b8840569abfebf50a1ce9d1"},{"id":"GenerateCoverageForAllProjects","line":175,"endLine":206,"hash":"b08fd0f371435cd7096d5604f76ad48d6658fabf50df6eb3ebdbe7de0d306986"},{"id":"CleanupTempFiles","line":208,"endLine":221,"hash":"e45b6c3d9338fd73c8ff9307d1a6aadefe70716a3734676b5dd8b69ef1e1710d"},{"id":"BuildCoverageProcessStartInfo","line":223,"endLine":235,"hash":"adfdf9dca2c3ac5bbb3be8cc794dc34767e6cbfe56f645863d3affa70f3c1ba3"},{"id":"IsCoverageRunSuccessful","line":237,"endLine":240,"hash":"2fa581914dede555d940080a08dff683ea101b3753995fe68dd04f5321413899"},{"id":"BuildCoverageOutputPath","line":242,"endLine":245,"hash":"c522bef0ef88b21fa11999021f3e287bca7beb619ae712b082809c5f7c0bd154"},{"id":"GenerateCoverage","line":247,"endLine":266,"hash":"a87233494c38172412496935692bfe83bc90726e94b63c73ac85b0908ffb9b67"},{"id":"HasStderrContent","line":268,"endLine":271,"hash":"410a5fa24d80d280c2f8bcc8d17cc389a81f184128f969790ed8762ad7fef69a"},{"id":"ReportCoverageError","line":273,"endLine":282,"hash":"caea41c7c81d9dbb7059f4fcd57a13aa0585bc966c0a85e0c1119f8a7f0be614"}]}
// clj-mutate-manifest-end
