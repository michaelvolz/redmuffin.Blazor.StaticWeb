namespace redmuffin.Tools.QualityGates.Analysis;

/// <summary>
///     A node in a normalized syntax tree following the dry4clj algorithm.
///     Each node has a kind tag (e.g. "if", "binary", "symbol") and zero
///     or more child nodes.
/// </summary>
public sealed record NormalizedNode(string Kind, IReadOnlyList<NormalizedNode> Children)
{
    public NormalizedNode(string kind)
        : this(kind, [])
    {
    }
}
