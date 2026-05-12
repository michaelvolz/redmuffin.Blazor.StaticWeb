namespace redmuffin.Tools.QualityGates.Analysis;

public sealed record SlnxDiscoveredProjects(
    IReadOnlyList<string> SourceProjects,
    IReadOnlyList<string> TestProjects,
    string SlnxPath
);
