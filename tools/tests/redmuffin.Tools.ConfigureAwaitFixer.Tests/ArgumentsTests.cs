namespace redmuffin.Tools.ConfigureAwaitFixer.Tests;

using redmuffin.Tools.ConfigureAwaitFixer;

public sealed class ArgumentsTests
{
    [Test]
    public async Task Parse_ReturnsArguments_WhenFileFlagHasValue()
    {
        // Arrange
        using var temp = new TempDir();
        var projectPath = await CreateProjectAsync(temp.Path).ConfigureAwait(false);
        var sourceFile = Path.Combine(temp.Path, "Source.cs");

        // Act
        var result = Arguments.Parse(["--file", sourceFile]);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.SingleFile).IsEqualTo(sourceFile);
        await Assert.That(result.TargetDir).IsEqualTo(temp.Path);
        await Assert.That(result.ProjectPath).IsEqualTo(projectPath);
    }

    [Test]
    public async Task Parse_ReturnsNull_WhenFileFlagHasNoValue()
    {
        // Act
        var result = Arguments.Parse(["--file"]);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Parse_UsesLastPositionalArgumentAsTargetDir()
    {
        // Arrange
        using var temp = new TempDir();
        await CreateProjectAsync(temp.Path).ConfigureAwait(false);
        var sourceFile = Path.Combine(temp.Path, "Source.cs");
        var otherDir = Path.Combine(temp.Path, "other");
        Directory.CreateDirectory(otherDir);
        var otherProjectPath = await CreateProjectAsync(otherDir, "OtherProject.csproj").ConfigureAwait(false);

        // Act
        var result = Arguments.Parse(["--file", sourceFile, otherDir]);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.SingleFile).IsEqualTo(sourceFile);
        await Assert.That(result.TargetDir).IsEqualTo(otherDir);
        await Assert.That(result.ProjectPath).IsEqualTo(otherProjectPath);
    }

    [Test]
    public async Task Parse_ReturnsPositionalTargetDirArguments()
    {
        // Arrange
        using var temp = new TempDir();
        var projectPath = await CreateProjectAsync(temp.Path).ConfigureAwait(false);

        // Act
        var result = Arguments.Parse([temp.Path]);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.TargetDir).IsEqualTo(temp.Path);
        await Assert.That(result.SingleFile).IsNull();
        await Assert.That(result.ProjectPath).IsEqualTo(projectPath);
    }

    [Test]
    public async Task Parse_ReturnsNull_WhenNoCsprojAboveTargetDir()
    {
        // Arrange
        using var temp = new TempDir();

        // Act
        var result = Arguments.Parse([temp.Path]);

        // Assert
        await Assert.That(Directory.EnumerateFiles(temp.Path, "*.csproj", SearchOption.AllDirectories)).IsEmpty();
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task FindCsproj_ReturnsCsprojInTargetDir()
    {
        // Arrange
        using var temp = new TempDir();
        var projectPath = await CreateProjectAsync(temp.Path).ConfigureAwait(false);

        // Act
        var result = Arguments.FindCsproj(temp.Path);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsEqualTo(projectPath);
    }

    [Test]
    public async Task FindCsproj_WalksUpToAncestorDirectory()
    {
        // Arrange
        using var temp = new TempDir();
        var projectPath = await CreateProjectAsync(temp.Path).ConfigureAwait(false);
        var deepTarget = Path.Combine(temp.Path, "src", "Features", "Raindrop");
        Directory.CreateDirectory(deepTarget);

        // Act
        var result = Arguments.FindCsproj(deepTarget);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsEqualTo(projectPath);
    }

    [Test]
    public async Task FindCsproj_ReturnsNull_WhenNoCsprojInTree()
    {
        // Arrange
        using var temp = new TempDir();

        // Act
        var result = Arguments.FindCsproj(temp.Path);

        // Assert
        await Assert.That(Directory.EnumerateFiles(temp.Path, "*.csproj", SearchOption.AllDirectories)).IsEmpty();
        await Assert.That(result).IsNull();
    }

    private static async Task<string> CreateProjectAsync(string dir, string name = "TestProject.csproj")
    {
        var path = Path.Combine(dir, name);
        await File.WriteAllTextAsync(path, "<Project Sdk=\"Microsoft.NET.Sdk\" />").ConfigureAwait(false);
        return path;
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; }

        public TempDir()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"caf-args-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
