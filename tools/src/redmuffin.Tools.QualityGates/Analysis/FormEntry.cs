namespace redmuffin.Tools.QualityGates.Analysis;

public sealed record FormEntry(
    string Id,
    int Line,
    int EndLine,
    string Hash);
