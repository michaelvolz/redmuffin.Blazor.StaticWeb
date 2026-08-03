namespace redmuffin.Tools.QualityGates.Analysis;

using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class CyclomaticComplexity
{
    public static IReadOnlyList<MethodComplexity> Analyze(string projectPath)
    {
        var results = new List<MethodComplexity>();

        foreach (var file in SourcePathFilter.EnumerateCsFiles(projectPath))
        {
            var source = File.ReadAllText(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var root = syntaxTree.GetCompilationUnitRoot();
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                var cc = ComputeCyclomaticComplexity(method);
                var lineSpan = method.GetLocation().GetLineSpan();
                results.Add(new MethodComplexity(
                    method.Identifier.Text,
                    Path.GetFullPath(file),
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.EndLinePosition.Line + 1,
                    cc));
            }
        }

        return results.AsReadOnly();
    }

    private static int ComputeCyclomaticComplexity(MethodDeclarationSyntax method)
    {
        var walker = new CcWalker();
        walker.Visit(method);
        return walker.DecisionPoints + 1;
    }

    private sealed class CcWalker : CSharpSyntaxWalker
    {
        public int DecisionPoints { get; private set; }

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            DecisionPoints++;
            base.VisitIfStatement(node);
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            DecisionPoints++;
            base.VisitWhileStatement(node);
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            DecisionPoints++;
            base.VisitForStatement(node);
        }

        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            DecisionPoints++;
            base.VisitForEachStatement(node);
        }

        public override void VisitCaseSwitchLabel(CaseSwitchLabelSyntax node)
        {
            DecisionPoints++;
            base.VisitCaseSwitchLabel(node);
        }

        public override void VisitCatchClause(CatchClauseSyntax node)
        {
            DecisionPoints++;
            base.VisitCatchClause(node);
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.LogicalAndExpression) ||
                node.IsKind(SyntaxKind.LogicalOrExpression) ||
                node.IsKind(SyntaxKind.CoalesceExpression))
            {
                DecisionPoints++;
            }

            base.VisitBinaryExpression(node);
        }

        public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
        {
            DecisionPoints++;
            base.VisitConditionalExpression(node);
        }

        public override void VisitSwitchExpressionArm(SwitchExpressionArmSyntax node)
        {
            DecisionPoints++;
            base.VisitSwitchExpressionArm(node);
        }

        public override void VisitBinaryPattern(BinaryPatternSyntax node)
        {
            if (node.IsKind(SyntaxKind.AndPattern) ||
                node.IsKind(SyntaxKind.OrPattern))
            {
                DecisionPoints++;
            }

            base.VisitBinaryPattern(node);
        }

        public override void VisitUnaryPattern(UnaryPatternSyntax node)
        {
            IncrementIfKind(node, SyntaxKind.NotPattern);
            base.VisitUnaryPattern(node);
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            IncrementIfKind(node, SyntaxKind.SuppressNullableWarningExpression);
            base.VisitPostfixUnaryExpression(node);
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            IncrementIfKind(node, SyntaxKind.CoalesceAssignmentExpression);
            base.VisitAssignmentExpression(node);
        }

        private void IncrementIfKind(SyntaxNode node, SyntaxKind kind)
        {
            if (node.IsKind(kind))
            {
                DecisionPoints++;
            }
        }

        public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
        {
            DecisionPoints++;
            base.VisitConditionalAccessExpression(node);
        }
    }
}

// clj-mutate-manifest-begin
// {"version":1,"testedAt":"2026-08-03T12:27:13.276927Z","moduleHash":"0fd359a0c6d529b5cee9b4898eacd31a6757316ee713a5d9eee0965dad7a33ac","forms":[{"id":"Analyze","line":9,"endLine":34,"hash":"7faa5b85fba1a53a646a0879122056d99a1b74b024a2c845b6fb8dabb92050c7"},{"id":"ComputeCyclomaticComplexity","line":36,"endLine":41,"hash":"232b265b3adc05ceb20e9152ef3463c97aa580ac1fbe9a9c1551a5debdaaccdf"}]}
// clj-mutate-manifest-end
