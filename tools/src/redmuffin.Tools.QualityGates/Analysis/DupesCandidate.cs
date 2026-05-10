namespace redmuffin.Tools.QualityGates.Analysis;

/// <summary>
///     A duplicate code candidate with similarity score and source locations,
///     matching the dry4clj output format.
/// </summary>
public sealed record DupesCandidate(
    double Score,
    string LeftFile,
    int LeftStartLine,
    int LeftEndLine,
    string RightFile,
    int RightStartLine,
    int RightEndLine,
    int LeftNodes,
    int RightNodes);
