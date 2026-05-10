namespace redmuffin.Tools.QualityGates.Commands;

/// <summary>
///     Options for the dupes (duplicate code detection) gate.
///     Defaults match Uncle Bob's dry4clj tool.
/// </summary>
public sealed record DupesOptions(
    double Threshold = 0.82,
    int MinLines = 4,
    int MinNodes = 20,
    string Format = "text",
    IReadOnlyList<string>? Paths = null)
{
    public IReadOnlyList<string> Paths { get; init; } = Paths ?? [];
}
