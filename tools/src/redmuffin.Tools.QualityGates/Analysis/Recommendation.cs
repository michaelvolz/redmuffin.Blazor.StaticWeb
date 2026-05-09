namespace redmuffin.Tools.QualityGates.Analysis;

public sealed record Recommendation(
    StabilityMode Mode,
    AiActionability AiActionability,
    string ActionabilityMessage);
