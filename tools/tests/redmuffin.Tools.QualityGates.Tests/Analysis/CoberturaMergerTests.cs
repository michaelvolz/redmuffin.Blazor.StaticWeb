namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using redmuffin.Tools.QualityGates.Analysis;

public sealed class CoberturaMergerTests
{
    [Test]
    public async Task Should_Copy_Single_Input_To_Output()
    {
        // Arrange
        var inputPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();
        try
        {
            var xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <coverage line-rate="1" branch-rate="1" version="1.9">
                  <packages>
                    <package>
                      <classes>
                        <class name="Foo" filename="A.cs">
                          <lines>
                            <line number="1" hits="3" branch="false"/>
                          </lines>
                        </class>
                      </classes>
                    </package>
                  </packages>
                </coverage>
                """;
            await File.WriteAllTextAsync(inputPath, xml).ConfigureAwait(false);

            // Act
            CoberturaMerger.Merge([inputPath], outputPath);

            // Assert
            await Assert.That(File.Exists(outputPath)).IsTrue();
            var merged = await File.ReadAllTextAsync(outputPath).ConfigureAwait(false);
            await Assert.That(merged).Contains("filename=\"A.cs\"");
            await Assert.That(merged).Contains("hits=\"3\"");
        }
        finally
        {
            File.Delete(inputPath);
            File.Delete(outputPath);
        }
    }

    [Test]
    public async Task Should_Include_Classes_From_Disjoint_Files()
    {
        // Arrange
        var path1 = Path.GetTempFileName();
        var path2 = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path1, """
                <?xml version="1.0" encoding="utf-8"?>
                <coverage>
                  <packages>
                    <package>
                      <classes>
                        <class name="Foo" filename="A.cs">
                          <lines>
                            <line number="1" hits="3" branch="false"/>
                          </lines>
                        </class>
                      </classes>
                    </package>
                  </packages>
                </coverage>
                """).ConfigureAwait(false);

            await File.WriteAllTextAsync(path2, """
                <?xml version="1.0" encoding="utf-8"?>
                <coverage>
                  <packages>
                    <package>
                      <classes>
                        <class name="Bar" filename="B.cs">
                          <lines>
                            <line number="5" hits="7" branch="false"/>
                          </lines>
                        </class>
                      </classes>
                    </package>
                  </packages>
                </coverage>
                """).ConfigureAwait(false);

            // Act
            CoberturaMerger.Merge([path1, path2], outputPath);

            // Assert
            await Assert.That(File.Exists(outputPath)).IsTrue();
            var merged = await File.ReadAllTextAsync(outputPath).ConfigureAwait(false);
            await Assert.That(merged).Contains("filename=\"A.cs\"");
            await Assert.That(merged).Contains("filename=\"B.cs\"");
        }
        finally
        {
            File.Delete(path1);
            File.Delete(path2);
            File.Delete(outputPath);
        }
    }

    [Test]
    public async Task Should_Sum_Hits_For_Same_Class_And_Line()
    {
        // Arrange
        var path1 = Path.GetTempFileName();
        var path2 = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path1, """
                <?xml version="1.0" encoding="utf-8"?>
                <coverage>
                  <packages>
                    <package>
                      <classes>
                        <class name="Foo" filename="Shared.cs">
                          <lines>
                            <line number="10" hits="3" branch="false"/>
                          </lines>
                        </class>
                      </classes>
                    </package>
                  </packages>
                </coverage>
                """).ConfigureAwait(false);

            await File.WriteAllTextAsync(path2, """
                <?xml version="1.0" encoding="utf-8"?>
                <coverage>
                  <packages>
                    <package>
                      <classes>
                        <class name="Foo" filename="Shared.cs">
                          <lines>
                            <line number="10" hits="5" branch="false"/>
                            <line number="11" hits="2" branch="false"/>
                          </lines>
                        </class>
                      </classes>
                    </package>
                  </packages>
                </coverage>
                """).ConfigureAwait(false);

            // Act
            CoberturaMerger.Merge([path1, path2], outputPath);

            // Assert
            await Assert.That(File.Exists(outputPath)).IsTrue();
            var result = CoverageParser.Parse(outputPath);
            // Line 10: 3 + 5 = 8
            await Assert.That(result[("Shared.cs", 10)]).IsEqualTo(8);
            // Line 11: only in file 2
            await Assert.That(result[("Shared.cs", 11)]).IsEqualTo(2);
        }
        finally
        {
            File.Delete(path1);
            File.Delete(path2);
            File.Delete(outputPath);
        }
    }

    [Test]
    public async Task Should_Throw_When_Input_Is_Empty()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            CoberturaMerger.Merge([], "/tmp/out.xml");
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task Merge_should_produce_indented_output()
    {
        var inputPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();
        try
        {
            var xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <coverage line-rate="1" branch-rate="1" version="1.9">
                  <packages>
                    <package>
                      <classes>
                        <class name="Foo" filename="A.cs">
                          <lines>
                            <line number="1" hits="3" branch="false"/>
                          </lines>
                        </class>
                      </classes>
                    </package>
                  </packages>
                </coverage>
                """;
            await File.WriteAllTextAsync(inputPath, xml).ConfigureAwait(false);
            var secondInput = Path.GetTempFileName();
            await File.WriteAllTextAsync(secondInput, xml).ConfigureAwait(false);

            CoberturaMerger.Merge([inputPath, secondInput], outputPath);
            var merged = await File.ReadAllTextAsync(outputPath).ConfigureAwait(false);
            await Assert.That(merged).Contains("\n  ");

            File.Delete(secondInput);
        }
        finally
        {
            File.Delete(inputPath);
            File.Delete(outputPath);
        }
    }
}
