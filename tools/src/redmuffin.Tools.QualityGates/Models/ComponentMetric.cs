namespace redmuffin.Tools.QualityGates.Models;

/// <summary>
/// Per-component architecture health metrics (dependency-checker style).
/// I = fan-out / (fan-in + fan-out); A = abstract types / total types;
/// D = |A + I − 1|; zone from healthy-threshold around the main sequence.
/// </summary>
public sealed record ComponentMetric(
    string Component,
    int FanIn,
    int FanOut,
    double Instability,
    double Abstractness,
    double Distance,
    ArchZone Zone);
