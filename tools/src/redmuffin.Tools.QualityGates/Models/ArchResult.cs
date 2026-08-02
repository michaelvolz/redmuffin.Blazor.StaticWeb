namespace redmuffin.Tools.QualityGates.Models;

public sealed record ArchResult(
    int ExitCode,
    IReadOnlyList<ArchViolation> Violations,
    IReadOnlyList<ArchCycle> Cycles,
    int ProjectsScanned,
    int ComponentsDefined)
{
    public IReadOnlyList<ComponentMetric> Metrics { get; init; } = [];
}
