namespace redmuffin.Tools.QualityGates.Analysis;

using System.Collections.Frozen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class MutationDiscoverer
{
    /// <summary>
    /// Syntactic shapes mutate4java treats as reference-like for null-rvalue
    /// (no SemanticModel). Table-driven for O(1) membership and CC 1.
    /// </summary>
    private static readonly FrozenSet<SyntaxKind> ReferenceLikeKinds =
        FrozenSet.ToFrozenSet(
        [
            SyntaxKind.StringLiteralExpression,
            SyntaxKind.InterpolatedStringExpression,
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.ImplicitObjectCreationExpression,
            SyntaxKind.ArrayCreationExpression,
            SyntaxKind.ImplicitArrayCreationExpression,
            SyntaxKind.CollectionExpression,
            SyntaxKind.AnonymousObjectCreationExpression,
            SyntaxKind.AsExpression,
        ]);

    public static IReadOnlyList<MutationSite> FindSites(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();

        var sites = new List<MutationSite>();
        var walker = new MutationWalker(sites);
        walker.Visit(root);

        return sites;
    }

    private sealed class MutationWalker(List<MutationSite> sites) : CSharpSyntaxWalker
    {
        private readonly List<MutationSite> _sites = sites;

        public override void Visit(SyntaxNode? node)
        {
            if (node is null)
            {
                return;
            }

            MatchNode(node);
            base.Visit(node);
        }

        private void MatchNode(SyntaxNode node)
        {
            foreach (var rule in MutationRules.All)
            {
                if (MatchesRule(node, rule))
                {
                    AddMutationSite(node, rule);
                }
            }

            TryAddNullRvalueSite(node);
        }

        /// <summary>
        /// mutate4java null-rvalue: replace reference-typed RHS of return, assignment,
        /// and variable initializers with null. Without a semantic model, only
        /// expressions that are syntactically reference-like are candidates.
        /// </summary>
        private void TryAddNullRvalueSite(SyntaxNode node)
        {
            var expression = ExtractNullRvalueExpression(node);
            if (expression is null || !IsReferenceLikeExpression(expression))
            {
                return;
            }

            if (expression.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return;
            }

            var location = expression.GetLocation();
            var lineSpan = location.GetLineSpan();
            _sites.Add(new MutationSite(
                Index: _sites.Count,
                Category: MutationCategory.NullRvalue,
                Line: lineSpan.StartLinePosition.Line,
                Column: lineSpan.StartLinePosition.Character,
                Description: "Replace expression with null",
                OriginalKind: expression.Kind(),
                MutantKind: SyntaxKind.NullLiteralExpression,
                Node: expression));
        }

        private static ExpressionSyntax? ExtractNullRvalueExpression(SyntaxNode node) =>
            node switch
            {
                ReturnStatementSyntax { Expression: { } expression } => expression,
                AssignmentExpressionSyntax assignment
                    when assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                    => assignment.Right,
                EqualsValueClauseSyntax equals => equals.Value,
                _ => null,
            };

        private static bool IsReferenceLikeExpression(ExpressionSyntax expression) =>
            ReferenceLikeKinds.Contains(expression.Kind());

        private static bool MatchesRule(SyntaxNode node, MutationRule rule)
        {
            if (!node.IsKind(rule.OriginalKind))
            {
                return false;
            }

            if (rule.MatchPredicate is not null && !rule.MatchPredicate(node))
            {
                return false;
            }

            if (IsSuppressed(node, rule))
            {
                return false;
            }

            return true;
        }

        private static bool IsSuppressed(SyntaxNode node, MutationRule rule) =>
            rule.SuppressionPredicate is not null
            && node.Parent is not null
            && rule.SuppressionPredicate(node, node.Parent);

        private void AddMutationSite(SyntaxNode node, MutationRule rule)
        {
            var siteNode = ResolveSiteNode(node, rule);
            var location = siteNode.GetLocation();
            var lineSpan = location.GetLineSpan();

            _sites.Add(new MutationSite(
                Index: _sites.Count,
                Category: rule.Category,
                Line: lineSpan.StartLinePosition.Line,
                Column: lineSpan.StartLinePosition.Character,
                Description: rule.Description,
                OriginalKind: siteNode.Kind(),
                MutantKind: rule.MutantKind,
                Node: siteNode));
        }

        private static SyntaxNode ResolveSiteNode(SyntaxNode node, MutationRule rule)
        {
            if (rule.Category == MutationCategory.Conditional
                && node is IfStatementSyntax ifStatement)
            {
                return ifStatement.Condition;
            }

            return node;
        }
    }
}
