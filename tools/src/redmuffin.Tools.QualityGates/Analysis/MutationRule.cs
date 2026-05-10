namespace redmuffin.Tools.QualityGates.Analysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public sealed record MutationRule(
    MutationCategory Category,
    SyntaxKind OriginalKind,
    SyntaxKind MutantKind,
    string Description,
    Func<SyntaxNode, SyntaxNode, bool>? SuppressionPredicate = null,
    Func<SyntaxNode, bool>? MatchPredicate = null);
