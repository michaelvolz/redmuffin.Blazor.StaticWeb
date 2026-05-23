namespace redmuffin.Tools.QualityGates.Commands;

public sealed record GateDescriptor(string Name, Func<Task<int>> Execute, bool Skip);
