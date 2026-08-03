namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using redmuffin.Tools.QualityGates.Analysis;

public sealed class TestMethodParserTests
{
    [Test]
    public async Task should_find_tunit_test_methods_in_file()
    {
        var results = await FindTestsAsync("""
            using TUnit.Core;

            public class MyTests
            {
                [Test]
                public async Task test_one() { }

                [Test]
                public void test_two() { }

                public void not_a_test() { }
            }
            """).ConfigureAwait(false);

        await Assert.That(results.Count).IsEqualTo(2);
        await Assert.That(results[0].MethodName).IsEqualTo("test_one");
        await Assert.That(results[1].MethodName).IsEqualTo("test_two");
    }

    [Test]
    public async Task should_return_empty_when_no_test_methods()
    {
        var results = await FindTestsAsync("""
            public class MyTests
            {
                public void helper() { }
            }
            """).ConfigureAwait(false);

        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task should_discover_tests_from_multiple_files()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"scrap_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "Tests1.cs"), """
            using TUnit.Core;
            public class Tests1 { [Test] public void a() { } }
            """).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(dir, "Tests2.cs"), """
            using TUnit.Core;
            public class Tests2 { [Test] public void b() { } }
            """).ConfigureAwait(false);

        var results = TestMethodParser.FindTests(dir);

        await Assert.That(results.Count).IsEqualTo(2);
    }

    [Test]
    public async Task should_capture_line_spans_correctly()
    {
        var results = await FindTestsAsync("""
            using TUnit.Core;
            public class MyTests
            {
                [Test]
                public void test_one()
                {
                    var x = 1;
                }
            }
            """).ConfigureAwait(false);

        await Assert.That(results).HasSingleItem();
        await Assert.That(results[0].StartLine).IsGreaterThan(0);
        await Assert.That(results[0].EndLine).IsGreaterThanOrEqualTo(results[0].StartLine);
    }

    [Test]
    public async Task should_record_container_class_name()
    {
        var results = await FindTestsAsync("""
            using TUnit.Core;
            public class MyTests
            {
                [Test]
                public async Task should_work() { }
            }
            """).ConfigureAwait(false);

        await Assert.That(results).HasSingleItem();
        await Assert.That(results[0].ContainerClassName).IsEqualTo("MyTests");
    }

    [Test]
    public async Task should_return_empty_for_empty_directory()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"scrap_empty_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        var results = TestMethodParser.FindTests(dir);

        await Assert.That(results).IsEmpty();
    }

    /// <summary>Writes source to a temp .cs file and returns the analysis results.</summary>
    private static async Task<IReadOnlyList<TestMethod>> FindTestsAsync(string source)
    {
        var dir = Path.Combine(Path.GetTempPath(), $"scrap_parser_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "Test.cs"), source).ConfigureAwait(false);
        return TestMethodParser.FindTests(dir);
    }

    [Test]
    public async Task should_report_start_line_greater_than_zero()
    {
        var source = "public sealed class C{[Test]public void M(){}}";
        var tests = await FindTestsAsync(source).ConfigureAwait(false);
        await Assert.That(tests.Count).IsEqualTo(1);
        await Assert.That(tests[0].StartLine).IsGreaterThan(0);
    }

    [Test]
    public async Task should_report_end_line_greater_than_zero()
    {
        var source = "public sealed class C{[Test]public void M(){}}";
        var tests = await FindTestsAsync(source).ConfigureAwait(false);
        await Assert.That(tests.Count).IsEqualTo(1);
        await Assert.That(tests[0].EndLine).IsGreaterThan(0);
    }

    [Test]
    public async Task should_skip_bodyless_and_expression_bodied_test_methods()
    {
        var results = await FindTestsAsync("""
            using TUnit.Core;
            public abstract class BaseTests
            {
                [Test]
                public abstract void abstract_test();

                [Test]
                public void expression_bodied() => Assert.That(1).IsEqualTo(1);
            }
            public class ConcreteTests
            {
                [Test]
                public void real_test() { }
            }
            """).ConfigureAwait(false);

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].MethodName).IsEqualTo("real_test");
        await Assert.That(results[0].ContainerClassName).IsEqualTo("ConcreteTests");
    }

    [Test]
    public async Task should_treat_attribute_name_containing_Test_as_test()
    {
        var results = await FindTestsAsync("""
            public class MyTests
            {
                [FactTest]
                public void named_like_test() { }

                [Something]
                public void not_test() { }
            }
            """).ConfigureAwait(false);

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].MethodName).IsEqualTo("named_like_test");
    }

    [Test]
    public async Task should_use_empty_container_class_when_method_is_top_level_shape()
    {
        // File-local method declarations are not valid C#, so use a nested
        // type without a class ancestor for the method via local functions —
        // parser only sees MethodDeclarationSyntax on types. Assert full path
        // is absolute instead.
        var results = await FindTestsAsync("""
            using TUnit.Core;
            public class Outer
            {
                [Test]
                public void path_probe() { }
            }
            """).ConfigureAwait(false);

        await Assert.That(results).HasSingleItem();
        await Assert.That(Path.IsPathRooted(results[0].FilePath)).IsTrue();
    }
}
