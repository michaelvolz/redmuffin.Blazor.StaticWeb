namespace redmuffin.Tools.QualityGates.Tests;

using System.Diagnostics;

file static class ConfigureAwaitFixerFixture
{
    static ConfigureAwaitFixerFixture()
    {
        FixerDll = ResolveFixerDll();
    }

    public static string FixerDll { get; }
    public static string ProjectDir { get; } = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src",
                     "redmuffin.Tools.ConfigureAwaitFixer"));

    private static string ResolveFixerDll()
    {
        var dll = Path.Combine(
            ProjectDir, "bin", "Debug", "net10.0", "ConfigureAwaitFixer.dll");

        if (!File.Exists(dll))
        {
            var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{ProjectDir}\" --verbosity quiet",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            })!;
            var err = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            if (proc.ExitCode != 0)
                throw new InvalidOperationException($"Fixer build failed (exit {proc.ExitCode}): {err}");
        }

        return dll;
    }
}

public sealed class ConfigureAwaitFixerTests
{
    private static string FixerDll => ConfigureAwaitFixerFixture.FixerDll;

    [Test]
    public async Task should_add_configureawait_to_bare_await_task()
    {
        // Arrange
        using var temp = new TempDir();
        await CreateTestProjectAsync(temp.Path).ConfigureAwait(false);
        var testFile = Path.Combine(temp.Path, "TestClass.cs");
        await File.WriteAllTextAsync(testFile, """
            using System.Threading.Tasks;

            public class TestClass
            {
                public async Task DoSomethingAsync()
                {
                    await Task.Delay(100);
                }
            }
            """).ConfigureAwait(false);

        // Act — run the fixer
        var exitCode = await RunFixerAsync(temp.Path).ConfigureAwait(false);

        // Assert
        await Assert.That(exitCode).IsEqualTo(0);
        var fixedText = await File.ReadAllTextAsync(testFile).ConfigureAwait(false);
        await Assert.That(fixedText).Contains(".ConfigureAwait(false)");
    }

    [Test]
    public async Task should_not_double_fix_already_configured_await()
    {
        using var temp = new TempDir();
        await CreateTestProjectAsync(temp.Path).ConfigureAwait(false);
        var testFile = Path.Combine(temp.Path, "TestClass.cs");
        await File.WriteAllTextAsync(testFile, """
            using System.Threading.Tasks;

            public class TestClass
            {
                public async Task DoSomethingAsync()
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }
            """).ConfigureAwait(false);

        var exitCode = await RunFixerAsync(temp.Path).ConfigureAwait(false);

        await Assert.That(exitCode).IsEqualTo(0);
        var fixedText = await File.ReadAllTextAsync(testFile).ConfigureAwait(false);
        var count = CountSubstrings(fixedText, ".ConfigureAwait(false)");
        await Assert.That(count).IsEqualTo(1);
    }

    private static async Task CreateTestProjectAsync(string dir)
    {
        var csproj = Path.Combine(dir, "TestProject.csproj");
        await File.WriteAllTextAsync(csproj, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <OutputType>Library</OutputType>
              </PropertyGroup>
            </Project>
            """).ConfigureAwait(false);

        // CA2007 is disabled by default in .NET 10 — explicitly enable it
        var editorconfig = Path.Combine(dir, ".editorconfig");
        await File.WriteAllTextAsync(editorconfig, """
            [*.cs]
            dotnet_diagnostic.CA2007.severity = warning
            """).ConfigureAwait(false);
    }

    private static async Task<int> RunFixerAsync(string targetDir)
    {
        var proc = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{FixerDll}\" \"{targetDir}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        })!;

        await proc.WaitForExitAsync().ConfigureAwait(false);
        return proc.ExitCode;
    }

    private static int CountSubstrings(string text, string substring)
    {
        var count = 0;
        var i = 0;
        while ((i = text.IndexOf(substring, i, StringComparison.Ordinal)) != -1)
        {
            count++;
            i += substring.Length;
        }

        return count;
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; }

        public TempDir()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"caf-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); } catch { /* best effort */ }
        }
    }
}
