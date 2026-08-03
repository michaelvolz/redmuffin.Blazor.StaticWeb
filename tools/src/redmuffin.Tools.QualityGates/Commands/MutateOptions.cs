namespace redmuffin.Tools.QualityGates.Commands;

public sealed record MutateOptions(
    bool Scan = false,
    bool MutateAll = false,
    bool SinceLastRun = false,
    bool UpdateManifest = false,
    int MaxWorkers = 1,
    int MutationWarning = 100,
    int TimeoutFactor = 10,
    bool ReuseCoverage = false,
    bool AutoCoverage = false,
    IReadOnlySet<int>? Lines = null,
    bool NoTestFilter = false);
