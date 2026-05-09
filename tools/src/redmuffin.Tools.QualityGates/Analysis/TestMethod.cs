namespace redmuffin.Tools.QualityGates.Analysis;

using Microsoft.CodeAnalysis;

public sealed record TestMethod(
    string MethodName,
    string FilePath,
    int StartLine,
    int EndLine,
    SyntaxNode BodySyntax,
    string ContainerClassName);
