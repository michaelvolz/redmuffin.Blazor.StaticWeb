namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using redmuffin.Tools.QualityGates.Analysis;

public sealed class MethodMapperTests
{
    [Test]
    public async Task should_compute_crap1_for_cc1_full_coverage()
    {
        var methods = new[] { new MethodComplexity("Foo", "A.cs", 1, 10, 1) };
        var coverage = new Dictionary<(string, int), int>
        {
            [("A.cs", 1)] = 5,
            [("A.cs", 2)] = 3,
            [("A.cs", 3)] = 1,
            [("A.cs", 4)] = 1,
            [("A.cs", 5)] = 1,
            [("A.cs", 6)] = 1,
            [("A.cs", 7)] = 1,
            [("A.cs", 8)] = 1,
            [("A.cs", 9)] = 1,
            [("A.cs", 10)] = 1,
        };

        var result = MethodMapper.Map(methods, coverage);

        // CRAP = 1² × (1 - 1.0)³ + 1 = 1 × 0 + 1 = 1
        await Assert.That(result).HasSingleItem();
        await Assert.That(result[0].CrapScore).IsEqualTo(1.0);
        await Assert.That(result[0].Coverage).IsEqualTo(1.0);
    }

    [Test]
    public async Task should_compute_crap2_for_cc2_full_coverage()
    {
        var methods = new[] { new MethodComplexity("Bar", "B.cs", 1, 5, 2) };
        var coverage = new Dictionary<(string, int), int>
        {
            [("B.cs", 1)] = 1,
            [("B.cs", 2)] = 1,
            [("B.cs", 3)] = 1,
            [("B.cs", 4)] = 1,
            [("B.cs", 5)] = 1,
        };

        var result = MethodMapper.Map(methods, coverage);

        // CRAP = 2² × 0³ + 2 = 4 × 0 + 2 = 2
        await Assert.That(result[0].CrapScore).IsEqualTo(2.0);
        await Assert.That(result[0].Coverage).IsEqualTo(1.0);
    }

    [Test]
    public async Task should_compute_crap_for_cc2_50percent_coverage()
    {
        var methods = new[] { new MethodComplexity("Baz", "C.cs", 1, 4, 2) };
        var coverage = new Dictionary<(string, int), int>
        {
            [("C.cs", 1)] = 5,
            [("C.cs", 2)] = 0,
            [("C.cs", 3)] = 0,
            [("C.cs", 4)] = 3,
        };

        var result = MethodMapper.Map(methods, coverage);

        // coverage = 2/4 = 0.5
        // CRAP = 2² × (1 - 0.5)³ + 2 = 4 × 0.125 + 2 = 2.5
        await Assert.That(result[0].Coverage).IsEqualTo(0.5);
        await Assert.That(result[0].CrapScore).IsEqualTo(2.5);
    }

    [Test]
    public async Task should_compute_crap_12_for_cc3_zero_coverage()
    {
        var methods = new[] { new MethodComplexity("Qux", "D.cs", 1, 3, 3) };
        var coverage = new Dictionary<(string, int), int>();
        // No coverage data for D.cs → all lines uncovered

        var result = MethodMapper.Map(methods, coverage);

        // CRAP = 3² × (1 - 0)³ + 3 = 9 × 1 + 3 = 12
        await Assert.That(result[0].Coverage).IsEqualTo(0.0);
        await Assert.That(result[0].CrapScore).IsEqualTo(12.0);
    }

    [Test]
    public async Task should_compute_crap5_for_cc5_full_coverage()
    {
        var methods = new[] { new MethodComplexity("Foo", "E.cs", 1, 5, 5) };
        var coverage = new Dictionary<(string, int), int>
        {
            [("E.cs", 1)] = 1,
            [("E.cs", 2)] = 1,
            [("E.cs", 3)] = 1,
            [("E.cs", 4)] = 1,
            [("E.cs", 5)] = 1,
        };

        var result = MethodMapper.Map(methods, coverage);

        // CRAP = 25 × 0 + 5 = 5
        await Assert.That(result[0].CrapScore).IsEqualTo(5.0);
    }

    [Test]
    public async Task should_compute_crap_for_cc5_80percent_coverage()
    {
        var methods = new[] { new MethodComplexity("Foo", "F.cs", 1, 5, 5) };
        var coverage = new Dictionary<(string, int), int>
        {
            [("F.cs", 1)] = 1,
            [("F.cs", 2)] = 2,
            [("F.cs", 3)] = 1,
            [("F.cs", 4)] = 0,
            [("F.cs", 5)] = 1,
        };

        var result = MethodMapper.Map(methods, coverage);

        // coverage = 4/5 = 0.8
        // CRAP = 25 × (0.2)³ + 5 = 25 × 0.008 + 5 = 5.2
        await Assert.That(result[0].Coverage).IsEqualTo(0.8);
        await Assert.That(result[0].CrapScore).IsEqualTo(5.2);
    }

    [Test]
    public async Task should_handle_method_with_zero_lines_as_zero_coverage()
    {
        var methods = new[] { new MethodComplexity("Empty", "G.cs", 5, 4, 2) };
        // startLine (5) > endLine (4) — zero-line span
        var coverage = new Dictionary<(string, int), int>();

        var result = MethodMapper.Map(methods, coverage);

        // coverage = 0, CRAP = 2² + 2 = 6
        await Assert.That(result[0].Coverage).IsEqualTo(0.0);
        await Assert.That(result[0].CrapScore).IsEqualTo(6.0);
    }

    [Test]
    public async Task should_treat_missing_file_in_coverage_as_zero_coverage()
    {
        var methods = new[] { new MethodComplexity("Foo", "H.cs", 1, 3, 4) };
        var coverage = new Dictionary<(string, int), int>
        {
            [("other.cs", 1)] = 5,
        };

        var result = MethodMapper.Map(methods, coverage);

        // No coverage for H.cs → all 3 lines uncovered → coverage = 0
        // CRAP = 4² × 1³ + 4 = 16 + 4 = 20
        await Assert.That(result[0].Coverage).IsEqualTo(0.0);
        await Assert.That(result[0].CrapScore).IsEqualTo(20.0);
    }

    [Test]
    public async Task should_handle_partial_coverage_within_span()
    {
        var methods = new[] { new MethodComplexity("Partial", "I.cs", 10, 15, 3) };
        var coverage = new Dictionary<(string, int), int>
        {
            [("I.cs", 10)] = 1,
            [("I.cs", 11)] = 0,
            [("I.cs", 12)] = 1,
            [("I.cs", 13)] = 1,
            [("I.cs", 14)] = 0,
            [("I.cs", 15)] = 1,
        };

        var result = MethodMapper.Map(methods, coverage);

        // 6 lines total, 4 covered → coverage = 4/6 ≈ 0.6667
        // CRAP = 9 × (1 - 4/6)³ + 3 = 9 × (1/3)³ + 3 = 9 × 0.037037 + 3 = 3.3333
        await Assert.That(result[0].Coverage).IsCloseTo(4.0 / 6.0, 0.001);
        var expectedCrap = (9.0 * Math.Pow(1.0 - 4.0 / 6.0, 3)) + 3.0;
        await Assert.That(result[0].CrapScore).IsCloseTo(expectedCrap, 0.001);
    }
}
