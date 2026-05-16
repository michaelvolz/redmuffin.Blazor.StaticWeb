namespace redmuffin.Tools.QualityGates.Tests.Commands;

using redmuffin.Tools.QualityGates.Commands;

public sealed class DupesCommandTests
{
    [Test]
    [MethodDataSource(nameof(ApplyDefault_Double_Data))]
    public async Task ApplyDefault_double_should_return_expected(double value, double defaultValue, double expected)
    {
        var result = DupesCommand.ApplyDefault(value, defaultValue);
        await Assert.That(result).IsEqualTo(expected);
    }

    public static IEnumerable<(double Value, double DefaultValue, double Expected)> ApplyDefault_Double_Data()
    {
        yield return (5.0, 10.0, 5.0);
        yield return (0.0, 10.0, 10.0);
        yield return (-1.0, 10.0, 10.0);
        yield return (1.0, 10.0, 1.0);
    }

    [Test]
    [MethodDataSource(nameof(ApplyDefault_Int_Data))]
    public async Task ApplyDefault_int_should_return_expected(int value, int defaultValue, int expected)
    {
        var result = DupesCommand.ApplyDefault(value, defaultValue);
        await Assert.That(result).IsEqualTo(expected);
    }

    public static IEnumerable<(int Value, int DefaultValue, int Expected)> ApplyDefault_Int_Data()
    {
        yield return (5, 10, 5);
        yield return (0, 10, 10);
        yield return (-1, 10, 10);
        yield return (1, 10, 1);
    }
}
