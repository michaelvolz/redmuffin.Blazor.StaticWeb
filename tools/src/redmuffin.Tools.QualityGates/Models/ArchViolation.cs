namespace redmuffin.Tools.QualityGates.Models;

public sealed record ArchViolation(
    string SourceProject,
    string TargetProject,
    string SourceComponent,
    string TargetComponent,
    string Reason);
