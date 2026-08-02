namespace redmuffin.Tools.QualityGates.Analysis;

public enum MutantResultType
{
    Killed,
    Survived,
    /// <summary>Apply left source text unchanged — not a real mutant for tests to kill.</summary>
    NoOp,
    Error,
}
