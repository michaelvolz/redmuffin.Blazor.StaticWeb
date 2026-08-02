namespace redmuffin.Tools.QualityGates.Models;

/// <summary>
/// Component-level directed edge used for forbidden-dependencies and
/// allowed-exceptions config entries.
/// </summary>
public sealed record DependencyEdge(string From, string To);
