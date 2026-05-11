namespace redmuffin.Tools.QualityGates.Tests.Analysis;

using Microsoft.CodeAnalysis.CSharp;
using redmuffin.Tools.QualityGates.Analysis;

public partial class DupesNormalizerTests
{
    private static async Task<IReadOnlyList<object>> ParseNormalized(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync().ConfigureAwait(false);
        return DupesNormalizer.Normalize(root);
    }

    private static async Task<ISet<string>> ParseFingerprints(string code)
    {
        var normalized = await ParseNormalized(code).ConfigureAwait(false);
        return DupesNormalizer.ComputeFingerprints(normalized);
    }
}
