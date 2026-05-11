namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using redmuffin.Tools.QualityGates.Analysis;

public sealed class DupesDetectorHelperTests
{
    private static DupesCandidate C(int score, string left, int lStart, int rStart) =>
        new(score, left, lStart, lStart + 1, "Right.cs", rStart, rStart + 1, LeftNodes: 10, RightNodes: 10);

    [Test]
    public async Task CandidateComparer_sorts_by_score_descending()
    {
        var a = C(100, "A.cs", 1, 1);
        var b = C(200, "B.cs", 1, 1);
        await Assert.That(DupesDetector.CandidateComparer(a, b)).IsGreaterThan(0);
        await Assert.That(DupesDetector.CandidateComparer(b, a)).IsLessThan(0);
    }

    [Test]
    public async Task CandidateComparer_same_score_sorts_by_file()
    {
        var a = C(100, "A.cs", 1, 1);
        var b = C(100, "B.cs", 1, 1);
        await Assert.That(DupesDetector.CandidateComparer(a, b)).IsLessThan(0);
    }

    [Test]
    public async Task CandidateComparer_same_score_same_file_sorts_by_line()
    {
        var a = C(100, "A.cs", 1, 1);
        var b = C(100, "A.cs", 2, 2);
        await Assert.That(DupesDetector.CandidateComparer(a, b)).IsLessThan(0);
    }

    [Test]
    public async Task CandidateComparer_equal_candidates_returns_zero()
    {
        var a = C(100, "A.cs", 1, 1);
        var b = C(100, "A.cs", 1, 1);
        await Assert.That(DupesDetector.CandidateComparer(a, b)).IsEqualTo(0);
    }

    [Test]
    public async Task CandidateComparer_different_score_returns_nonzero()
    {
        var a = C(80, "A.cs", 1, 1);
        var b = C(100, "A.cs", 1, 1);
        var result = DupesDetector.CandidateComparer(a, b);
        await Assert.That(result).IsNotEqualTo(0);
    }
}
