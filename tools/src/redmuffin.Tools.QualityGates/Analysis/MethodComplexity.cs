namespace redmuffin.Tools.QualityGates.Analysis;

public sealed record MethodComplexity(
    string MethodName,
    string FilePath,
    int StartLine,
    int EndLine,
    int Complexity);
