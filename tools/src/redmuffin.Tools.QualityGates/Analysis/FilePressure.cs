namespace redmuffin.Tools.QualityGates.Analysis;

public sealed record FilePressure(
    double TotalExtractionPressure,
    IReadOnlyList<double> ClusterPressures,
    double MatrixCredit,
    double NetPressure);
