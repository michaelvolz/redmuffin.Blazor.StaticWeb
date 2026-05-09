namespace redmuffin.Tools.QualityGates.Models;

public sealed record ArchCycle(IReadOnlyList<string> Components, int Length);
