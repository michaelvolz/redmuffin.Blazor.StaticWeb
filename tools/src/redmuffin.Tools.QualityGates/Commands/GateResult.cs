namespace redmuffin.Tools.QualityGates.Commands;

public sealed record GateResult(string Name, int ExitCode, bool Skipped);
