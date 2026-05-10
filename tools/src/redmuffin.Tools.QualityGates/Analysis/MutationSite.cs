namespace redmuffin.Tools.QualityGates.Analysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public sealed record MutationSite(
    int Index,
    MutationCategory Category,
    int Line,
    int Column,
    string Description,
    SyntaxKind OriginalKind,
    SyntaxKind MutantKind,
    SyntaxNode Node);
