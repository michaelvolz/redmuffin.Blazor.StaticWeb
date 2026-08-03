namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using redmuffin.Tools.QualityGates.Analysis;

public sealed class SubjectRepetitionTests
{
    [Test]
    public async Task Collect_three_non_clustered_same_class_appends_subject_channel()
    {
        var methods = MakeMethods(3, "MyTests");
        var subject = new List<DuplicationChannel>();

        var nextId = SubjectRepetition.Collect(
            methods, new HashSet<int>(), subject, clusterId: 5);

        await Assert.That(nextId).IsEqualTo(6);
        await Assert.That(subject.Count).IsEqualTo(1);
        await Assert.That(subject[0].ClusterId).IsEqualTo(6);
        await Assert.That(subject[0].SharedForms).IsEqualTo(0);
        await Assert.That(subject[0].VariablePoints).IsEqualTo(0);
        await Assert.That(subject[0].InstanceCount).IsEqualTo(3);
        await Assert.That(subject[0].ChannelType).IsEqualTo(ChannelType.Subject);
    }

    [Test]
    public async Task Collect_two_methods_does_not_form_subject_channel()
    {
        var methods = MakeMethods(2, "MyTests");
        var subject = new List<DuplicationChannel>();

        var nextId = SubjectRepetition.Collect(
            methods, new HashSet<int>(), subject, clusterId: 0);

        await Assert.That(nextId).IsEqualTo(0);
        await Assert.That(subject).IsEmpty();
    }

    [Test]
    public async Task Collect_skips_already_clustered_indices()
    {
        var methods = MakeMethods(4, "MyTests");
        var subject = new List<DuplicationChannel>();
        // Only one non-clustered left after excluding 0,1,2 → no subject group of ≥3
        var clustered = new HashSet<int> { 0, 1, 2 };

        var nextId = SubjectRepetition.Collect(methods, clustered, subject, clusterId: 0);

        await Assert.That(nextId).IsEqualTo(0);
        await Assert.That(subject).IsEmpty();
    }

    [Test]
    public async Task Collect_two_subject_groups_increments_cluster_ids()
    {
        var methods = MakeMethods(3, "A").Concat(MakeMethods(3, "B")).ToList();
        var subject = new List<DuplicationChannel>();

        var nextId = SubjectRepetition.Collect(
            methods, new HashSet<int>(), subject, clusterId: 0);

        await Assert.That(nextId).IsEqualTo(2);
        await Assert.That(subject.Count).IsEqualTo(2);
        var ids = subject.Select(c => c.ClusterId).OrderBy(id => id).ToList();
        await Assert.That(ids[0]).IsEqualTo(1);
        await Assert.That(ids[1]).IsEqualTo(2);
        await Assert.That(subject.All(c => c.SharedForms == 0)).IsTrue();
        await Assert.That(subject.All(c => c.VariablePoints == 0)).IsTrue();
    }

    private static List<TestMethod> MakeMethods(int count, string className)
    {
        var tree = CSharpSyntaxTree.ParseText("class X { void M() { } }");
        var body = tree.GetCompilationUnitRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;

        return Enumerable.Range(0, count)
            .Select(i => new TestMethod(
                MethodName: $"test_{i}",
                FilePath: "Test.cs",
                StartLine: i + 1,
                EndLine: i + 1,
                BodySyntax: body,
                ContainerClassName: className))
            .ToList();
    }
}
