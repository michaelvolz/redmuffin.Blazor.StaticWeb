namespace redmuffin.Tools.QualityGates.Commands;

public sealed record ScrapOptions(
    bool Verbose = false,
    bool Json = false,
    bool WriteBaseline = false,
    string? ComparePath = null,
    double StabilityThreshold = 12.0);
