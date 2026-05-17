namespace redmuffin.Tools.QualityGates.Analysis;

public sealed record DepthResult(
    string MethodName,
    string FilePath,
    int LineNumber,
    bool IsShallow,
    int ParameterCount,
    bool IsWrongAbstraction,
    bool IsEntangled,
    int CompositeScore,
    IReadOnlyList<string> Signals);
