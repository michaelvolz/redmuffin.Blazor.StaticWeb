namespace redmuffin.Tools.QualityGates.Analysis;

public sealed record DuplicationResults(
    IReadOnlyList<DuplicationChannel> HarmfulDuplication,
    IReadOnlyList<DuplicationChannel> CaseMatrixRepetition,
    IReadOnlyList<DuplicationChannel> SubjectRepetition,
    double EffectiveDuplicationScore);
