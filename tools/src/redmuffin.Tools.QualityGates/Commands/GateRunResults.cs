namespace redmuffin.Tools.QualityGates.Commands;

public sealed record GateRunResults(
    int OverallExit,
    int CrapExit,
    int ScrapExit,
    string? ArchConfig,
    int ArchExit,
    string? MutateSource,
    int MutateExit,
    bool RunDupes,
    int DupesExit,
    bool RunDepth,
    int DepthExit);
