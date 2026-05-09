namespace redmuffin.Tools.QualityGates.Analysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class TestNormalizer
{
    public static IReadOnlyList<string> Normalize(MethodDeclarationSyntax method)
    {
        var walker = new NormalizerWalker();
        walker.Visit(method.Body ?? (SyntaxNode)method);

        return walker.Features.ToList().AsReadOnly();
    }

    private sealed class NormalizerWalker : CSharpSyntaxWalker
    {
        public List<string> Features { get; } = [];

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            Features.Add("$id");
        }

        public override void VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            var token = node.Kind() switch
            {
                SyntaxKind.StringLiteralExpression => "$str",
                SyntaxKind.NumericLiteralExpression => "$num",
                SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression => "$bool",
                SyntaxKind.NullLiteralExpression => "$null",
                SyntaxKind.DefaultLiteralExpression => "$default",
                _ => "$lit",
            };
            Features.Add(token);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            // Walk the expression part (member access chain) first, then arguments
            if (node.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                Features.Add(".(");
                Visit(memberAccess.Expression);
                Features.Add(")");
            }
            else
            {
                Visit(node.Expression);
            }

            foreach (var arg in node.ArgumentList.Arguments)
            {
                Visit(arg);
            }
        }

        public override void VisitBlock(BlockSyntax node)
        {
            foreach (var statement in node.Statements)
            {
                Visit(statement);
            }
        }

        public override void VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            Visit(node.Expression);
        }

        public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            Features.Add("$local");
            Visit(node.Declaration);
        }
    }
}
