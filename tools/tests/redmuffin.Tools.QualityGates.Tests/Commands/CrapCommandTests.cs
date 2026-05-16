namespace redmuffin.Tools.QualityGates.Tests.Commands;

using redmuffin.Tools.QualityGates.Analysis;
using redmuffin.Tools.QualityGates.Commands;

public sealed class CrapCommandTests
{
    [Test]
    public async Task should_return_exit_code_0_when_all_methods_below_threshold()
    {
        var results = new List<MethodCrap>
        {
            new("Foo", "A.cs", 10, 1, 1.0, 1.0),
            new("Bar", "B.cs", 20, 2, 0.8, 5.2),
        };

        var exitCode = CrapHandler.Run(results, maxCrap: 8);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task should_return_exit_code_2_when_any_method_breaches_threshold()
    {
        var results = new List<MethodCrap>
        {
            new("Foo", "A.cs", 10, 1, 1.0, 1.0),
            new("Bad", "B.cs", 20, 3, 0.0, 12.0),
        };

        var exitCode = CrapHandler.Run(results, maxCrap: 8);

        await Assert.That(exitCode).IsEqualTo(2);
    }

    [Test]
    public async Task should_respect_custom_max_crap_threshold()
    {
        var results = new List<MethodCrap>
        {
            new("Mid", "A.cs", 10, 2, 0.5, 2.5),
        };

        var exitCode = CrapHandler.Run(results, maxCrap: 3);
        await Assert.That(exitCode).IsEqualTo(0);

        var exitCode2 = CrapHandler.Run(results, maxCrap: 2);
        await Assert.That(exitCode2).IsEqualTo(2);
    }

    [Test]
    public async Task should_output_table_with_headers()
    {
        var results = new List<MethodCrap>
        {
            new("Foo", "A.cs", 10, 1, 1.0, 1.0),
        };

        using var output = new StringWriter();
        CrapHandler.Run(results, maxCrap: 8, output);

        var text = output.ToString();
        await Assert.That(text).Contains("CRAP");
        await Assert.That(text).Contains("CC");
        await Assert.That(text).Contains("Coverage");
        await Assert.That(text).Contains("Method");
        await Assert.That(text).Contains("File:Line");
    }

    [Test]
    public async Task should_sort_results_by_crap_descending()
    {
        var results = new List<MethodCrap>
        {
            new("Low", "A.cs", 10, 1, 1.0, 1.0),
            new("High", "B.cs", 20, 3, 0.0, 12.0),
            new("Mid", "C.cs", 30, 2, 0.5, 2.5),
        };

        using var output = new StringWriter();
        CrapHandler.Run(results, maxCrap: 8, output);

        var text = output.ToString();
        var highIndex = text.IndexOf("High", StringComparison.Ordinal);
        var midIndex = text.IndexOf("Mid", StringComparison.Ordinal);
        var lowIndex = text.IndexOf("Low", StringComparison.Ordinal);

        await Assert.That(highIndex).IsLessThan(midIndex);
        await Assert.That(midIndex).IsLessThan(lowIndex);
    }

    [Test]
    public async Task should_show_method_and_file_location_in_table()
    {
        var results = new List<MethodCrap>
        {
            new("DoWork", "/src/app/Worker.cs", 42, 2, 0.8, 5.2),
        };

        using var output = new StringWriter();
        CrapHandler.Run(results, maxCrap: 8, output);

        var text = output.ToString();
        await Assert.That(text).Contains("DoWork");
        await Assert.That(text).Contains("/src/app/Worker.cs");
    }

    [Test]
    public async Task should_return_exit_code_0_for_empty_results()
    {
        var results = new List<MethodCrap>();

        var exitCode = CrapHandler.Run(results, maxCrap: 8);

        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task ValidatePaths_should_return_1_when_project_directory_does_not_exist()
    {
        using var tempFile = new TempFile();
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        var error = CrapCommand.ValidatePaths(nonExistentDir, tempFile.Path);

        await Assert.That(error).IsEqualTo(1);
    }

    [Test]
    public async Task ValidatePaths_should_return_1_when_coverage_file_does_not_exist()
    {
        var existingDir = Path.GetTempPath();
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        var error = CrapCommand.ValidatePaths(existingDir, nonExistentFile);

        await Assert.That(error).IsEqualTo(1);
    }

    [Test]
    public async Task ValidatePaths_should_return_null_when_both_exist()
    {
        using var tempFile = new TempFile();
        var existingDir = Path.GetDirectoryName(tempFile.Path)!;

        var error = CrapCommand.ValidatePaths(existingDir, tempFile.Path);

        await Assert.That(error).IsNull();
    }

    private static string ResolveTestProjectPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "redmuffin.Tools.QualityGates.Tests.csproj")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("Could not find test project root");
    }

    private sealed class TempFile : IDisposable
    {
        public string Path { get; }

        public TempFile()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                System.IO.Path.GetRandomFileName());
            File.WriteAllText(Path, string.Empty);
        }

        public void Dispose() => File.Delete(Path);
    }

    [Test]
    public async Task Execute_should_return_1_when_no_coverage_file_and_no_auto_coverage()
    {
        var exitCode = CrapCommand.Execute(
            projectPath: Path.GetTempPath(),
            coveragePath: null,
            maxCrap: 8,
            changedOnly: false);

        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task RunAnalysis_should_return_zero_with_valid_inputs_and_no_branch()
    {
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "MutationTarget");
        var coveragePath = await CreateMinimalCoverageXml("Calculator.cs", 5, 1).ConfigureAwait(false);

        try
        {
            var exitCode = CrapCommand.RunAnalysis(fixtureDir, coveragePath, maxCrap: 8, changedOnly: false);
            await Assert.That(exitCode).IsEqualTo(0);
        }
        finally
        {
            File.Delete(coveragePath);
        }
    }

    [Test]
    public async Task RunAnalysis_should_return_zero_with_changed_only()
    {
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "MutationTarget");
        var coveragePath = await CreateMinimalCoverageXml("Calculator.cs", 5, 1).ConfigureAwait(false);

        try
        {
            var exitCode = CrapCommand.RunAnalysis(fixtureDir, coveragePath, maxCrap: 8, changedOnly: true);
            await Assert.That(exitCode).IsEqualTo(0);
        }
        finally
        {
            File.Delete(coveragePath);
        }
    }

    [Test]
    public async Task RunAnalysis_should_return_violations_when_not_changed_only()
    {
        var testProjectDir = ResolveTestProjectPath();
        var coveragePath = await CreateMinimalCoverageXml("CrapCommandTests.cs", 1, 1).ConfigureAwait(false);

        try
        {
            var exitCode = CrapCommand.RunAnalysis(testProjectDir, coveragePath, maxCrap: 8, changedOnly: false);
            await Assert.That(exitCode).IsEqualTo(2);
        }
        finally
        {
            File.Delete(coveragePath);
        }
    }

    [Test]
    public async Task RunAnalysis_should_return_1_on_invalid_coverage_xml()
    {
        var fixtureDir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "MutationTarget");
        var coveragePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(coveragePath, "not valid xml <<<").ConfigureAwait(false);

        try
        {
            var exitCode = CrapCommand.RunAnalysis(fixtureDir, coveragePath, maxCrap: 8, changedOnly: false);
            await Assert.That(exitCode).IsEqualTo(1);
        }
        finally
        {
            File.Delete(coveragePath);
        }
    }

    private static async Task<string> CreateMinimalCoverageXml(string filename, int lineNumber, int hits)
    {
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path,
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
              <packages>
                <package>
                  <classes>
                    <class filename="{filename}">
                      <lines>
                        <line number="{lineNumber}" hits="{hits}"/>
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """).ConfigureAwait(false);
        return path;
    }

    [Test]
    [Skip("Requires real coverage generation — slow in CI")]
    public async Task ResolveCoverage_should_return_auto_coverage_when_auto_is_true_and_path_is_null()
    {
        var testProject = ResolveTestProjectPath();
        var result = CrapCommand.ResolveCoverage(
            coveragePath: null,
            testProjectPaths: [testProject],
            autoCoverage: true);

        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task ResolveCoverage_should_return_error_when_path_is_null_and_auto_is_false()
    {
        var result = CrapCommand.ResolveCoverage(
            coveragePath: null,
            testProjectPaths: null,
            autoCoverage: false);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ResolveCoverage_should_return_provided_path_when_not_null()
    {
        using var tempFile = new TempFile();
        var result = CrapCommand.ResolveCoverage(
            coveragePath: tempFile.Path,
            testProjectPaths: null,
            autoCoverage: false);

        await Assert.That(result).IsEqualTo(tempFile.Path);
    }

    [Test]
    public async Task BuildCoverageProcessStartInfo_should_set_expected_properties()
    {
        var startInfo = CrapCommand.BuildCoverageProcessStartInfo(
            testProjectPath: "/test/project",
            outputPath: "/tmp/output.xml");

        await Assert.That(startInfo.FileName).IsEqualTo("dotnet");
        await Assert.That(startInfo.RedirectStandardOutput).IsTrue();
        await Assert.That(startInfo.RedirectStandardError).IsTrue();
        await Assert.That(startInfo.UseShellExecute).IsFalse();
        await Assert.That(startInfo.CreateNoWindow).IsTrue();
    }

    [Test]
    public async Task BuildCoverageProcessStartInfo_should_include_paths_in_arguments()
    {
        var startInfo = CrapCommand.BuildCoverageProcessStartInfo(
            testProjectPath: "/test/project",
            outputPath: "/tmp/output.xml");

        await Assert.That(startInfo.Arguments).Contains("/test/project");
        await Assert.That(startInfo.Arguments).Contains("/tmp/output.xml");
    }

    [Test]
    [MethodDataSource(nameof(IsCoverageRunSuccessful_Data))]
    public async Task IsCoverageRunSuccessful_should_return_expected(int exitCode, bool fileExists, bool expected)
    {
        using var tempFile = fileExists ? new TempFile() : null;
        var filePath = tempFile?.Path ?? "/nonexistent/path.xml";

        var result = CrapCommand.IsCoverageRunSuccessful(exitCode, filePath);

        await Assert.That(result).IsEqualTo(expected);
    }

    public static IEnumerable<(int ExitCode, bool FileExists, bool Expected)> IsCoverageRunSuccessful_Data()
    {
        yield return (0, true, true);
        yield return (0, false, false);
        yield return (1, true, false);
        yield return (1, false, false);
    }

    [Test]
    public async Task ValidateTestProjectList_should_return_error_when_null()
    {
        var result = CrapCommand.ValidateTestProjectList(null);
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task ValidateTestProjectList_should_return_error_when_empty()
    {
        var result = CrapCommand.ValidateTestProjectList([]);
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task ValidateTestProjectList_should_return_null_when_populated()
    {
        var result = CrapCommand.ValidateTestProjectList(["/some/path"]);
        await Assert.That(result).IsNull();
    }

    // ── MergeTempCoverageFiles ──

    [Test]
    public async Task MergeTempCoverageFiles_should_return_single_path()
    {
        using var temp = new TempFile();
        var result = CrapCommand.MergeTempCoverageFiles([temp.Path]);
        await Assert.That(result).IsEqualTo(temp.Path);
    }

    [Test]
    public async Task MergeTempCoverageFiles_should_merge_multiple()
    {
        var files = new List<string>();
        try
        {
            files.Add(await CreateMinimalCoverageXml("A.cs", 1, 5).ConfigureAwait(false));
            files.Add(await CreateMinimalCoverageXml("A.cs", 1, 3).ConfigureAwait(false));

            var merged = CrapCommand.MergeTempCoverageFiles(files);
        await Assert.That(merged).IsNotNull();
        await Assert.That(merged).IsNotEqualTo(files[0]);
        await Assert.That(File.Exists(merged)).IsTrue();

        File.Delete(merged);
    }
    finally
    {
        foreach (var f in files) File.Delete(f);
    }
}

[Test]
public async Task GenerateCoverageForAllProjects_should_return_null_on_failure()
{
    var result = CrapCommand.GenerateCoverageForAllProjects(
        ["/fake1", "/fake2"],
        generateCoverage: _ => null);

    await Assert.That(result).IsNull();
}

[Test]
public async Task GenerateCoverageForAllProjects_should_merge_two()
{
    var t1 = await CreateMinimalCoverageXml("A.cs", 1, 2).ConfigureAwait(false);
    var t2 = await CreateMinimalCoverageXml("A.cs", 1, 3).ConfigureAwait(false);
    string? merged = null;

    try
    {
        merged = CrapCommand.GenerateCoverageForAllProjects(
            ["/p1", "/p2"],
            generateCoverage: i => i == "/p1" ? t1 : t2);

        await Assert.That(merged).IsNotNull();
        await Assert.That(File.Exists(merged)).IsTrue();
    }
    finally
    {
        File.Delete(t1);
        File.Delete(t2);
        if (merged is not null) File.Delete(merged);
    }
}
}
