namespace redmuffin.Tools.QualityGates.Models;

public sealed record ArchResult(
    int ExitCode,
    List<ArchViolation> Violations,
    List<ArchCycle> Cycles,
    int ProjectsScanned,
    int ComponentsDefined);
