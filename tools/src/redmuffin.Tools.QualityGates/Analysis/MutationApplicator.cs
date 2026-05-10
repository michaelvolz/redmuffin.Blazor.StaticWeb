namespace redmuffin.Tools.QualityGates.Analysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class MutationApplicator
{
    public static string Apply(string source, int siteIndex, MutationSite site)
    {
        if (siteIndex < 0 || siteIndex != site.Index)
        {
            throw new ArgumentOutOfRangeException(nameof(siteIndex));
        }

        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();

        var oldSpan = site.Node.Span;
        var targetNode = root.DescendantNodes()
            .FirstOrDefault(n => n.Span == oldSpan && n.Kind() == site.OriginalKind)
            ?? throw new InvalidOperationException(
                $"Cannot find node with span [{oldSpan.Start},{oldSpan.End}] and kind {site.OriginalKind}");

        var rewriter = new MutationRewriter(targetNode, site);
        var newRoot = rewriter.Visit(root);

        return newRoot!.ToFullString();
    }

    private sealed class MutationRewriter : CSharpSyntaxRewriter
    {
        private readonly SyntaxNode _targetNode;
        private readonly MutationSite _site;

        public MutationRewriter(SyntaxNode targetNode, MutationSite site)
        {
            _targetNode = targetNode;
            _site = site;
        }

        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            if (node is not null && node.Span == _targetNode.Span)
            {
                return Mutate(node);
            }

            return base.Visit(node);
        }

        private SyntaxNode Mutate(SyntaxNode node)
        {
            return _site.Category switch
            {
                MutationCategory.Arithmetic => MutateArithmetic(node),
                MutationCategory.Comparison => MutateComparison(node),
                MutationCategory.Equality => MutateEquality(node),
                MutationCategory.Boolean => MutateBoolean(node),
                MutationCategory.Conditional => MutateConditional(node),
                MutationCategory.Constant => MutateConstant(node),
                _ => node,
            };
        }

        private SyntaxNode MutateArithmetic(SyntaxNode node)
        {
            if (node is BinaryExpressionSyntax binary)
            {
                var newKind = _site.MutantKind;
                return SyntaxFactory.BinaryExpression(newKind, binary.Left, binary.Right)
                    .WithTriviaFrom(binary);
            }

            return node;
        }

        private SyntaxNode MutateComparison(SyntaxNode node)
        {
            if (node is BinaryExpressionSyntax binary)
            {
                return SyntaxFactory.BinaryExpression(_site.MutantKind, binary.Left, binary.Right)
                    .WithTriviaFrom(binary);
            }

            return node;
        }

        private SyntaxNode MutateEquality(SyntaxNode node)
        {
            if (node is BinaryExpressionSyntax binary)
            {
                return SyntaxFactory.BinaryExpression(_site.MutantKind, binary.Left, binary.Right)
                    .WithTriviaFrom(binary);
            }

            return node;
        }

        private SyntaxNode MutateBoolean(SyntaxNode node)
        {
            if (node is LiteralExpressionSyntax literal)
            {
                return SyntaxFactory.LiteralExpression(_site.MutantKind).WithTriviaFrom(literal);
            }

            return node;
        }

        private SyntaxNode MutateConditional(SyntaxNode node)
        {
            if (node is ExpressionSyntax expression)
            {
                return SyntaxFactory.PrefixUnaryExpression(
                    SyntaxKind.LogicalNotExpression,
                    SyntaxFactory.ParenthesizedExpression(expression))
                    .WithTriviaFrom(expression);
            }

            return node;
        }

        private SyntaxNode MutateConstant(SyntaxNode node)
        {
            if (node is LiteralExpressionSyntax literal && literal.Kind() == SyntaxKind.NumericLiteralExpression)
            {
                var newValue = literal.Token.Value is 0 ? 1 : 0;
                return SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(newValue))
                    .WithTriviaFrom(literal);
            }

            return node;
        }
    }
}
