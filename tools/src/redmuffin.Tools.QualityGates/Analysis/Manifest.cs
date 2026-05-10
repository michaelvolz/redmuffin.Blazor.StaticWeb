namespace redmuffin.Tools.QualityGates.Analysis;

public sealed record Manifest(
    int Version,
    DateTime TestedAt,
    string ModuleHash,
    IReadOnlyList<FormEntry> Forms);
