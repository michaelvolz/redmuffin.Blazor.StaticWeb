namespace redmuffin.Tools.QualityGates.Analysis;

public sealed record DuplicationChannel(
    int ClusterId,
    IReadOnlyList<TestMethod> Methods,
    int SharedForms,
    int VariablePoints,
    int InstanceCount,
    ChannelType ChannelType);
