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
}
