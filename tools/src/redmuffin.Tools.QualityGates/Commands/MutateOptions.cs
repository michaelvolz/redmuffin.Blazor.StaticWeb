namespace redmuffin.Tools.QualityGates.Commands;

public sealed record MutateOptions(
    bool Scan = false,
    bool MutateAll = false,
    bool SinceLastRun = false,
    int MaxWorkers = 1,
    int MutationWarning = 50,
    int TimeoutFactor = 10,
    bool ReuseCoverage = false,
    bool AutoCoverage = false,
    IReadOnlySet<int>? Lines = null);
