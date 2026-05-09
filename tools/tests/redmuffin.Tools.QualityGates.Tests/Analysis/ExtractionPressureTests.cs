namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using redmuffin.Tools.QualityGates.Analysis;

public sealed class ExtractionPressureTests
{
    // --- ComputeDefore ---

    [Test]
    public async Task should_compute_dbefore_for_valid_input()
    {
        // F=5, I=3, V=2 → D_before = (2 * 2^1.5) / 3 = (2 * 2.828) / 3 ≈ 1.886
        var pressure = ExtractionPressure.ComputeDefore(5, 3, 2);

        await Assert.That(pressure).IsEqualTo(1.885618083164127);
    }

    [Test]
    public async Task should_return_zero_when_f_is_three_or_less()
    {
        var pressure = ExtractionPressure.ComputeDefore(3, 4, 1);

        await Assert.That(pressure).IsEqualTo(0.0);
    }

    [Test]
    public async Task should_return_zero_when_v_exceeds_four()
    {
        var pressure = ExtractionPressure.ComputeDefore(6, 2, 5);

        await Assert.That(pressure).IsEqualTo(0.0);
    }

    [Test]
    public async Task should_return_zero_when_instance_count_is_one()
    {
        var pressure = ExtractionPressure.ComputeDefore(4, 1, 3);

        await Assert.That(pressure).IsEqualTo(0.0);
    }

    [Test]
    public async Task should_compute_large_cluster_pressure()
    {
        // F=20, I=10, V=3 → D_before = (17 * 9^1.5) / 4 = (17 * 27) / 4 = 114.75
        var pressure = ExtractionPressure.ComputeDefore(20, 10, 3);

        await Assert.That(pressure).IsEqualTo(114.75);
    }

    // --- ComputeExtractionPressure ---

    [Test]
    public async Task should_compute_extraction_pressure_for_cluster()
    {
        // F=5, I=3, V=2
        // D_before ≈ 1.8856
        // H = 5*0.5 + 2*0.3 = 2.5 + 0.6 = 3.1
        // Pressure = max(0, 1.8856 - 0 - 3.1) = max(0, -1.2144) = 0
        var pressure = ExtractionPressure.ComputeExtractionPressure(5, 3, 2);

        await Assert.That(pressure).IsEqualTo(0.0);
    }

    [Test]
    public async Task should_return_positive_pressure_when_dbefore_exceeds_helper_cost()
    {
        // F=20, I=10, V=3
        // D_before = 114.75
        // H = 20*0.5 + 3*0.3 = 10 + 0.9 = 10.9
        // Pressure = max(0, 114.75 - 0 - 10.9) = 103.85
        var pressure = ExtractionPressure.ComputeExtractionPressure(20, 10, 3);

        await Assert.That(pressure).IsEqualTo(103.85);
    }

    // --- ComputeFilePressure ---

    [Test]
    public async Task should_sum_extraction_pressure_across_harmful_clusters()
    {
        var harmful = new[]
        {
            new DuplicationChannel(1, Array.Empty<TestMethod>(), 20, 3, 10, ChannelType.Harmful),
            new DuplicationChannel(2, Array.Empty<TestMethod>(), 5, 2, 3, ChannelType.Harmful),
        };
        var results = new DuplicationResults(harmful, Array.Empty<DuplicationChannel>(), Array.Empty<DuplicationChannel>(), 0.0);

        var filePressure = ExtractionPressure.ComputeFilePressure(results);

        // Cluster 1: F=20,I=10,V=3 → 114.75, H=10.9 → 103.85
        // Cluster 2: F=5,I=3,V=2 → 1.8856, H=3.1 → 0
        // Total = 103.85 + 0 = 103.85, matrix credit = 0
        await Assert.That(filePressure.NetPressure).IsEqualTo(103.85);
    }

    [Test]
    public async Task should_subtract_matrix_credit_for_case_matrix_clusters()
    {
        var harmful = new[]
        {
            new DuplicationChannel(1, Array.Empty<TestMethod>(), 20, 3, 10, ChannelType.Harmful),
        };
        var caseMatrix = new[]
        {
            new DuplicationChannel(2, Array.Empty<TestMethod>(), 0, 0, 3, ChannelType.CaseMatrix),
            new DuplicationChannel(3, Array.Empty<TestMethod>(), 0, 0, 2, ChannelType.CaseMatrix),
        };
        var results = new DuplicationResults(harmful, caseMatrix, Array.Empty<DuplicationChannel>(), 0.0);

        var filePressure = ExtractionPressure.ComputeFilePressure(results);

        // Harmful pressure: F=20,I=10,V=3 → 114.75, H=10.9 → 103.85
        // Matrix credit: 1.5 * 2 = 3.0
        // Net = 103.85 - 3.0 = 100.85
        await Assert.That(filePressure.NetPressure).IsEqualTo(100.85);
    }

    [Test]
    public async Task should_return_zero_pressure_when_no_harmful_clusters()
    {
        var results = new DuplicationResults(
            Array.Empty<DuplicationChannel>(),
            Array.Empty<DuplicationChannel>(),
            Array.Empty<DuplicationChannel>(),
            0.0);

        var filePressure = ExtractionPressure.ComputeFilePressure(results);

        await Assert.That(filePressure.NetPressure).IsEqualTo(0.0);
    }
}
