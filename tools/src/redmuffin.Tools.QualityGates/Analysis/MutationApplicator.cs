namespace redmuffin.Tools.QualityGates.Analysis;

using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class MutationApplicator
{
    public static string Apply(string source, MutationSite site)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();

        var oldSpan = site.Node.Span;
        var targetNode = root.DescendantNodes()
            .FirstOrDefault(n => n.Span == oldSpan && n.IsKind(site.OriginalKind))
            ?? throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Cannot find node with span [{oldSpan.Start},{oldSpan.End}] and kind {site.OriginalKind}"));

        // Replace the already-resolved target node. Do not walk with a span-only
        // rewriter: ArgumentSyntax often shares Span with its expression, and a
        // parent match would NO-OP (cast fail) without visiting the child.
        var mutated = ApplyMutation(targetNode, site);
        var newRoot = root.ReplaceNode(targetNode, mutated);
        return newRoot.ToFullString();
    }

    private static SyntaxNode ApplyMutation(SyntaxNode node, MutationSite site) =>
        site.Category switch
        {
            MutationCategory.Arithmetic or MutationCategory.Comparison or MutationCategory.Equality
                or MutationCategory.Logical
                => MutateArithmeticOrRelational(node, site),
            MutationCategory.Boolean => MutateBoolean(node, site),
            MutationCategory.Conditional => MutateConditional(node),
            MutationCategory.Unary => MutateUnaryStrip(node),
            MutationCategory.NullRvalue => MutateNullRvalue(node),
            _ => MutateConstant(node),
        };

    private static SyntaxNode MutateArithmeticOrRelational(SyntaxNode node, MutationSite site)
    {
        if (node is BinaryExpressionSyntax binary)
        {
            return SyntaxFactory.BinaryExpression(site.MutantKind, binary.Left, binary.Right)
                .WithTriviaFrom(binary);
        }

        if (node is PostfixUnaryExpressionSyntax postfix)
        {
            return SyntaxFactory.PostfixUnaryExpression(site.MutantKind, postfix.Operand)
                .WithTriviaFrom(postfix);
        }

        if (node is PrefixUnaryExpressionSyntax prefix
            && site.OriginalKind is SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression)
        {
            return SyntaxFactory.PrefixUnaryExpression(site.MutantKind, prefix.Operand)
                .WithTriviaFrom(prefix);
        }

        return node;
    }

    private static SyntaxNode MutateBoolean(SyntaxNode node, MutationSite site)
    {
        if (node is LiteralExpressionSyntax literal)
        {
            return SyntaxFactory.LiteralExpression(site.MutantKind).WithTriviaFrom(literal);
        }

        return node;
    }

    private static SyntaxNode MutateConditional(SyntaxNode node)
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

    private static SyntaxNode MutateConstant(SyntaxNode node)
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

    private static SyntaxNode MutateUnaryStrip(SyntaxNode node)
    {
        if (node is PrefixUnaryExpressionSyntax prefix)
        {
            return prefix.Operand.WithTriviaFrom(prefix);
        }

        return node;
    }

    private static SyntaxNode MutateNullRvalue(SyntaxNode node)
    {
        if (node is ExpressionSyntax expression)
        {
            return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                .WithTriviaFrom(expression);
        }

        return node;
    }
}
