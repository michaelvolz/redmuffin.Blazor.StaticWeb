namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using Microsoft.CodeAnalysis.CSharp;
using redmuffin.Tools.QualityGates.Analysis;

public sealed class ScrapDuplicationTests
{
    // --- JaccardSimilarity ---

    [Test]
    public async Task should_return_one_for_identical_sets()
    {
        var setA = new HashSet<string> { "a", "b", "c" };
        var setB = new HashSet<string> { "a", "b", "c" };

        var similarity = ScrapDuplication.JaccardSimilarity(setA, setB);

        await Assert.That(similarity).IsEqualTo(1.0);
    }

    [Test]
    public async Task should_return_zero_for_disjoint_sets()
    {
        var setA = new HashSet<string> { "a", "b" };
        var setB = new HashSet<string> { "c", "d" };

        var similarity = ScrapDuplication.JaccardSimilarity(setA, setB);

        await Assert.That(similarity).IsEqualTo(0.0);
    }

    [Test]
    public async Task should_return_half_for_half_overlapping_sets()
    {
        var setA = new HashSet<string> { "a", "b" };
        var setB = new HashSet<string> { "b" };

        var similarity = ScrapDuplication.JaccardSimilarity(setA, setB);

        await Assert.That(similarity).IsEqualTo(0.5);
    }

    [Test]
    public async Task should_return_zero_for_both_empty_sets()
    {
        var setA = new HashSet<string>();
        var setB = new HashSet<string>();

        var similarity = ScrapDuplication.JaccardSimilarity(setA, setB);

        await Assert.That(similarity).IsEqualTo(0.0);
    }

    [Test]
    public async Task should_return_zero_when_one_set_is_empty()
    {
        var setA = new HashSet<string> { "a" };
        var setB = new HashSet<string>();

        var similarity = ScrapDuplication.JaccardSimilarity(setA, setB);

        await Assert.That(similarity).IsEqualTo(0.0);
    }

    // --- Analyze: clustering ---

    [Test]
    public async Task should_cluster_similar_methods_as_harmful_duplication()
    {
        var methods = ParseMethodsFromSource("""
            using TUnit.Core;
            public class MyTests
            {
                [Test]
                public void test_a() { var x = 1; Assert.That(x).IsNotNull(); }

                [Test]
                public void test_b() { var y = 2; Assert.That(y).IsNotNull(); }
            }
            """);

        var results = ScrapDuplication.Analyze(methods);

        await Assert.That(results.HarmfulDuplication.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task should_not_cluster_dissimilar_methods()
    {
        var methods = ParseMethodsFromSource("""
            using TUnit.Core;
            public class MyTests
            {
                [Test]
                public void test_a() { var x = 1; Assert.That(x).IsNotNull(); }

                [Test]
                public void test_b() { var z = "hello"; SomethingDifferent(z); }
            }
            """);

        var results = ScrapDuplication.Analyze(methods);

        await Assert.That(results.HarmfulDuplication).IsEmpty();
        await Assert.That(results.SubjectRepetition).IsEmpty();
        await Assert.That(results.CaseMatrixRepetition).IsEmpty();
    }

    [Test]
    public async Task should_return_no_clusters_for_single_method()
    {
        var methods = ParseMethodsFromSource("""
            using TUnit.Core;
            public class MyTests
            {
                [Test]
                public void test_a() { var x = 1; Assert.That(x).IsNotNull(); }
            }
            """);

        var results = ScrapDuplication.Analyze(methods);

        await Assert.That(results.HarmfulDuplication).IsEmpty();
        await Assert.That(results.SubjectRepetition).IsEmpty();
        await Assert.That(results.CaseMatrixRepetition).IsEmpty();
    }

    [Test]
    public async Task should_return_no_clusters_for_empty_method_list()
    {
        var results = ScrapDuplication.Analyze(Array.Empty<TestMethod>());

        await Assert.That(results.HarmfulDuplication).IsEmpty();
        await Assert.That(results.SubjectRepetition).IsEmpty();
        await Assert.That(results.CaseMatrixRepetition).IsEmpty();
    }

    // --- Channel classification: subject repetition ---

    [Test]
    public async Task should_classify_same_class_dissimilar_methods_as_subject_repetition()
    {
        var methods = ParseMethodsFromSource("""
            using TUnit.Core;
            public class MyTests
            {
                [Test]
                public void test_a() { var x = 1; Assert.That(x).IsNotNull(); }

                [Test]
                public void test_b() { SetupDb(); var result = Query("SELECT 1"); Verify(result); }

                [Test]
                public void test_c()
                {
                    if (true)
                    {
                        DoSomething();
                    }
                }

                [Test]
                public void test_d()
                {
                    for (var i = 0; i < 10; i++)
                    {
                        Process(i);
                    }
                }
            }
            """);

        var results = ScrapDuplication.Analyze(methods);

        // Four genuinely different test structures in same class → subject repetition cluster
        await Assert.That(results.SubjectRepetition.Count).IsGreaterThan(0);
    }

    /// <summary>
    /// Parses all [Test] methods from a C# source string and returns them
    /// as TestMethod records using TestMethodParser's logic.
    /// </summary>
    private static List<TestMethod> ParseMethodsFromSource(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();

        return root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .Where(m => m.AttributeLists
                .SelectMany(a => a.Attributes)
                .Any(a => a.Name.ToString() == "Test"))
            .Select(m => new TestMethod(
                MethodName: m.Identifier.Text,
                FilePath: "Test.cs",
                StartLine: m.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                EndLine: m.GetLocation().GetLineSpan().EndLinePosition.Line + 1,
                BodySyntax: m.Body ?? (Microsoft.CodeAnalysis.SyntaxNode)m,
                ContainerClassName: ((Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax)m.Parent!).Identifier.Text
            ))
            .ToList();
    }

    [Test]
    public async Task RouteToChannel_harmful_adds_to_harmful_list()
    {
        var harmful = new List<DuplicationChannel>();
        var caseMatrix = new List<DuplicationChannel>();
        var subject = new List<DuplicationChannel>();
        var channel = new DuplicationChannel(1, [], 3, 2, 2, ChannelType.Harmful);

        ScrapDuplication.RouteToChannel(
            ChannelType.Harmful, channel, harmful, caseMatrix, subject);

        await Assert.That(harmful.Count).IsEqualTo(1);
        await Assert.That(caseMatrix.Count).IsEqualTo(0);
        await Assert.That(subject.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RouteToChannel_case_matrix_adds_to_case_matrix_list()
    {
        var harmful = new List<DuplicationChannel>();
        var caseMatrix = new List<DuplicationChannel>();
        var subject = new List<DuplicationChannel>();
        var channel = new DuplicationChannel(2, [], 1, 5, 3, ChannelType.CaseMatrix);

        ScrapDuplication.RouteToChannel(
            ChannelType.CaseMatrix, channel, harmful, caseMatrix, subject);

        await Assert.That(harmful.Count).IsEqualTo(0);
        await Assert.That(caseMatrix.Count).IsEqualTo(1);
        await Assert.That(subject.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RouteToChannel_subject_adds_to_subject_list()
    {
        var harmful = new List<DuplicationChannel>();
        var caseMatrix = new List<DuplicationChannel>();
        var subject = new List<DuplicationChannel>();
        var channel = new DuplicationChannel(3, [], 0, 0, 2, ChannelType.Subject);

        ScrapDuplication.RouteToChannel(
            ChannelType.Subject, channel, harmful, caseMatrix, subject);

        await Assert.That(harmful.Count).IsEqualTo(0);
        await Assert.That(caseMatrix.Count).IsEqualTo(0);
        await Assert.That(subject.Count).IsEqualTo(1);
    }
}
