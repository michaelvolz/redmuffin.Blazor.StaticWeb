namespace redmuffin.Tools.QualityGates.Analysis;

public sealed record MethodCrap(
    string MethodName,
    string FilePath,
    int StartLine,
    int Complexity,
    double Coverage,
    double CrapScore,
    bool IsCoverageGap = false);
